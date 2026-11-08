using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UnityConsent;
using Scripts.Economy;

namespace Scripts
{
    [Serializable]
    public sealed class TelemetryConfiguration
    {
        public string buildChannel = "beta";
        public string buildNumber = "development";
        public string supportEmail = "bledaburat@gmail.com";
        public string privacyPolicyUrl = "";
    }

    public sealed class TelemetryRuntime : MonoBehaviour
    {
        public const string ConsentKey = "fruitlab_analytics_consent_v1";
        private const string DeletionKey = "fruitlab_analytics_deletion_pending";
        private const string ConsentScopeKey="fruitlab_analytics_scope_v1";
        public static TelemetryRuntime Instance { get; private set; }
        public TelemetryConfiguration Configuration { get; private set; }
        public bool HasChoice => PlayerPrefs.HasKey(ConsentKey);
        public bool AnalyticsAllowed => PlayerPrefs.GetInt(ConsentKey, 0) == 1;
        public bool DeletionPending => PlayerPrefs.GetInt(DeletionKey, 0) == 1;
        private bool _ready, _initializing, _sessionRecorded, _warned;
        private double _retryAt;
        private double _drainAt;
        private bool _paused,_focused=true;
        private PlayerEngagementService _engagement;
        private ProfileTelemetryDelivery _delivery;
        private sealed class PendingEvent {public string Name;public Dictionary<string,object> Data;}
        private readonly Queue<PendingEvent> _pending = new Queue<PendingEvent>();
        private TelemetryPrivacyPanel _panel;
        private bool CanUpload => !Application.isEditor && AnalyticsAllowed && !DeletionPending;
        public static EconomyTelemetryContext EconomyContext()
        {
            var runtime=Instance;if(runtime==null || !runtime.CanUpload)return null;
            string scope=PlayerPrefs.GetString(ConsentScopeKey,"");if(!Guid.TryParseExact(scope,"N",out _))return null;
            return new EconomyTelemetryContext {ConsentScope=scope,UtcDay=GardenDay.FromUtc(DateTime.UtcNow),
                BuildChannel=runtime.Configuration.buildChannel,Version=Application.version,Build=runtime.Configuration.buildNumber};
        }
        private void EnsureScope()
        {
            if(!CanUpload || Guid.TryParseExact(PlayerPrefs.GetString(ConsentScopeKey,""),"N",out _))return;
            PlayerPrefs.SetString(ConsentScopeKey,Guid.NewGuid().ToString("N"));PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning)
            {
                var consent = EndUserConsent.GetConsentState(); consent.AnalyticsIntent = ConsentStatus.Denied;
                EndUserConsent.SetConsentState(consent); return;
            }
#endif
            if (Instance != null) return;
            var root = new GameObject("FruitLabTelemetry"); DontDestroyOnLoad(root);
            root.AddComponent<TelemetryRuntime>();
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            var config = Resources.Load<TextAsset>("TelemetryConfig");
            Configuration = config != null ? JsonUtility.FromJson<TelemetryConfiguration>(config.text) : new TelemetryConfiguration();
            EnsureScope();
            ApplyConsent();
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (!Application.isEditor) _ = InitializeAsync();
        }
        private IEnumerator Start(){yield return null;RecordEngagement(PlayerActivity.ActiveDay);}
        private async Task InitializeAsync()
        {
            if (_initializing) return;
            _initializing = true;
            try
            {
                await UnityServicesBootstrap.InitializeAsync();
                if (this == null) return;
                _ready = true; _warned = false;
                if (DeletionPending)
                {
                    AnalyticsService.Instance.RequestDataDeletion();
                    PlayerPrefs.DeleteKey(DeletionKey); PlayerPrefs.Save();
                }
                if (CanUpload && !_sessionRecorded) { _sessionRecorded = true; GameTelemetry.SessionStarted(); }
                RecordEngagement(PlayerActivity.ActiveDay);
                Drain();
            }
            catch (Exception error)
            {
                _retryAt = Time.realtimeSinceStartupAsDouble + 30;
                if (!_warned) { _warned = true; Debug.LogWarning("Analytics initialization deferred: " + error.GetType().Name); }
            }
            finally { _initializing = false; }
        }
        private void Update()
        {
            if (!Application.isEditor && !_ready && !_initializing && Time.realtimeSinceStartupAsDouble >= _retryAt)
                _ = InitializeAsync();
            if(_ready && CanUpload && Time.realtimeSinceStartupAsDouble>=_drainAt){_drainAt=Time.realtimeSinceStartupAsDouble+5;Drain();}
        }
        private void ApplyConsent()
        {
            var consent = EndUserConsent.GetConsentState();
            consent.AnalyticsIntent = CanUpload ? ConsentStatus.Granted : ConsentStatus.Denied;
            consent.AdsIntent = ConsentStatus.Denied;
            EndUserConsent.SetConsentState(consent);
        }
        public void SetAnalyticsChoice(bool allowed)
        {
            bool previouslyAllowed=AnalyticsAllowed;
            allowed = allowed && !DeletionPending;
            PlayerPrefs.SetInt(ConsentKey, allowed ? 1 : 0); PlayerPrefs.Save();
            if (!allowed) { _pending.Clear(); _sessionRecorded = false; }
            if(!Application.isEditor)
            {
                if(!allowed || !previouslyAllowed)PlayerPrefs.DeleteKey(ConsentScopeKey);
                EnsureScope();PlayerPrefs.Save();
                if(!allowed)
                    try{PlayerProfileRuntime.Current.Update(profile=>profile.TelemetryOutbox.Clear());}
                    catch(Exception error){Debug.LogWarning("Analytics queue cleanup deferred: "+error.GetType().Name);}
                else RecordEngagement(PlayerActivity.ActiveDay);
            }
            ApplyConsent();
            if (!Application.isEditor) _ = InitializeAsync();
        }
        public void RequestAnalyticsDeletion()
        {
            PlayerPrefs.SetInt(DeletionKey, 1);
            SetAnalyticsChoice(false);
        }
        public void Record(Dictionary<string, object> data)
        {
            if (!CanUpload) return;
            var copy = new Dictionary<string, object>(data) {
                ["flBuildChannel"] = Configuration.buildChannel,
                ["flVersion"] = Application.version,
                ["flBuild"] = Configuration.buildNumber
            };
            if (_ready && Send(GameTelemetry.EventName,copy))return;
            if (_pending.Count < 64) _pending.Enqueue(new PendingEvent {Name=GameTelemetry.EventName,Data=copy});
        }
        private bool Send(string eventName,Dictionary<string, object> data)
        {
            try
            {
                var ev = new CustomEvent(eventName);
                foreach (var pair in data) ev.Add(pair.Key, pair.Value);
                AnalyticsService.Instance.RecordEvent(ev);
                return true;
            }
            catch (Exception error)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("Analytics event could not be recorded: " + error.GetType().Name); }
                return false;
            }
        }
        private void Drain()
        {
            if(!_ready || !CanUpload)return;
            int sent=0;
            while(_pending.Count>0 && sent++<16)
            {
                var next=_pending.Peek();if(!Send(next.Name,next.Data))return;_pending.Dequeue();
            }
            try
            {
                var context=EconomyContext();if(context==null)return;
                if(_delivery==null)_delivery=new ProfileTelemetryDelivery(PlayerProfileRuntime.Current);
                _delivery.Drain(context.ConsentScope,data=>Send(ProfileTelemetryJournal.EventName,data.Parameters()));
            }
            catch(Exception error){if(!_warned){_warned=true;Debug.LogWarning("Economy analytics delivery deferred: "+error.GetType().Name);}}
        }
        public void RecordEngagement(PlayerActivity activity)
        {
            if(!CanUpload || _paused || !_focused)return;
            try
            {
                if(_engagement==null)_engagement=new PlayerEngagementService(PlayerProfileRuntime.Current,EconomyContext);
                _engagement.Record(PlayerActivity.ActiveDay);
                if(activity!=PlayerActivity.ActiveDay)_engagement.Record(activity);
            }
            catch(Exception error){if(!_warned){_warned=true;Debug.LogWarning("Activity analytics deferred: "+error.GetType().Name);}}
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Menu") StartCoroutine(MenuReady());
        }
        private IEnumerator MenuReady()
        {
            yield return null; yield return null;
            var menu = FindFirstObjectByType<Menu.MenuUIView>();
            if (menu == null) yield break;
            var button = TelemetryPrivacyPanel.AddButton(menu.transform, "PrivacyButton", "Gizlilik ve veri", ShowPrivacy);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = new Vector2(190, 34);
            rect.anchoredPosition = new Vector2(0, -8);
            if (!Application.isEditor && !HasChoice) ShowPrivacy();
        }
        public void ShowPrivacy()
        {
            if (_panel != null) return;
            _panel = TelemetryPrivacyPanel.Open(this);
        }
        private void OnApplicationPause(bool paused)
        {
            bool foreground=!_paused && _focused;_paused=paused;GameTelemetry.Pause(_paused || !_focused);
            if(!foreground && !_paused && _focused)RecordEngagement(PlayerActivity.ActiveDay);
            if(paused){Drain();Flush();}
        }
        private void OnApplicationFocus(bool focused)
        {
            bool foreground=!_paused && _focused;_focused=focused;
            GameTelemetry.Pause(_paused || !_focused);
            if(!foreground && !_paused && _focused)RecordEngagement(PlayerActivity.ActiveDay);
        }
        private void OnApplicationQuit() { GameTelemetry.Abandon("quit");Drain(); Flush(); }
        public void Flush()
        {
            if (!_ready || !CanUpload) return;
            try { AnalyticsService.Instance.Flush(); } catch (Exception) { /* Never interrupt gameplay for telemetry. */ }
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded; Instance = null;
        }
    }
}

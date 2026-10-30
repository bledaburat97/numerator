using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UnityConsent;

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
        public static TelemetryRuntime Instance { get; private set; }
        public TelemetryConfiguration Configuration { get; private set; }
        public bool HasChoice => PlayerPrefs.HasKey(ConsentKey);
        public bool AnalyticsAllowed => PlayerPrefs.GetInt(ConsentKey, 0) == 1;
        public bool DeletionPending => PlayerPrefs.GetInt(DeletionKey, 0) == 1;
        private bool _ready, _initializing, _sessionRecorded, _warned;
        private double _retryAt;
        private readonly Queue<Dictionary<string, object>> _pending = new Queue<Dictionary<string, object>>();
        private TelemetryPrivacyPanel _panel;
        private bool CanUpload => !Application.isEditor && AnalyticsAllowed && !DeletionPending;

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
            ApplyConsent();
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (!Application.isEditor) _ = InitializeAsync();
        }
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
                while (CanUpload && _pending.Count > 0) Send(_pending.Dequeue());
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
            allowed = allowed && !DeletionPending;
            PlayerPrefs.SetInt(ConsentKey, allowed ? 1 : 0); PlayerPrefs.Save();
            if (!allowed) { _pending.Clear(); _sessionRecorded = false; }
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
            if (_ready) Send(copy);
            else if (_pending.Count < 64) _pending.Enqueue(copy);
        }
        private void Send(Dictionary<string, object> data)
        {
            try
            {
                var ev = new CustomEvent(GameTelemetry.EventName);
                foreach (var pair in data) ev.Add(pair.Key, pair.Value);
                AnalyticsService.Instance.RecordEvent(ev);
            }
            catch (Exception error)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("Analytics event could not be recorded: " + error.GetType().Name); }
            }
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
        private void OnApplicationPause(bool paused) { GameTelemetry.Pause(paused); if (paused) Flush(); }
        private void OnApplicationQuit() { GameTelemetry.Abandon("quit"); Flush(); }
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

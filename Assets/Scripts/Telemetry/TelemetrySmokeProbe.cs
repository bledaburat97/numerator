#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Scripts.Economy;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace Scripts
{
    // Explicit, Editor-only SDK upload test. Does not change a player's saved consent or progression.
    public sealed class TelemetrySmokeProbe : MonoBehaviour
    {
        public const string RequestPath = "Library/FruitLabEditor/telemetry-smoke.request";
        private IDisposable _profileIsolation;
        private static string ProfileFingerprint()
        {
            string path=Path.Combine(Application.persistentDataPath,"player-profile.json");
            if(!File.Exists(path))return "missing";
            using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (!File.Exists(RequestPath)) return;
            var profile=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{var data=new PlayerProfileData();data.Progress.LevelId=17;return data;});
            var isolated=PlayerProfileRuntime.UseForTests(profile);
            var root = new GameObject("TelemetrySdkSmokeProbe"); DontDestroyOnLoad(root);
            root.AddComponent<TelemetrySmokeProbe>()._profileIsolation=isolated;
        }
        private async void Start()
        {
            var original = EndUserConsent.GetConsentState();
            bool economy=File.ReadAllText(RequestPath)=="economy";
            string eventName=economy?ProfileTelemetryJournal.EventName:GameTelemetry.EventName;
            string fingerprint=ProfileFingerprint();
            object dispatcher = null; EventInfo finished = null;
            Action<int, bool, bool, bool, bool, byte[]> handler = null;
            object report;
            try
            {
                await UnityServicesBootstrap.InitializeAsync();
                var sdk = AnalyticsService.Instance;
                dispatcher = sdk.GetType().GetField("m_DataDispatcher", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sdk);
                finished = dispatcher.GetType().GetEvent("FlushFinished");
                var uploaded = new TaskCompletionSource<int>();
                handler = (status, success, bad, intermittent, network, payload) => {
                    string text = Encoding.UTF8.GetString(payload);
                    if (text.Contains(eventName) && text.Contains("integration_smoke")) uploaded.TrySetResult(status);
                };
                finished.AddEventHandler(dispatcher, handler);
                EndUserConsent.SetConsentState(new ConsentState { AnalyticsIntent = ConsentStatus.Granted, AdsIntent = ConsentStatus.Denied });
                var flushing = dispatcher.GetType().GetProperty("FlushInProgress");
                double until = Time.realtimeSinceStartupAsDouble + 25;
                while ((bool)flushing.GetValue(dispatcher) && Time.realtimeSinceStartupAsDouble < until) await Task.Delay(50);
                var sample=new CustomEvent(eventName) {
                    ["flAction"] = "integration_smoke", ["flBuildChannel"] = "integration_test",
                    ["flVersion"] = Application.version, ["flBuild"] = "editor-sdk-smoke"
                };
                if(economy)
                {
                    sample.Add("flEventId",Guid.NewGuid().ToString("N"));sample.Add("flUtcDay",GardenDay.FromUtc(DateTime.UtcNow));
                    sample.Add("flAmount",(long)int.MaxValue+1);sample.Add("flFirst",false);sample.Add("flFruit","apple");
                }
                sdk.RecordEvent(sample);
                sdk.Flush();
                if (await Task.WhenAny(uploaded.Task, Task.Delay(30000)) != uploaded.Task)
                    throw new Exception("SDK upload callback timed out");
                int http = await uploaded.Task;
                if (http < 200 || http >= 300) throw new Exception("SDK collector returned HTTP " + http);
                if(!economy)
                {
                    TelemetryRuntime.Instance.ShowPrivacy();
                    await Task.Delay(1000);
                    ScreenCapture.CaptureScreenshot("Library/FruitLabEditor/preview-privacy.png");
                    await Task.Delay(1200);
                }
                if(fingerprint!=ProfileFingerprint())throw new Exception("SDK test changed the real player profile");
                report = new { passed = true, sdk_upload_http = http, event_name = eventName,
                    channel = "integration_test", game_profile_unchanged = true, profile_isolated=true,schema_verified_separately = true,
                    event_browser_processing_pending = true };
            }
            catch (Exception error) { report = new { passed = false, error = error.GetType().Name + ": " + error.Message }; }
            finally
            {
                if (handler != null) finished?.RemoveEventHandler(dispatcher, handler);
                EndUserConsent.SetConsentState(original); File.Delete(RequestPath);
            }
            File.WriteAllText(economy?"docs/design/economy-telemetry-sdk-upload.json":"docs/design/telemetry-sdk-upload.json", JsonConvert.SerializeObject(report, Formatting.Indented));
            UnityEditor.EditorApplication.isPlaying = false;
        }
        private void OnDestroy(){_profileIsolation?.Dispose();_profileIsolation=null;}
    }
}
#endif

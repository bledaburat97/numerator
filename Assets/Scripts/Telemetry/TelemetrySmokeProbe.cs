#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.UnityConsent;

namespace Scripts
{
    // Explicit, Editor-only SDK upload test. Does not change a player's saved consent or progression.
    public sealed class TelemetrySmokeProbe : MonoBehaviour
    {
        public const string RequestPath = "Library/FruitLabEditor/telemetry-smoke.request";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (!File.Exists(RequestPath)) return;
            var root = new GameObject("TelemetrySdkSmokeProbe"); DontDestroyOnLoad(root); root.AddComponent<TelemetrySmokeProbe>();
        }
        private async void Start()
        {
            var original = EndUserConsent.GetConsentState();
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
                    if (text.Contains("fruitlabGameplay") && text.Contains("integration_smoke")) uploaded.TrySetResult(status);
                };
                finished.AddEventHandler(dispatcher, handler);
                EndUserConsent.SetConsentState(new ConsentState { AnalyticsIntent = ConsentStatus.Granted, AdsIntent = ConsentStatus.Denied });
                var flushing = dispatcher.GetType().GetProperty("FlushInProgress");
                double until = Time.realtimeSinceStartupAsDouble + 25;
                while ((bool)flushing.GetValue(dispatcher) && Time.realtimeSinceStartupAsDouble < until) await Task.Delay(50);
                sdk.RecordEvent(new CustomEvent(GameTelemetry.EventName) {
                    ["flAction"] = "integration_smoke", ["flBuildChannel"] = "integration_test",
                    ["flVersion"] = Application.version, ["flBuild"] = "editor-sdk-smoke"
                });
                sdk.Flush();
                if (await Task.WhenAny(uploaded.Task, Task.Delay(30000)) != uploaded.Task)
                    throw new Exception("SDK upload callback timed out");
                int http = await uploaded.Task;
                if (http < 200 || http >= 300) throw new Exception("SDK collector returned HTTP " + http);
                TelemetryRuntime.Instance.ShowPrivacy();
                await Task.Delay(1000);
                ScreenCapture.CaptureScreenshot("Library/FruitLabEditor/preview-privacy.png");
                await Task.Delay(1200);
                report = new { passed = true, sdk_upload_http = http, event_name = GameTelemetry.EventName,
                    channel = "integration_test", player_preferences_changed = false, schema_verified_separately = true,
                    event_browser_processing_pending = true };
            }
            catch (Exception error) { report = new { passed = false, error = error.GetType().Name + ": " + error.Message }; }
            finally
            {
                if (handler != null) finished?.RemoveEventHandler(dispatcher, handler);
                EndUserConsent.SetConsentState(original); File.Delete(RequestPath);
            }
            File.WriteAllText("docs/design/telemetry-sdk-upload.json", JsonConvert.SerializeObject(report, Formatting.Indented));
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif

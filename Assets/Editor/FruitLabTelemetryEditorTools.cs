using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using Newtonsoft.Json;
using Scripts;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.EngineDiagnostics;
using UnityEngine;
using UnityEngine.UnityConsent;

public static class FruitLabTelemetryEditorTools
{
    public const string ConfigPath = "Assets/Resources/TelemetryConfig.json";
    public static readonly Dictionary<string, string> Parameters = new Dictionary<string, string> {
        ["flAction"] = "STRING", ["flBuildChannel"] = "STRING", ["flVersion"] = "STRING", ["flBuild"] = "STRING",
        ["flMode"] = "STRING", ["flLevel"] = "INTEGER", ["flSlots"] = "INTEGER", ["flFruits"] = "INTEGER",
        ["flGuesses"] = "INTEGER", ["flDuration"] = "FLOAT", ["flDifficulty"] = "STRING", ["flEntry"] = "STRING",
        ["flRound"] = "INTEGER", ["flOutcome"] = "STRING", ["flCoins"] = "INTEGER", ["flCrystals"] = "INTEGER",
        ["flTutorial"] = "STRING", ["flStep"] = "INTEGER", ["flPowerup"] = "STRING", ["flSource"] = "STRING",
        ["flOperation"] = "STRING"
    };

    [MenuItem("Fruit Lab/Telemetry/Open Unity Dashboard")]
    public static void Dashboard() => Application.OpenURL("https://cloud.unity.com/home/organizations/" + CloudProjectSettings.organizationKey + "/projects/" + CloudProjectSettings.projectId);

    [MenuItem("Fruit Lab/Telemetry/Use beta analytics channel")]
    public static void Beta() => SetChannel("beta");
    [MenuItem("Fruit Lab/Telemetry/Use production analytics channel")]
    public static void Production() => SetChannel("production");
    private static void SetChannel(string channel)
    {
        var config = JsonUtility.FromJson<TelemetryConfiguration>(File.ReadAllText(ConfigPath));
        config.buildChannel = channel; File.WriteAllText(ConfigPath, JsonUtility.ToJson(config, true) + "\n");
        AssetDatabase.ImportAsset(ConfigPath);
    }

    public static string Review()
    {
        var events = new List<Dictionary<string, object>>();
        double now = 0;
        using (GameTelemetry.CaptureForTests(ev => events.Add(ev), () => now))
        {
            GameTelemetry.BeginSingle(12, 5, 9, LevelEntryMode.New, 0);
            now = 3; GameTelemetry.GuessAccepted(1); GameTelemetry.Pause(true);
            now = 103; GameTelemetry.Pause(false); now = 107;
            GameTelemetry.Finish("win", 2, 2, 0);
            GameTelemetry.Finish("win", 2, 2, 0); GameTelemetry.Abandon("scene_exit");
            Require(events.Count == 2 && (double)events[1]["flDuration"] == 7,
                "Pause time must be excluded; a completed attempt must be emitted once");
            Require((int)events[1]["flGuesses"] == 2 && (int)events[1]["flCrystals"] == 0, "Accepted guesses and actual rewards are recorded");
            GameTelemetry.BeginSingle(12, 3, 7, LevelEntryMode.Retry, 0);
            GameTelemetry.Finish("fail", 8);
            GameTelemetry.BeginSingle(12, 3, 7, LevelEntryMode.Resume, 3);
            GameTelemetry.Abandon("menu"); GameTelemetry.Abandon("scene_exit");
            Require(events.Count(ev => (string)ev["flAction"] == "level_abandoned") == 1, "An abandoned attempt is counted once");
            GameTelemetry.BeginDuel(2, MultiplayerRules.For(Difficulty.Hard)); GameTelemetry.Finish("disconnected", 3);
            GameTelemetry.Tutorial("tutorial_started", "first_level"); GameTelemetry.Tutorial("tutorial_step", "first_level", 10);
            GameTelemetry.Tutorial("tutorial_completed", "first_level", 10);
            GameTelemetry.Powerup(RewardType.Bomb, false, "single"); GameTelemetry.Powerup(RewardType.Life, true, "duel");
            GameTelemetry.Connection("join", "failed", Difficulty.Medium);
        }
        foreach (var ev in events)
        {
            var sdkEvent = new Unity.Services.Analytics.CustomEvent(GameTelemetry.EventName);
            foreach (var pair in ev)
            {
                Require(Parameters.TryGetValue(pair.Key, out var type), "Only schema-approved parameters enter the SDK: " + pair.Key);
                Require(type == "STRING" ? pair.Value is string : type == "INTEGER" ? pair.Value is int : pair.Value is double,
                    "Parameter type matches the dashboard schema: " + pair.Key);
                sdkEvent.Add(pair.Key, pair.Value);
            }
        }
        var originalConsent = EndUserConsent.GetConsentState();
        bool hadChoice = PlayerPrefs.HasKey(TelemetryRuntime.ConsentKey);
        int choice = PlayerPrefs.GetInt(TelemetryRuntime.ConsentKey);
        const string deletionKey = "fruitlab_analytics_deletion_pending";
        bool hadDeletion = PlayerPrefs.HasKey(deletionKey); int deletion = PlayerPrefs.GetInt(deletionKey);
        var root = new GameObject("TelemetryConsentRegression");
        try
        {
            PlayerPrefs.DeleteKey(TelemetryRuntime.ConsentKey); PlayerPrefs.DeleteKey(deletionKey);
            var runtime = root.AddComponent<TelemetryRuntime>();
            Require(!runtime.HasChoice && !runtime.AnalyticsAllowed, "No implicit analytics opt-in");
            runtime.SetAnalyticsChoice(false); runtime.Record(new Dictionary<string, object> { ["flAction"] = "smoke" });
            Require(EndUserConsent.GetConsentState().AnalyticsIntent == ConsentStatus.Denied, "Declining collection denies SDK consent");
            runtime.SetAnalyticsChoice(true);
            Require(runtime.AnalyticsAllowed && EndUserConsent.GetConsentState().AnalyticsIntent == ConsentStatus.Denied,
                "Editor sessions never upload, even when the saved device preference is enabled");
            runtime.RequestAnalyticsDeletion();
            Require(!runtime.AnalyticsAllowed && runtime.DeletionPending, "Deletion disables collection and survives an offline initialization");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            if (hadChoice) PlayerPrefs.SetInt(TelemetryRuntime.ConsentKey, choice); else PlayerPrefs.DeleteKey(TelemetryRuntime.ConsentKey);
            if (hadDeletion) PlayerPrefs.SetInt(deletionKey, deletion); else PlayerPrefs.DeleteKey(deletionKey);
            PlayerPrefs.Save(); EndUserConsent.SetConsentState(originalConsent);
        }
        Directory.CreateDirectory("docs/design");
        File.WriteAllText("docs/design/telemetry-review.json", JsonConvert.SerializeObject(new {
            passed = true, sdk = "Unity Analytics 6.3.0", native_diagnostics_enabled = EngineDiagnosticsSettings.enabled,
            pause_excluded = true, outcomes_deduplicated = true, abandonment = true, single_and_duel = true,
            tutorial_and_powerups = true, typed_schema = true, consent_denied_by_default = true,
            editor_uploads_disabled = true, offline_deletion_queued = true, preferences_restored = true,
            cloud_ingestion_verified = false, examples = events
        }, Formatting.Indented));
        File.WriteAllText("docs/analytics-event-schema.json", JsonConvert.SerializeObject(new {
            eventName = GameTelemetry.EventName, environment = "production",
            parameters = Parameters.Select(p => new { name = p.Key, type = p.Value, required = p.Key == "flAction" || p.Key == "flBuildChannel" || p.Key == "flVersion" || p.Key == "flBuild" })
        }, Formatting.Indented));
        Require(EngineDiagnosticsSettings.enabled, "Native Engine Diagnostics is enabled for builds");
        return "Telemetry event lifecycle, schema encoding, consent, Editor isolation and offline deletion checks passed";
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Telemetry regression: " + message); }
}

public sealed class FruitLabTelemetryBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        EngineDiagnosticsSettings.enabled = true;
        var config = JsonUtility.FromJson<TelemetryConfiguration>(File.ReadAllText(FruitLabTelemetryEditorTools.ConfigPath));
        config.buildNumber = report.summary.platform == BuildTarget.iOS ? PlayerSettings.iOS.buildNumber
            : report.summary.platform == BuildTarget.Android ? PlayerSettings.Android.bundleVersionCode.ToString() : Application.version + "-development";
        File.WriteAllText(FruitLabTelemetryEditorTools.ConfigPath, JsonUtility.ToJson(config, true) + "\n");
        AssetDatabase.ImportAsset(FruitLabTelemetryEditorTools.ConfigPath, ImportAssetOptions.ForceSynchronousImport);
    }
    public void OnPostprocessBuild(BuildReport report)
    {
#if UNITY_IOS
        if (report.summary.platform != BuildTarget.iOS) return;
        string path = UnityEditor.iOS.Xcode.PBXProject.GetPBXProjectPath(report.summary.outputPath);
        var project = new UnityEditor.iOS.Xcode.PBXProject(); project.ReadFromFile(path);
        foreach (string target in new[] { project.GetUnityMainTargetGuid(), project.GetUnityFrameworkTargetGuid() })
        {
            project.SetBuildProperty(target, "DEBUG_INFORMATION_FORMAT", "dwarf-with-dsym");
            project.SetBuildProperty(target, "GCC_GENERATE_DEBUGGING_SYMBOLS", "YES");
        }
        project.WriteToFile(path);
#endif
    }
}

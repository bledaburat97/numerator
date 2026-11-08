using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Scripts;
using Scripts.Economy;
using UnityEditor;
using UnityEditor.EngineDiagnostics;
using UnityEngine;

public static class FruitLabReleaseEditorTools
{
    public static string Review()
    {
        string folder="Library/FruitLabEditor/ReleaseTests/"+Guid.NewGuid().ToString("N");
        var examples=FruitLabEconomyReleaseChecks.Review(folder,PlayerProfileRuntime.LoadSettings());
        foreach(var item in examples)
        {
            var sdk=new Unity.Services.Analytics.CustomEvent(ProfileTelemetryJournal.EventName);
            foreach(var pair in item.Parameters())
            {
                Require(FruitLabTelemetryEditorTools.EconomyParameters.TryGetValue(pair.Key,out string type),"Undefined economy parameter: "+pair.Key);
                Require(type=="STRING"?pair.Value is string:type=="BOOLEAN"?pair.Value is bool:pair.Value is int || pair.Value is long,"Schema type mismatch: "+pair.Key);
                sdk.Add(pair.Key,pair.Value);
            }
            Require(!item.Parameters().ContainsKey("ConsentScope") && !item.Parameters().ContainsKey("RoundId"),"Local operation and consent identities must not enter analytics");
        }
        Directory.CreateDirectory("docs/design");
        File.WriteAllText("docs/economy-analytics-event-schema.json",JsonConvert.SerializeObject(new {eventName=ProfileTelemetryJournal.EventName,environment="production",
            parameters=FruitLabTelemetryEditorTools.EconomyParameters.Select(pair=>new {name=pair.Key,type=pair.Value,required=new[]{"flAction","flBuildChannel","flVersion","flBuild","flEventId","flUtcDay"}.Contains(pair.Key)})},Formatting.Indented));
        var config=JsonUtility.FromJson<TelemetryConfiguration>(File.ReadAllText(FruitLabTelemetryEditorTools.ConfigPath));
        bool privacy=Uri.TryCreate(config.privacyPolicyUrl,UriKind.Absolute,out var uri) && uri.Scheme=="https";
        bool portraits=PlayerSettings.defaultInterfaceOrientation==UIOrientation.Portrait || (PlayerSettings.defaultInterfaceOrientation==UIOrientation.AutoRotation && PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToLandscapeLeft && !PlayerSettings.allowedAutorotateToLandscapeRight && !PlayerSettings.allowedAutorotateToPortraitUpsideDown);
        string[] expected={"Menu","Game","Lobby","WaitingScene"};var scenes=EditorBuildSettings.scenes.Where(scene=>scene.enabled).Select(scene=>Path.GetFileNameWithoutExtension(scene.path)).ToArray();
        bool requiredScenes=expected.All(scenes.Contains);
        var warnings=new System.Collections.Generic.List<string>();
        if(LevelPlaytestSchedule.Settings.Enabled)warnings.Add("Playtest level cycle is enabled; final level pacing has not been applied.");
        if(!privacy)warnings.Add("Public app privacy policy URL is missing.");
        if(config.buildChannel!="production")warnings.Add("Analytics channel is beta; keep for TestFlight, switch for public production.");
        if(!portraits)warnings.Add("Portrait orientation settings need review.");
        if(!requiredScenes)warnings.Add("Required game scenes are missing from the enabled build scene list.");
        Require(EngineDiagnosticsSettings.enabled,"Engine Diagnostics must remain enabled");
        var report=new {passed=true,profile_version=PlayerProfileData.CurrentVersion,typed_sdk_events=true,
            outbox_bounded=ProfileTelemetryJournal.Capacity,consent_scoped=true,original_build_preserved=true,
            retry_event_ids_stable=true,exactly_once_rewards=true,source_and_activity_days=true,
            checks=new[]{"planting day", "same UTC day", "missed days advance once", "fourth visit harvest", "duplicate/parallel purchase", "insufficient balance", "one million seed single batch", "atomic transaction interruption", "delivery acknowledgement interruption", "disk restart", "legacy version 3 migration", "consent off/re-enable", "bounded queue and activity checkpoints", "Int64/boolean SDK encoding"},
            portrait=portraits,required_scenes=requiredScenes,diagnostics=true,configuration_ready=warnings.Count==0,public_store_ready=false,warnings,
            physical_device_verified=false,event_browser_verified=false,artifacts=folder,
            examples=examples.Select(item=>item.Parameters())};
        File.WriteAllText("Library/FruitLabEditor/economy-release-review.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return "Economy release checks and SDK encoding passed. Public release configuration has "+warnings.Count+" pending item(s); see economy-release-review.json.";
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Release check: "+message);}
}

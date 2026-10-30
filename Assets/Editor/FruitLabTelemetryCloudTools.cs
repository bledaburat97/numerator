using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine.Networking;

// Editor-only administration. Uses the existing Unity login in memory; never stores a token.
public static class FruitLabTelemetryCloudTools
{
    private static string _gatewayToken;
    public static async void Inspect()
    {
        object report;
        try
        {
            string org = CloudProjectSettings.organizationKey, project = CloudProjectSettings.projectId;
            var metadata = await Request("GET", $"/api/live-ops/accounts/v1/organizations/{org}/projects/{project}");
            report = new { connected = true, project_id = project, organization = org,
                name = (string)metadata["name"], fields = ((JObject)metadata).Properties().Select(p => p.Name).ToArray(),
                analytics_enabled = (bool?)metadata["analyticsEnabled"],
                environments = metadata["environments"]?.Select(e => new { unity_id = (string)e["unityId"],
                    type = (string)e["type"], analytics_ready = (bool?)e["analyticsReady"] }) };
        }
        catch (Exception error) { report = new { connected = false, error = error.Message }; }
        Directory.CreateDirectory("Library/FruitLabEditor");
        File.WriteAllText("Library/FruitLabEditor/telemetry-cloud-inspect.json", JsonConvert.SerializeObject(report, Formatting.Indented));
    }
    public static async void Provision()
    {
        object report;
        try
        {
            string org = CloudProjectSettings.organizationKey, project = CloudProjectSettings.projectId;
            var metadata = await Request("GET", $"/api/live-ops/accounts/v1/organizations/{org}/projects/{project}");
            if ((string)metadata["name"] != CloudProjectSettings.projectName)
                throw new Exception("Connected project name does not match the Editor; no schema was changed.");
            if ((bool?)metadata["analyticsEnabled"] != true)
                throw new Exception("Analytics must first be enabled by the project owner in Unity Dashboard > Analytics > Get started.");
            var environment = metadata["environments"].First(e => (string)e["type"] == "LIVE");
            string env = (string)environment["unityId"];
            string prefix = $"/api/live-ops/events/v3/organizations/{org}/projects/{project}/environments/{env}";
            var schemas = (JArray)await Request("GET", prefix + "/schemas");
            var existing = schemas.FirstOrDefault(e => (string)e["name"] == Scripts.GameTelemetry.EventName);
            int created = 0;
            if (existing == null)
            {
                var standard = await Request("GET", prefix + "/schemas/gameStarted");
                var parameters = (JArray)await Request("GET", prefix + "/parameters");
                // Fetch/diff before writes; only add our names, never edit an existing definition.
                foreach (var field in FruitLabTelemetryEditorTools.Parameters)
                {
                    var parameter = parameters.FirstOrDefault(p => (string)p["name"] == field.Key);
                    if (parameter != null && (string)parameter["type"] != field.Value)
                        throw new Exception("Existing analytics parameter type differs: " + field.Key + ". No definition was overwritten.");
                }
                foreach (var field in FruitLabTelemetryEditorTools.Parameters)
                {
                    if (parameters.Any(p => (string)p["name"] == field.Key)) continue;
                    await Request("POST", prefix + "/parameters", new { name = field.Key,
                        description = "Fruit Lab gameplay " + field.Key, type = field.Value, isRestricted = false });
                    created++;
                }
                var roots = new JArray(standard["parameters"].Select(Reference));
                var children = (JArray)roots.First(p => (string)p["name"] == "eventParams")["children"];
                foreach (string name in FruitLabTelemetryEditorTools.Parameters.Keys)
                    children.Add(new JObject { ["name"] = name, ["isRequired"] = name == "flAction" || name == "flBuildChannel" || name == "flVersion" || name == "flBuild", ["children"] = new JArray() });
                await Request("POST", prefix + "/schemas", new { name = Scripts.GameTelemetry.EventName,
                    description = "Tutorial, level, powerup and duel gameplay; no names or room codes.", parameters = roots, isEnabled = true });
            }
            var verified = await Request("GET", prefix + "/schemas/" + Scripts.GameTelemetry.EventName);
            var leaves = verified["parameters"].SelectMany(p => p["children"] ?? new JArray()).Select(p => (string)p["name"]).ToArray();
            if (!FruitLabTelemetryEditorTools.Parameters.Keys.All(leaves.Contains) || (bool?)verified["isEnabled"] != true)
                throw new Exception("An existing event is incomplete or disabled. No existing schema was overwritten.");
            report = new { passed = true, project = (string)metadata["name"], environment = "production",
                event_name = Scripts.GameTelemetry.EventName, parameters_created = created, already_existed = existing != null,
                schema_verified = true, existing_definitions_preserved = true };
        }
        catch (Exception error) { report = new { passed = false, error = error.Message }; }
        Directory.CreateDirectory("docs/design");
        File.WriteAllText("docs/design/telemetry-cloud-setup.json", JsonConvert.SerializeObject(report, Formatting.Indented));
    }
    private static JObject Reference(JToken parameter) => new JObject {
        ["name"] = parameter["name"], ["isRequired"] = parameter["isRequired"] ?? false,
        ["children"] = new JArray((parameter["children"] ?? new JArray()).Select(Reference))
    };
    public static async Task<JToken> Request(string method, string path, object body = null, bool authenticate = true)
    {
        if (authenticate && string.IsNullOrEmpty(_gatewayToken))
        {
            var exchange = await Request("POST", "/api/auth/v1/genesis-token-exchange/unity",
                new { token = CloudProjectSettings.accessToken }, false);
            _gatewayToken = (string)exchange["token"];
            if (string.IsNullOrEmpty(_gatewayToken)) throw new Exception("Unity Cloud session could not be authorized.");
        }
        using (var request = new UnityWebRequest("https://services.unity.com" + path, method))
        {
            request.timeout = 20; request.downloadHandler = new DownloadHandlerBuffer();
            if (authenticate) request.SetRequestHeader("Authorization", "Bearer " + _gatewayToken);
            request.SetRequestHeader("Accept", "application/json");
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            var operation = request.SendWebRequest(); while (!operation.isDone) await Task.Yield();
            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception("Unity Analytics administration returned HTTP " + request.responseCode + ". Dashboard activation or a Unity Cloud login may be required.");
            return string.IsNullOrEmpty(request.downloadHandler.text) ? new JObject() : JToken.Parse(request.downloadHandler.text);
        }
    }
}

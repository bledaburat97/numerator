using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game;
using Menu;
using Scripts;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Unity.Netcode;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FruitLabMultiplayerEditorTools
{
    private const string Folder = "Library/FruitLabEditor";
    private const string ProbeSceneKey = "FruitLab.MultiplayerProbeSceneSetup";
    [Serializable] private class ProbeSceneEntry { public string path; public bool loaded, active; }
    [Serializable] private class ProbeSceneSetup { public ProbeSceneEntry[] scenes; }
    static FruitLabMultiplayerEditorTools()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredEditMode || !File.Exists(MultiplayerNetworkProbe.EditorConfigPath)) return;
            File.Delete(MultiplayerNetworkProbe.EditorConfigPath);
            string saved = SessionState.GetString(ProbeSceneKey, ""); SessionState.EraseString(ProbeSceneKey);
            if (!string.IsNullOrEmpty(saved)) EditorSceneManager.RestoreSceneManagerSetup(JsonUtility.FromJson<ProbeSceneSetup>(saved).scenes.Select(entry => new SceneSetup { path = entry.path, isLoaded = entry.loaded, isActive = entry.active }).ToArray());
        };
        EditorApplication.update += () => {
            string stop = Folder + "/editor-multiplayer-probe-stop.request";
            if (!File.Exists(stop)) return;
            File.Delete(stop);
            if (File.Exists(MultiplayerNetworkProbe.EditorConfigPath) && EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        };
    }
    public static string StartEditorProbe()
    {
        Require(File.Exists(MultiplayerNetworkProbe.EditorConfigPath), "An explicit editor probe configuration exists");
        SessionState.SetString(ProbeSceneKey, JsonUtility.ToJson(new ProbeSceneSetup { scenes = EditorSceneManager.GetSceneManagerSetup().Select(scene => new ProbeSceneEntry { path = scene.path, loaded = scene.isLoaded, active = scene.isActive }).ToArray() }));
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
        return "Started the real Unity Editor as a multiplayer validation peer";
    }
    public static string ConfigureBuild()
    {
        foreach (var target in new[] { UnityEditor.Build.NamedBuildTarget.Standalone, UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.Build.NamedBuildTarget.iOS })
        {
            string defines = PlayerSettings.GetScriptingDefineSymbols(target);
            if (!defines.Split(';').Contains("URP_COMPATIBILITY_MODE")) PlayerSettings.SetScriptingDefineSymbols(target, defines + ";URP_COMPATIBILITY_MODE");
        }
        return "Explicit URP compatibility enabled for Standalone, Android and iOS";
    }
    public static string Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before the multiplayer bake");
        string backup = Folder + "/MultiplayerBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"); Directory.CreateDirectory(backup);
        const string rowPath = "Assets/Prefabs/ResultBlock.prefab";
        File.Copy(rowPath, backup + "/ResultBlock.prefab", true);
        var rowRoot = PrefabUtility.LoadPrefabContents(rowPath);
        try
        {
            var stripe = FruitLabStyle.Detail(rowRoot.transform, "PlayerStripe", FruitLabStyle.LocalPlayer);
            FruitLabStyle.Place(stripe.rectTransform, rowRoot.transform, 0, 6, 4, 52);
            stripe.gameObject.SetActive(false);
            var serialized = new SerializedObject(rowRoot.GetComponent<ResultBlockView>());
            serialized.FindProperty("playerStripe").objectReferenceValue = stripe;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(rowRoot, rowPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(rowRoot); }
        foreach (string name in new[] { "Menu", "Lobby", "WaitingScene", "Game" })
        {
            string path = "Assets/Scenes/" + name + ".unity"; File.Copy(path, backup + "/" + name + ".unity", true);
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                if (name == "Menu") Find<MenuUIView>(scene).GetMultiplayerButton().SetButtonStatus(true);
                else if (name != "Game")
                {
                    var canvas = Find<Canvas>(scene);
                    var scaler = canvas.GetComponent<CanvasScaler>(); scaler.referenceResolution = new Vector2(375, 667); scaler.matchWidthOrHeight = 0;
                    scaler.SendMessage("Update", SendMessageOptions.DontRequireReceiver); Canvas.ForceUpdateCanvases();
                    var layout = canvas.GetComponent<FruitLabOnlineLayout>() ?? canvas.gameObject.AddComponent<FruitLabOnlineLayout>(); layout.ApplyLayout();
                    Transform panel = (Transform)canvas.GetComponentInChildren<LobbyUIView>(true)?.transform ?? canvas.GetComponentInChildren<WaitingSceneUIView>(true).transform;
                    Transform message = canvas.GetComponentInChildren<LobbyMessagePopupView>(true)?.transform;
                    foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
                        if (!graphic.transform.IsChildOf(panel) && (message == null || !graphic.transform.IsChildOf(message))
                            && graphic.name != "LabRoom" && graphic.gameObject != canvas.gameObject) graphic.enabled = false;
                    foreach (Image image in panel.GetComponentsInChildren<Image>(true))
                        if (image.GetComponentInParent<BaseButtonView>() == null && image.name != "LabRim") FruitLabStyle.Surface(image, FruitLabStyle.Recess);
                    if (name == "Lobby")
                    {
                        var view = Find<LobbyUIView>(scene);
                        view.GetCreateLobbyButton().SetText("Düello yarat"); view.GetJoinWithCodeButton().SetText("Düelloya katıl"); view.GetMenuButton().SetText("Menü");
                    }
                    else
                    {
                        var view = Find<WaitingSceneUIView>(scene); view.SetLobbyNameText("Orta"); view.SetLobbyCodeText("------");
                        view.GetMenuButton().SetText("Menü"); view.GetReadyButton().SetText("Hazırım");
                    }
                }
                foreach (NetworkObject networkObject in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<NetworkObject>(true)))
                    networkObject.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);
                foreach (Component component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)))
                {
                    if (component == null) continue; EditorUtility.SetDirty(component);
                    if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
                EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene), "Scene saved: " + name);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        return "Saved Menu, Lobby, WaitingScene and Game network identities; backup=" + backup;
    }
    public static void Review(Canvas canvas, Camera camera)
    {
        var view = canvas.GetComponentInChildren<GameUIView>(true); view.SetUserText("Oyuncu 1"); view.SetOpponentText("Oyuncu 2");
        view.SetOpponentInfoStatus(true); view.SetMultiplayerStatus("Sıra sende", 60); view.SetCoinCounterStatus(false);
        canvas.GetComponentInChildren<LifeBarView>(true).gameObject.SetActive(false);
        canvas.GetComponentInChildren<PowerUpListHolderView>(true).gameObject.SetActive(false);
        int rowIndex = 0; foreach (var row in canvas.GetComponentsInChildren<ResultBlockView>()) row.SetPlayerIdentity(rowIndex++ % 2 == 0);
        foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        int count = canvas.GetComponentInChildren<BoardAreaView>(true).GetComponentsInChildren<BoardHolderView>(true).Length;
        Capture(camera, Folder + "/preview-multiplayer-game-" + count + ".png");
        Require(view.IsMultiplayer && canvas.GetComponentsInChildren<ResultBlockView>().Length >= 6, "Shared guess history stays visible");
        Require(view.MultiplayerTimerText == "60 sn", "A visible multiplayer countdown shows the shared deadline");
        foreach (var row in canvas.GetComponentsInChildren<ResultBlockView>())
        {
            var stripe = row.transform.Find("PlayerStripe").GetComponent<Image>();
            Require(stripe.gameObject.activeSelf && !stripe.raycastTarget && stripe.color == (row.IsLocalGuess == true ? FruitLabStyle.LocalPlayer : FruitLabStyle.OpponentPlayer), "Player stripes show local perspective without intercepting scroll input");
        }
        view.SetMultiplayerTurn("Sıra sende", 60, true);
        Require(!canvas.GetComponentsInChildren<Transform>(true).Any(t => t.name == "YourTurnNotification"), "Turn status stays in the header without covering history");
        view.SetMultiplayerTurn("Rakibin yeni oyun teklif ediyor", 0, false);
        foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        Capture(camera, Folder + "/preview-multiplayer-rematch-" + count + ".png");
        var popup = canvas.GetComponentInChildren<LevelFinishPopupView>(true);
        popup.SetMultiplayerResult(true, false, RewardType.Bomb);
        popup.SetStatus(true); popup.GetText().gameObject.SetActive(true);
        popup.SetText("Kazandın!"); popup.GetText().rectTransform.localScale = Vector3.one;
        popup.GetButton(LevelFinishButtonType.Game).SetButtonStatus(true); popup.GetButton(LevelFinishButtonType.Game).SetText("Tekrar oyna");
        popup.GetButton(LevelFinishButtonType.Menu).SetButtonStatus(true); popup.GetButton(LevelFinishButtonType.Menu).SetText("Menü");
        popup.GetButton(LevelFinishButtonType.Claim).SetButtonStatus(false);
        foreach (var group in popup.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
        Require(!((Component)popup.GetVerticalCrystalProgressView()).gameObject.activeSelf, "Multiplayer victory never displays a crystal bar");
        foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        Capture(camera, Folder + "/preview-multiplayer-victory-" + count + ".png");
        popup.SetStatus(false);
    }
    public static string RenderOnlineScenes() { RenderOnline("Lobby"); RenderOnline("WaitingScene"); return "Rendered lobby and waiting room"; }
    public static void ValidateRules()
    {
        int cases = 0;
        var modes = new List<object>();
        foreach (Difficulty difficulty in Enum.GetValues(typeof(Difficulty)))
        {
            var settings = MultiplayerRules.For(difficulty);
            var all = Permutations(settings.FruitCount, settings.BoardCount).ToArray();
            // Exhaust every easy pair; cover every medium/hard answer against 16 diverse guesses and itself.
            var guesses = difficulty == Difficulty.Easy ? all : Enumerable.Range(0, 16).Select(i => all[i * (all.Length - 1) / 15]).ToArray();
            int modeCases = 0;
            foreach (int[] answer in all)
            foreach (int[] guess in guesses.Concat(new[] { answer }).Distinct(new CardComparer()))
            {
                var match = new MultiplayerMatchState(0, 7, 1, answer, difficulty);
                Require(!match.MarkBoardReady(0, 1, 100), "One ready board cannot start the clock");
                Require(match.MarkBoardReady(7, 1, 100) && match.Deadline == 160, "Both boards start a 60-second host deadline");
                Require(match.TrySubmit(0, 1, 1, guess, 101, out var feedback, out _), "Valid active-player command accepted");
                int correct = guess.Where((fruit, i) => fruit == answer[i]).Count(); int present = guess.Count(answer.Contains);
                Require(feedback.Correct == correct && feedback.Misplaced == present - correct && feedback.Won == answer.SequenceEqual(guess), "Mode feedback and win detection match the answer");
                Require(!match.TrySubmit(0, 1, 1, guess, 102, out _, out _), "Repeated commands cannot consume another turn");
                modeCases++;
            }
            var bounds = new MultiplayerMatchState(0, 7, 1, all[0], difficulty);
            bounds.MarkBoardReady(0, 1, 0); bounds.MarkBoardReady(7, 1, 0);
            var oversized = (int[])all[0].Clone(); oversized[0] = settings.FruitCount + 1;
            Require(!bounds.TrySubmit(0, 1, 1, oversized, 1, out _, out _), "Fruit IDs outside the selected mode are rejected");
            Require(!bounds.TrySubmit(0, 1, 1, all[0].Take(settings.BoardCount - 1).ToArray(), 1, out _, out _), "Missing slots are rejected in every mode");
            Require(bounds.TrySubmit(0, 1, 1, all[0], 1, out var win, out _) && win.Won, "All slots are required for a win in every mode");
            cases += modeCases;
            modes.Add(new { difficulty = difficulty.ToString(), slots = settings.BoardCount, fruits = settings.FruitCount, answers = all.Length, feedback_cases = modeCases });
        }
        var model = new MultiplayerMatchState(0, 7, 1, new[] { 4, 1, 7 });
        bool hidden = false; try { model.RevealAnswer(); } catch (InvalidOperationException) { hidden = true; }
        Require(hidden, "The secret cannot be revealed during play");
        model.MarkBoardReady(0, 1, 10); model.MarkBoardReady(7, 1, 10);
        foreach (int[] guess in new[] { new[] { 1, 1, 2 }, new[] { 0, 1, 2 }, new[] { 1, 2, 10 }, new[] { 1, 2 }, Array.Empty<int>() })
            Require(!model.TrySubmit(0, 1, 1, guess, 11, out _, out _), "Invalid fruit shape rejected without changing turn");
        Require(!model.TrySubmit(7, 1, 1, new[] { 4, 1, 7 }, 11, out _, out _), "Out-of-turn correct answers are rejected");
        Require(!model.TrySubmit(99, 1, 1, new[] { 4, 1, 7 }, 11, out _, out _), "Unknown clients cannot submit");
        Require(!model.TrySubmit(0, 2, 1, new[] { 4, 1, 7 }, 11, out _, out _), "Stale or forged round IDs are rejected");
        Require(!model.TrySubmit(0, 1, 1, new[] { 4, 1, 7 }, 70, out _, out _) && model.CurrentPlayer == 7 && model.Turn == 2,
            "A command arriving at the deadline cannot win an expired turn");
        Require(!model.Tick(70) && model.Tick(130) && model.Turn == 3, "Each host deadline advances exactly once");
        Require(model.TrySubmit(0, 1, 3, new[] { 4, 1, 7 }, 131, out _, out _) && model.Winner == 0, "Winning command closes the match");
        Require(model.RevealAnswer().SequenceEqual(new[] { 4, 1, 7 }), "The final answer becomes available after completion");
        Require(!model.VoteForRematch(0, 1) && !model.VoteForRematch(0, 1) && !model.VoteForRematch(99, 1) && model.VoteForRematch(7, 1),
            "A rematch needs two distinct current-player votes");
        var next = new MultiplayerMatchState(0, 7, 2, new[] { 2, 5, 6 }); next.MarkBoardReady(0, 2, 0); next.MarkBoardReady(7, 2, 0);
        Require(next.CurrentPlayer == 7, "Rematches alternate the starting player"); next.Disconnect();
        Require(!next.Tick(200) && !next.VoteForRematch(0, 2), "Disconnect finishes play and prevents rematch");
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/multiplayer-rules-review.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
            passed = true, feedback_cases = cases, modes, checks = new[] { "host-only answer", "two-board readiness barrier", "authoritative 60-second deadlines", "sender/turn/round validation", "invalid and duplicate commands", "winner", "two-vote rematch", "alternating starter", "disconnect" }
        }, Newtonsoft.Json.Formatting.Indented));
        Debug.Log("FRUIT_LAB_MULTIPLAYER_RULES_PASS: " + cases + " feedback cases across three modes and lifecycle guards.");
    }
    private static IEnumerable<int[]> Permutations(int fruits, int slots, int[] prefix = null)
    {
        prefix = prefix ?? Array.Empty<int>();
        if (prefix.Length == slots) { yield return prefix; yield break; }
        for (int fruit = 1; fruit <= fruits; fruit++)
            if (!prefix.Contains(fruit))
                foreach (var value in Permutations(fruits, slots, prefix.Concat(new[] { fruit }).ToArray())) yield return value;
    }
    private sealed class CardComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] a, int[] b) => a.SequenceEqual(b);
        public int GetHashCode(int[] cards) { int hash = 17; foreach (int card in cards) hash = hash * 31 + card; return hash; }
    }
    public static string BuildIos()
    {
        Require(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS), "iOS Build Support must be installed for this Unity editor");
        string existing = "Builds/Unity-iPhone.xcodeproj/project.pbxproj";
        if (File.Exists(existing))
        {
            var team = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(existing), @"DEVELOPMENT_TEAM = ""?([A-Z0-9]{10})");
            if (team.Success) { PlayerSettings.iOS.appleDeveloperTeamID = team.Groups[1].Value; PlayerSettings.iOS.appleEnableAutomaticSigning = true; }
        }
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Lobby.unity", "Assets/Scenes/WaitingScene.unity", "Assets/Scenes/Game.unity" },
            locationPathName = "Builds/iOS", target = BuildTarget.iOS, options = BuildOptions.Development });
        Require(report != null && report.summary.result == BuildResult.Succeeded, "iOS Xcode export succeeded");
        return "Builds/iOS/Unity-iPhone.xcodeproj (fresh export; existing Xcode project preserved)";
    }
    public static string Build()
    {
        Require(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX), "macOS player support is available");
        string output = "Builds/MultiplayerValidation/NumeratorMultiplayer.app";
        var options = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Lobby.unity", "Assets/Scenes/WaitingScene.unity", "Assets/Scenes/Game.unity" },
            locationPathName = output, target = BuildTarget.StandaloneOSX, options = BuildOptions.Development };
        var report = BuildPipeline.BuildPlayer(options);
        Require(report != null, "Build pipeline returned no report; inspect the Unity Editor log");
        Require(report.summary.result == BuildResult.Succeeded, "Development player build: " + report.summary.result + ", errors=" + report.summary.totalErrors);
        return output;
    }
    private static void RenderOnline(string name)
    {
        var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RenderTexture target = null;
        try
        {
            var clone = Object.Instantiate(Find<Canvas>(source).gameObject); SceneManager.MoveGameObjectToScene(clone, scene);
            var canvas = clone.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)clone.transform; rect.localScale = Vector3.one; rect.position = Vector3.zero; rect.sizeDelta = new Vector2(375, 667);
            clone.GetComponent<FruitLabOnlineLayout>().ApplyLayout();
            var popup = clone.GetComponentInChildren<LobbyMessagePopupView>(true); if (popup != null) popup.gameObject.SetActive(false);
            foreach (Transform child in clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            var root = new GameObject("OnlineCapture", typeof(Camera)); SceneManager.MoveGameObjectToScene(root, scene);
            var camera = root.GetComponent<Camera>(); camera.enabled = false; camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.orthographic = true; camera.orthographicSize = 333.5f; camera.aspect = 375f / 667; camera.nearClipPlane = 1; camera.farClipPlane = 2000;
            camera.transform.position = new Vector3(0, 0, -1000); camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FruitLabStyle.Background; canvas.worldCamera = camera;
            target = new RenderTexture(750, 1334, 24); target.Create(); camera.targetTexture = target;
            if (name == "WaitingScene")
            {
                var names = clone.GetComponentInChildren<PlayerNameAreaView>(true);
                for (int i = 0; i < 2; i++)
                {
                    var row = (PlayerNameView)names.CreatePlayerNameView(); row.Init(i, _ => { });
                    row.SetPlayerName(i == 0 ? "Oyuncu 1" : "Oyuncu 2"); row.SetRole(i == 0, i == 0, i == 1); row.SetReadyStatus(i == 0);
                }
            }
            foreach (Transform child in clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            Capture(camera, Folder + "/preview-multiplayer-" + name.ToLowerInvariant() + ".png");
            if (name == "Lobby")
            {
                var view = clone.GetComponentInChildren<LobbyUIView>(true);
                Require(!((Component)view.GetDifficultyButton(0)).gameObject.activeSelf && !view.CodeInput.gameObject.activeSelf, "Entry options start collapsed");
                view.ShowCreateOptions();
                Require(((Component)view.GetDifficultyButton(0)).gameObject.activeSelf && !view.CodeInput.gameObject.activeSelf, "Create expands difficulty options only");
                Capture(camera, Folder + "/preview-multiplayer-lobby-create.png");
                view.ShowJoinCodeInput();
                Require(view.CodeInput.gameObject.activeSelf && !((Component)view.GetDifficultyButton(0)).gameObject.activeSelf, "Join expands only the code input");
                Capture(camera, Folder + "/preview-multiplayer-lobby-join.png");
            }
        }
        finally { if (target != null) { target.Release(); Object.DestroyImmediate(target); } EditorSceneManager.CloseScene(scene, true); EditorSceneManager.ClosePreviewScene(source); }
    }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).First();
    private static void Capture(Camera camera, string output)
    {
        Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = camera.targetTexture });
        var before = RenderTexture.active; RenderTexture.active = camera.targetTexture;
        var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
        try { image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply(); File.WriteAllBytes(output, image.EncodeToPNG()); }
        finally { Object.DestroyImmediate(image); RenderTexture.active = before; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Multiplayer check: " + message); }
}

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
                    var settingsData = new SerializedObject(layout);
                    settingsData.FindProperty("settingsPopupPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SettingsPopupView>("Assets/Prefabs/SettingsPopup.prefab");
                    settingsData.ApplyModifiedPropertiesWithoutUndo();
                    Transform panel = (Transform)canvas.GetComponentInChildren<LobbyUIView>(true)?.transform ?? canvas.GetComponentInChildren<WaitingSceneUIView>(true).transform;
                    Transform message = canvas.GetComponentInChildren<LobbyMessagePopupView>(true)?.transform;
                    Transform settings = (Transform)(canvas.GetComponentInChildren<LobbyUIView>(true) != null
                        ? canvas.GetComponentInChildren<LobbyUIView>(true).GetMenuButton().GetButtonRectTransform()
                        : canvas.GetComponentInChildren<WaitingSceneUIView>(true).GetMenuButton().GetButtonRectTransform());
                    foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
                        if (!graphic.transform.IsChildOf(panel) && (message == null || !graphic.transform.IsChildOf(message))
                            && !graphic.transform.IsChildOf(settings)
                            && graphic.name != "LabRoom" && graphic.gameObject != canvas.gameObject) graphic.enabled = false;
                    if (name == "Lobby")
                    {
                        var view = Find<LobbyUIView>(scene);
                        view.GetCreateLobbyButton().SetText("Oyun Oluştur"); view.GetJoinWithCodeButton().SetText("Oyuna Katıl"); view.HideDialogs();
                    }
                    else
                    {
                        var view = Find<WaitingSceneUIView>(scene); view.SetLobbyNameText("Orta"); view.SetLobbyCodeText("------");
                        view.GetReadyButton().SetText("Hazırım"); view.SetStartAvailability(true, false);
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
        int rowIndex = 0;
        foreach (var row in canvas.GetComponentsInChildren<ResultBlockView>())
        {
            int player = rowIndex++ % 4;
            row.SetPlayerIdentity(player == 0, "Oyuncu " + (player + 1));
        }
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
        popup.GetButton(LevelFinishButtonType.Menu).SetButtonStatus(false);
        popup.GetButton(LevelFinishButtonType.Claim).SetButtonStatus(false);
        foreach (var group in popup.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
        Require(!((Component)popup.GetVerticalCrystalProgressView()).gameObject.activeSelf, "Multiplayer victory never displays a crystal bar");
        foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        Capture(camera, Folder + "/preview-multiplayer-victory-" + count + ".png");
        popup.SetStatus(false);
    }
    public static string RenderOnlineScenes()
    {
        foreach (float height in new[] { 580f, 667f, 812f }) { RenderOnline("Lobby", height); RenderOnline("WaitingScene", height); }
        return "Rendered entry/create/join dialogs and four-player lobby at 375x580, 375x667 and 375x812";
    }
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
        foreach (int count in new[] { 3, 4 })
        foreach (Difficulty difficulty in Enum.GetValues(typeof(Difficulty)))
        {
            var players = new ulong[] { 0, 7, 31, 79 }.Take(count).ToArray();
            int slots = MultiplayerRules.For(difficulty).BoardCount;
            var answer = new[] { 4, 1, 7, 2, 5 }.Take(slots).ToArray();
            var wrong = Enumerable.Range(1, slots).ToArray();
            var group = new MultiplayerMatchState(players, 1, answer, difficulty);
            for (int i = 0; i < count; i++)
                Require(group.MarkBoardReady(players[i], 1, 100) == (i == count - 1), "Every connected board must be ready");
            for (int turn = 1; turn <= count * 2; turn++)
            {
                ulong player = players[(turn - 1) % count];
                Require(group.CurrentPlayer == player && group.TrySubmit(player, 1, turn, wrong, 100 + turn, out _, out _),
                    "Three/four players rotate in join order without skipped or repeated turns");
            }
            Require(group.Tick(group.Deadline) && group.CurrentPlayer == players[1], "Timeout advances to the next group participant");
            double deadline = group.Deadline;
            group.RemovePlayer(players[count - 1], 200);
            Require(group.Phase == MultiplayerPhase.Playing && group.Deadline == deadline, "A non-current guest departure preserves the active clock");
            if (count == 4)
            {
                group.RemovePlayer(players[1], 201);
                Require(group.CurrentPlayer == players[2] && group.Deadline == 261 && group.Players.Count == 2,
                    "A departing current guest passes its turn to the next connected player");
            }
            group.RemovePlayer(group.Players.First(player => player != 0), 202);
            Require(group.Phase == MultiplayerPhase.Disconnected, "A single remaining player cannot continue");
            var rematch = new MultiplayerMatchState(players, 1, answer, difficulty);
            foreach (ulong player in players) rematch.MarkBoardReady(player, 1, 0);
            Require(rematch.TrySubmit(0, 1, 1, answer, 1, out _, out _), "Group victory accepted");
            for (int i = 0; i < count; i++)
                Require(rematch.VoteForRematch(players[i], 1) == (i == count - 1), "A group rematch requires every current participant");
            var preparation = new MultiplayerMatchState(players, 1, answer, difficulty);
            foreach (ulong player in players.Take(count - 1)) preparation.MarkBoardReady(player, 1, 0);
            preparation.RemovePlayer(players[count - 1], 5);
            Require(preparation.Phase == MultiplayerPhase.Playing, "A departing unready guest cannot block the remaining boards");
            preparation.RemovePlayer(0, 6);
            Require(preparation.Phase == MultiplayerPhase.Disconnected, "The host leaving ends the whole match");
            var preparingRematch = new MultiplayerMatchState(players, 2, answer, difficulty);
            foreach (ulong player in players.Where(player => player != players[1])) preparingRematch.MarkBoardReady(player, 2, 0);
            preparingRematch.RemovePlayer(players[1], 5);
            Require(preparingRematch.Phase == MultiplayerPhase.Playing && preparingRematch.CurrentPlayer == players[2]
                && preparingRematch.Deadline == 65, "A rematch starter leaving during board setup passes its turn before the clock starts");
        }
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/multiplayer-rules-review.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
            passed = true, feedback_cases = cases, modes, player_counts = new[] { 2, 3, 4 }, group_lifecycle_checked = true, checks = new[] { "host-only answer", "all-player board readiness barrier", "authoritative 60-second deadlines", "sender/turn/round validation", "invalid and duplicate commands", "winner", "all-player rematch", "alternating starter", "disconnect" }
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
    public static string BuildIos(bool development = true)
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
            locationPathName = "Builds/iOS", target = BuildTarget.iOS, options = development ? BuildOptions.Development : BuildOptions.None });
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
    private static void RenderOnline(string name, float phoneHeight = 667)
    {
        var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RenderTexture target = null;
        try
        {
            var clone = Object.Instantiate(Find<Canvas>(source).gameObject); SceneManager.MoveGameObjectToScene(clone, scene);
            var canvas = clone.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)clone.transform; rect.localScale = Vector3.one; rect.position = Vector3.zero; rect.sizeDelta = new Vector2(375, phoneHeight);
            clone.GetComponent<FruitLabOnlineLayout>().ApplyLayout();
            var popup = clone.GetComponentInChildren<LobbyMessagePopupView>(true); if (popup != null) popup.gameObject.SetActive(false);
            foreach (Transform child in clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            var root = new GameObject("OnlineCapture", typeof(Camera)); SceneManager.MoveGameObjectToScene(root, scene);
            var camera = root.GetComponent<Camera>(); camera.enabled = false; camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.orthographic = true; camera.orthographicSize = phoneHeight * .5f; camera.aspect = 375f / phoneHeight; camera.nearClipPlane = 1; camera.farClipPlane = 2000;
            camera.transform.position = new Vector3(0, 0, -1000); camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FruitLabStyle.Background; canvas.worldCamera = camera;
            target = new RenderTexture(750, Mathf.RoundToInt(phoneHeight * 2), 24); target.Create(); camera.targetTexture = target;
            if (name == "WaitingScene")
            {
                var names = clone.GetComponentInChildren<PlayerNameAreaView>(true);
                for (int i = 0; i < MultiplayerRules.MaxPlayers; i++)
                {
                    var row = (PlayerNameView)names.CreatePlayerNameView(); row.Init(i, _ => { });
                    row.SetPlayerName("Oyuncu " + (i + 1)); row.SetRole(i == 0, i == 0, i > 0); row.SetReadyStatus(i != 3);
                }
                clone.GetComponentInChildren<WaitingSceneUIView>(true).SetStartAvailability(true, false);
                clone.GetComponentInChildren<WaitingSceneUIView>(true).SetStatus("Tüm oyuncuların hazır olması bekleniyor.");
            }
            Canvas.ForceUpdateCanvases();
            foreach (var button in clone.GetComponentsInChildren<BaseButtonView>())
            {
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rect, button.GetButtonRectTransform());
                Require(rect.rect.Contains(bounds.min) && rect.rect.Contains(bounds.max), "Online controls stay on screen at " + phoneHeight + ": " + button.name);
            }
            if (name == "WaitingScene")
            {
                Require(clone.GetComponentInChildren<WaitingSceneUIView>(true).transform.Find("OnlineTitle").GetComponent<TMPro.TMP_Text>().alignment
                    == TMPro.TextAlignmentOptions.Midline, "Lobby heading is centered");
                var names = clone.GetComponentInChildren<PlayerNameAreaView>(true).transform as RectTransform;
                foreach (var row in names.GetComponentsInChildren<PlayerNameView>())
                {
                    var corners = new Vector3[4]; ((RectTransform)row.transform).GetWorldCorners(corners);
                    var bounds = new Rect(names.rect.xMin - .1f, names.rect.yMin - .1f, names.rect.width + .2f, names.rect.height + .2f);
                    Require(corners.All(corner => bounds.Contains(names.InverseTransformPoint(corner))), "All four player rows fit the lobby at " + phoneHeight);
                }
            }
            foreach (Transform child in clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            Capture(camera, Folder + "/preview-multiplayer-" + name.ToLowerInvariant() + (phoneHeight == 667 ? "" : "-" + phoneHeight) + ".png");
            if (name == "Lobby")
            {
                var view = clone.GetComponentInChildren<LobbyUIView>(true);
                Require(view.NameInput.transform.parent.name == "ProfileSection" && ((Component)view.GetCreateLobbyButton()).transform.parent.name == "GameSection",
                    "Profile input and game actions belong to separate panels");
                Require(view.NameInput.placeholder.gameObject.activeInHierarchy && view.NameInput.placeholder.enabled,
                    "An empty name shows its placeholder");
                Require(!view.CreatePopup.gameObject.activeSelf && !view.JoinPopup.gameObject.activeSelf, "Entry dialogs start closed");
                view.ShowCreateOptions();
                Require(view.CreatePopup.gameObject.activeInHierarchy && !view.JoinPopup.gameObject.activeSelf, "Create shows a separate difficulty dialog");
                Capture(camera, Folder + "/preview-multiplayer-lobby-create" + (phoneHeight == 667 ? "" : "-" + phoneHeight) + ".png");
                view.ShowJoinCodeInput();
                Require(view.JoinPopup.gameObject.activeInHierarchy && !view.CreatePopup.gameObject.activeSelf, "Join shows a separate code dialog");
                view.SetJoinError("Giriş yapılamadı, kodu tekrar deneyiniz.");
                Capture(camera, Folder + "/preview-multiplayer-lobby-join" + (phoneHeight == 667 ? "" : "-" + phoneHeight) + ".png");
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

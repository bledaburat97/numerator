using System;
using System.IO;
using System.Linq;
using DG.Tweening;
using Game;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// A local, file-based editor task queue. No network or arbitrary code execution.
// Uses the user's already-running editor rather than starting another licensed instance.
[InitializeOnLoad]
public static class FruitLabEditorTools
{
    private const string Folder = "Library/FruitLabEditor";
    [Serializable] private class Job { public string id; public string operation; public int board = 5; public int fruits = 9; }
    [Serializable] private class Reply { public int revision = 5; public string id; public bool ok; public string details; public string error; }
    private static bool _busy;
    private static double _nextCheck;

    static FruitLabEditorTools() => EditorApplication.update += Poll;

    private static void Poll()
    {
        if (_busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < _nextCheck) return;
        _nextCheck = EditorApplication.timeSinceStartup + .5;
        string refresh = Folder + "/refresh-v5.request";
        if (File.Exists(refresh))
        {
            File.Delete(refresh);
            EditorApplication.delayCall += () => {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                DateTime runtimeSource = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                    .Select(File.GetLastWriteTimeUtc).Max();
                DateTime editorSource = Directory.GetFiles("Assets/Editor", "*.cs", SearchOption.AllDirectories)
                    .Select(File.GetLastWriteTimeUtc).Max();
                if (runtimeSource > File.GetLastWriteTimeUtc("Library/ScriptAssemblies/Assembly-CSharp.dll")
                    || editorSource > File.GetLastWriteTimeUtc("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"))
                    CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            };
            _nextCheck = EditorApplication.timeSinceStartup + 3d;
            return;
        }
        string request = Folder + "/request-v5.json";
        if (!File.Exists(request)) return;
        Job requested = JsonUtility.FromJson<Job>(File.ReadAllText(request));
        if ((requested.operation == "tutorial-review" || requested.operation == "tutorial-reset" || requested.operation == "fail-review" || requested.operation == "settings-navigation-review" || requested.operation == "settings-review" || requested.operation == "success-order-review" || requested.operation == "bomb-review" || requested.operation == "cards" || requested.operation == "crystal-review" || requested.operation == "retry-review" || requested.operation == "success-review") && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            _nextCheck = EditorApplication.timeSinceStartup + 1d;
            return;
        }
        // A refresh can return before Unity schedules its compilation. Never bake
        // or render the scene with assemblies older than the requested sources.
        DateTime runtimeSourceTime = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc).Max();
        DateTime editorSourceTime = Directory.GetFiles("Assets/Editor", "*.cs", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc).Max();
        if (runtimeSourceTime > File.GetLastWriteTimeUtc("Library/ScriptAssemblies/Assembly-CSharp.dll")
            || editorSourceTime > File.GetLastWriteTimeUtc("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll"))
        {
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
            _nextCheck = EditorApplication.timeSinceStartup + 3d;
            return;
        }
        _busy = true;
        Job job = JsonUtility.FromJson<Job>(File.ReadAllText(request));
        File.Delete(request);
        Reply reply = new Reply { id = job.id };
        try
        {
            switch (job.operation)
            {
                case "inspect": reply.details = Inspect(); break;
                case "tutorial-review":
                    reply.details = FruitLabTutorialEditorTools.Bake()
                        + "\n" + Render(2, 5, validation: FruitLabTutorialEditorTools.Review, validateBeforeRender: true);
                    break;
                case "tutorial-reset": reply.details = FruitLabTutorialEditorTools.ResetLocalProgress(); break;
                case "settings-navigation-review":
                    reply.details = Render(5, 9, validation: FruitLabSettingsEditorTools.Review, validateBeforeRender: true)
                        + "\nMenu/Game navigation requests and release timing passed; no live scene switch or player data mutation";
                    break;
                case "settings-review":
                    reply.details = FruitLabSettingsEditorTools.Bake()
                        + "\n" + Render(5, 9, validation: FruitLabSettingsEditorTools.Review, validateBeforeRender: true)
                        + "\n" + FruitLabRewardEditorTools.RenderMenu()
                        + "\n" + FruitLabSettingsEditorTools.AuditTutorials();
                    break;
                case "success-order-review":
                    FruitLabFailureValidation.ValidateSuccessOrder();
                    reply.details = "Timed left-to-right winning frames passed for 2-5 slots and the one-slot bomb case";
                    break;
                case "bomb-review":
                    FruitLabFailureValidation.ValidateBomb();
                    reply.details = "20 board/fruit combinations, late/repeated-use guards, wrong/correct guesses and in-memory save/resume passed";
                    break;
                case "crystal-review":
                    FruitLabCrystalFlightEditorTools.PreparePrefab();
                    FruitLabFailureValidation.ValidateLogic();
                    FruitLabRewardEditorTools.ValidateRewards();
                    reply.details = Render(5, 9, validation: (canvas, camera) => FruitLabCrystalFlightEditorTools.Review(canvas, camera, false, 5))
                        + "\n" + Render(2, 5, validation: (canvas, camera) => FruitLabCrystalFlightEditorTools.Review(canvas, camera, false, 2))
                        + "\n" + Render(5, 9, true, validation: (canvas, camera) => FruitLabCrystalFlightEditorTools.Review(canvas, camera, true, 5))
                        + "\n" + Render(2, 5, true, validation: (canvas, camera) => FruitLabCrystalFlightEditorTools.Review(canvas, camera, true, 2))
                        + "\nFlight frames: Library/FruitLabEditor/crystal-flight-{hint,collection}-00..12.png";
                    break;
                case "usage-audit": reply.details = FruitLabUsageAudit.Run(); break;
                case "validate": FruitLabLayoutValidation.Validate(); reply.details = "120 scene/prefab configurations passed"; break;
                case "preview": reply.details = Render(job.board, job.fruits); break;
                case "bake": reply.details = Bake(); break;
                case "rewards":
                    reply.details = FruitLabRewardEditorTools.BakeAndValidate() + "\n" + Render(5, 9, true, false)
                        + "\n" + Render(5, 9, true, true) + "\n" + Render(5, 9, true, false, true)
                        + "\n" + FruitLabRewardEditorTools.RenderMenu();
                    break;
                case "menu-preview": reply.details = FruitLabRewardEditorTools.RenderMenu(); break;
                case "success-review":
                    FruitLabFailureValidation.ValidateLogic();
                    reply.details = Bake() + "\n" + FruitLabRewardEditorTools.BakeAndValidate()
                        + "\n" + Render(5, 9, true) + "\n" + Render(2, 5, true)
                        + "\n" + Render(5, 9, true, true) + "\n" + Render(2, 5, true, true)
                        + "\n" + Render(5, 9, true, false, true);
                    FruitLabLayoutValidation.Validate();
                    break;
                case "retry-review":
                    reply.details = Render(5, 9, validation: FruitLabRetryValidation.Validate)
                        + "\n" + Render(2, 5, validation: FruitLabRetryValidation.Validate);
                    break;
                case "cards":
                    FruitLabCardEditorTools.PrepareAssets();
                    FruitLabFailureValidation.ValidateLogic();
                    reply.details = Render(5, 9) + "\n" + Render(2, 5)
                        + "\n" + Render(5, 9, false, false, false, true) + "\n" + Render(2, 5, false, false, false, true)
                        + "\n" + Render(5, 9, true) + "\n" + Render(2, 5, true);
                    FruitLabLayoutValidation.Validate();
                    Debug.Log("FRUIT_LAB_CARDS_PASS: raised cards, monochrome holders and elimination persistence verified.");
                    break;
                case "fail-review":
                    FruitLabFailureValidation.ValidateLogic();
                    reply.details = Bake() + "\n" + FruitLabRewardEditorTools.BakeAndValidate()
                        + "\n" + Render(5, 9, true, false, true) + "\n" + Render(2, 5, true, false, true);
                    break;
                case "theme":
                    FruitLabThemeEditorTools.ImportAssets();
                    reply.details = Bake() + "\n" + Render(5, 9) + "\n" + Render(2, 5)
                        + "\n" + FruitLabRewardEditorTools.BakeAndValidate()
                        + "\n" + Render(5, 9, true, false) + "\n" + Render(5, 9, true, true)
                        + "\n" + Render(5, 9, true, false, true) + "\n" + FruitLabRewardEditorTools.RenderMenu();
                    FruitLabLayoutValidation.Validate();
                    break;
                case "review":
                    FruitLabLayoutValidation.Validate();
                    reply.details = Bake() + "\n" + Render(5, 9) + "\n" + Render(2, 5);
                    break;
                default: throw new InvalidOperationException("Unknown Fruit Lab editor operation");
            }
            reply.ok = true;
        }
        catch (Exception error) { reply.error = error.ToString(); }
        finally
        {
            File.WriteAllText(Folder + "/response-v5.json", JsonUtility.ToJson(reply, true));
            _busy = false;
        }
    }

    private static string Inspect()
    {
        string result = "Playing=" + EditorApplication.isPlaying + "; Screen=" + Screen.width + "x" + Screen.height;
        result += "; HistoryRowLimit=" + FruitLabLayoutMetrics.GetHistoryRowHeight(1000f, 4f);
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene scene = SceneManager.GetSceneAt(index);
            result += "\nScene=" + scene.path + "; dirty=" + scene.isDirty;
            foreach (Canvas canvas in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
                result += "\nCanvas=" + canvas.name + "; size=" + ((RectTransform)canvas.transform).rect.size;
        }
        return result;
    }

    private static string Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before saving the scene layout.");
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Additive);
        File.Copy("Assets/Scenes/Game.unity", Folder + "/Game.before.unity", true);
        try
        {
            foreach (GameObject root in scene.GetRootGameObjects()) Undo.RegisterFullObjectHierarchyUndo(root, "Apply Fruit Lab scene layout");
            FruitLabGameLayout layout = Find<FruitLabGameLayout>(scene);
            CanvasScaler scaler = layout.GetComponent<CanvasScaler>();
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            layout.ApplyLayout();
            foreach (Component component in layout.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Game.unity");
            return "Saved Game.unity using existing scene objects; backup=" + Folder + "/Game.before.unity";
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Tools/Fruit Lab/Apply Layout To Game Scene")]
    public static void ApplyToGameScene()
    {
        Directory.CreateDirectory(Folder);
        Debug.Log(Bake());
    }

    [MenuItem("Tools/Fruit Lab/Apply Layout To Game Scene", true)]
    private static bool CanApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).First();

    private static string Render(int boardCount, int fruitCount, bool popupPreview = false, bool claimPreview = false, bool failPreview = false, bool cardPreview = false, Action<Canvas, Camera> validation = null, bool validateBeforeRender = false)
    {
        Scene scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        Scene renderScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RenderTexture target = null;
        try
        {
            GameObject clone = Object.Instantiate(Find<FruitLabGameLayout>(scene).gameObject);
            SceneManager.MoveGameObjectToScene(clone, renderScene);
            FruitLabGameLayout layout = clone.GetComponent<FruitLabGameLayout>();
            Canvas canvas = layout.GetComponent<Canvas>();
            canvas.transform.SetParent(null, false);
            var cameraObject = new GameObject("FruitLabCaptureCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, renderScene);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            RectTransform rect = (RectTransform)canvas.transform;
            rect.pivot = Vector2.one * .5f;
            rect.localScale = Vector3.one;
            rect.position = Vector3.zero;
            rect.sizeDelta = new Vector2(375f, 812f);
            layout.ApplyLayout();
            foreach (CanvasGroup group in canvas.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1f;
            var gameView = canvas.GetComponentInChildren<GameUIView>(true);
            gameView.SetUserText("Bölüm 12");
            gameView.SetCoinCount(240);
            var powers = canvas.GetComponentInChildren<PowerUpListHolderView>(true);
            powers.SetPowerUpCount(RewardType.Bomb, 2);
            powers.SetPowerUpCount(RewardType.Revealing, 1);
            powers.SetPowerUpCount(RewardType.Life, 3);
            var board = canvas.GetComponentInChildren<BoardAreaView>(true);
            var sizes = new SizeManager();
            var boardLayout = new BoardLayoutManager(board, sizes);
            boardLayout.Initialize(boardCount);
            var boardSlots = new System.Collections.Generic.List<IBoardHolderView>();
            for (int index = 0; index < boardCount; index++)
            {
                var slot = board.CreateBoardHolderView();
                slot.SetLocalScale(); slot.SetSize(boardLayout.GetSizeOfBoardHolder());
                slot.SetLocalPosition(boardLayout.GetBoardHolderSceneLocalPositionList()[index]);
                boardSlots.Add(slot);
            }
            var initial = canvas.GetComponentInChildren<InitialCardAreaView>(true);
            var initialLayout = new InitialCardAreaLayoutManager(initial, sizes);
            var factory = new InitialCardAreaFactory(initial, null, initialLayout);
            var info = Enumerable.Range(0, fruitCount).Select(_ => new CardItemInfo { isExisted = true }).ToList();
            var holders = factory.CreateCardHolders(fruitCount, info, new System.Collections.Generic.List<Vector2>());
            var cards = factory.CreateCardItems(holders, info);
            if (cardPreview)
            {
                for (int i = 0; i < 2; i++)
                {
                    var card = cards[i].GetView();
                    card.SetParent(boardSlots[i].GetGardenRectTransform());
                    card.InitLocalScale(); card.SetLocalPosition(Vector3.zero);
                }
                FruitLabCardEditorTools.EliminateForReview(initial, initialLayout, factory, holders, cards, fruitCount - 1);
            }
            foreach (InitialHolderView holder in canvas.GetComponentsInChildren<InitialHolderView>(true)) holder.ApplyLabStyle();
            var life = canvas.GetComponentInChildren<LifeBarView>(true);
            var rewards = new System.Collections.Generic.List<LifeBarRewardInfo>();
            int[] boundaries = { 0, 2, 5 };
            for (int index = 0; index < 3; index++)
                rewards.Add(new LifeBarRewardInfo(boundaries[index], index < 5 - boardCount ? LifeBarRewardType.Coin : LifeBarRewardType.Crystal, true));
            new LifeBarController(life).SetLifeBar(12, rewards, 6);
            foreach (RewardTokenView token in canvas.GetComponentsInChildren<RewardTokenView>(true)) token.ApplyLabStyle();
            var result = canvas.GetComponentInChildren<ResultAreaView>(true);
            var answer = Enumerable.Range(1, boardCount).ToList();
            var random = new System.Random(29 + boardCount);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int index = 0; index < (popupPreview ? 8 : 6); index++)
            {
                System.Collections.Generic.List<int> guess;
                do
                {
                    var candidates = Enumerable.Range(1, fruitCount).ToList();
                    for (int position = candidates.Count - 1; position > 0; position--)
                    {
                        int other = random.Next(position + 1);
                        int value = candidates[position]; candidates[position] = candidates[other]; candidates[other] = value;
                    }
                    guess = candidates.Take(boardCount).ToList();
                } while (guess.SequenceEqual(answer) || !seen.Add(string.Join(",", guess)));
                if (popupPreview && !failPreview && index == 7) guess = new System.Collections.Generic.List<int>(answer);
                int correct = guess.Where((value, position) => value == answer[position]).Count();
                var row = result.CreateResultBlock();
                var rowController = new ResultBlockController();
                rowController.Initialize(row, new ResultBlockModel {
                    finalNumbers = guess,
                    correctPosCount = correct, wrongPosCount = guess.Count(answer.Contains) - correct
                }, result.GetWidth(), result.GetRowHeight());
                if (popupPreview) rowController.RevealAnswer(answer);
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(result.GetComponent<RectTransform>());
            ScrollRect scroll = result.GetComponentInParent<ScrollRect>();
            scroll.verticalNormalizedPosition = 0f;
            if (popupPreview)
            {
                var popup = canvas.GetComponentInChildren<LevelFinishPopupView>(true);
                popup.SetStatus(true); popup.SetOutcome(!failPreview);
                popup.GetText().gameObject.SetActive(true);
                popup.GetText().text = failPreview ? "Tekrar dene" : "Kazandın!";
                popup.GetText().rectTransform.localScale = Vector3.one;
                popup.GetButton(LevelFinishButtonType.Menu).SetButtonStatus(!claimPreview);
                popup.GetButton(LevelFinishButtonType.Menu).SetText("Menü");
                popup.GetButton(LevelFinishButtonType.Game).SetButtonStatus(!claimPreview);
                popup.GetButton(LevelFinishButtonType.Game).SetText(failPreview ? "Tekrar oyna" : "Bölüm 13");
                popup.GetButton(LevelFinishButtonType.Claim).SetButtonStatus(claimPreview);
                popup.GetButton(LevelFinishButtonType.Claim).SetText("Ödülü al");
                foreach (CanvasGroup group in popup.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1f;
                var bar = (VerticalCrystalProgressView)popup.GetVerticalCrystalProgressView();
                bar.SetStatus(!failPreview); bar.SetAlpha(1f);
                bar.ShowRewardPreview(claimPreview ? RewardType.Life : RewardType.Revealing);
                bar.InitProgress(claimPreview ? 1f / 6f : 4f / 6f);
                if (popupPreview)
                {
                    initial.GetCanvasGroup().alpha = 0f;
                    life.GetCanvasGroup().alpha = 0f;
                    gameView.GetTopButtonsCanvasGroup().alpha = 0f;
                    gameView.GetMiddleButtonsCanvasGroup().alpha = 0f;
                    result.EnableReview();
                    for (int index = 0; index < boardCount; index++)
                    {
                        var card = initial.CreateCardItemView(boardSlots[index].GetGardenRectTransform());
                        card.Init(answer[index]); card.InitLocalScale(); card.SetSize(initialLayout.GetBoxSize());
                        card.SetLocalPosition(Vector3.zero); card.SetLockImageStatus(true);
                        card.SetResultOutline(CardResultState.Correct);
                        if (!failPreview) boardSlots[index].SetSuccessFrameStatus(true);
                    }
                }
                if (claimPreview)
                {
                    popup.SetRewardPresentation();
                    bar.PresentRewardPreviewAtCenter(RewardType.Revealing, .42f).Complete(true);
                }
                Transform dimRoot = canvas.transform.Find("SafeArea/FadePanel");
                dimRoot.gameObject.SetActive(false);
                Image dim = dimRoot.GetComponentInChildren<Image>(true);
                dim.gameObject.SetActive(true);
                dim.color = new Color(FruitLabStyle.Background.r, FruitLabStyle.Background.g, FruitLabStyle.Background.b, .9f);
            }
            foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            camera.cullingMask = 1 << 30;
            camera.orthographic = true;
            camera.orthographicSize = 406f;
            camera.transform.position = new Vector3(0f, 0f, -1000f);
            camera.transform.rotation = Quaternion.identity;
            camera.nearClipPlane = 1f; camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = FruitLabStyle.Background;
            target = new RenderTexture(750, 1624, 24, RenderTextureFormat.ARGB32);
            target.Create(); camera.targetTexture = target;
            camera.aspect = 750f / 1624f;
            Canvas.ForceUpdateCanvases();
            if (validateBeforeRender) { validation?.Invoke(canvas, camera); validation = null; }
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            else camera.Render();
            validation?.Invoke(canvas, camera);
            if (failPreview) FruitLabFailureValidation.ValidateReview(canvas, camera);
            else if (popupPreview)
            {
                FruitLabFailureValidation.ValidateReview(canvas, camera, true);
                FruitLabCompletionValidation.ValidateBoard(canvas, boardCount);
            }
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D capture = new Texture2D(750, 1624, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0f, 0f, 750f, 1624f), 0, 0); capture.Apply();
            RenderTexture.active = previous;
            string suffix = boardCount == 5 && fruitCount == 9 ? "" : "-" + boardCount + "-" + fruitCount;
            string output = popupPreview ? Folder + (failPreview ? "/preview-level-fail-" + boardCount + "-" + fruitCount + ".png" : (claimPreview ? "/preview-reward-claim" : "/preview-level-finish") + suffix + ".png")
                : Folder + (cardPreview ? "/preview-cards-" : "/preview-") + boardCount + "-" + fruitCount + ".png";
            File.WriteAllBytes(output, capture.EncodeToPNG());
            Object.DestroyImmediate(capture);
            return output;
        }
        finally
        {
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.CloseScene(renderScene, true);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}

// Inventory only: preview scenes are closed without saving; player data is untouched.
public static class FruitLabUsageAudit
{
    [Serializable] public class Report
    {
        public string generatedUtc, unityVersion;
        public string[] enabledScenes, roots, reachableAssets;
        public Prefab[] prefabs;
        public SceneInfo[] scenes;
        public string[] warnings;
    }
    [Serializable] public class Prefab { public string path; public bool reachable; public long bytes; public string[] dependencies; }
    [Serializable] public class SceneInfo { public string path; public Node[] objects; public Link[] references; }
    [Serializable] public class Node
    {
        public int id, parent;
        public string path, name, tag, prefabSource;
        public bool activeSelf, activeInHierarchy, animated;
        public int missingScripts;
        public string[] components, scriptPaths;
    }
    [Serializable] public class Link { public int source, target; public string component, property; }

    public static string Run()
    {
        var warnings = new System.Collections.Generic.List<string>();
        string[] enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        string[] assets = AssetDatabase.GetAllAssetPaths().Where(path => path.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(path)).ToArray();
        var roots = new System.Collections.Generic.HashSet<string>(enabled);
        foreach (string path in assets.Where(path => path.Contains("/Resources/") && !path.Contains("/Editor/"))) roots.Add(path);
        foreach (Object asset in PlayerSettings.GetPreloadedAssets()) if (asset != null) roots.Add(AssetDatabase.GetAssetPath(asset));
        foreach (string bundle in AssetDatabase.GetAllAssetBundleNames())
            foreach (string path in AssetDatabase.GetAssetPathsFromAssetBundle(bundle)) roots.Add(path);
        string[] reachable = AssetDatabase.GetDependencies(roots.ToArray(), true);
        var used = new System.Collections.Generic.HashSet<string>(reachable);
        string[] projectPrefabs = assets.Where(path => path.EndsWith(".prefab") &&
            (path.StartsWith("Assets/Prefabs/") || path == "Assets/UnmaskService.prefab" || path == "Assets/Resources/ProjectContext.prefab")).OrderBy(path => path).ToArray();
        var prefabs = projectPrefabs.Select(path => new Prefab { path = path, reachable = used.Contains(path),
            bytes = new FileInfo(path).Length, dependencies = AssetDatabase.GetDependencies(path, false).Where(dep => dep != path).ToArray() }).ToArray();
        var scenes = new System.Collections.Generic.List<SceneInfo>();
        foreach (string path in assets.Where(path => path.StartsWith("Assets/Scenes/") && path.EndsWith(".unity")).OrderBy(path => path))
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                Transform[] transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var nodes = transforms.ToDictionary(t => t.gameObject.GetInstanceID(), t => new Node {
                    id = t.gameObject.GetInstanceID(), parent = t.parent == null ? 0 : t.parent.gameObject.GetInstanceID(),
                    path = PathOf(t), name = t.name, tag = t.gameObject.tag,
                    activeSelf = t.gameObject.activeSelf, activeInHierarchy = t.gameObject.activeInHierarchy,
                    prefabSource = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject),
                    missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),
                    components = t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().FullName).ToArray(),
                    scriptPaths = t.GetComponents<MonoBehaviour>().Where(c => c != null)
                        .Select(c => AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(c))).Distinct().ToArray()
                });
                var links = new System.Collections.Generic.List<Link>();
                foreach (Transform t in transforms)
                foreach (Component component in t.GetComponents<Component>().Where(c => c != null))
                {
                    try
                    {
                        var iterator = new SerializedObject(component).GetIterator();
                        while (iterator.Next(true))
                        {
                            if (iterator.propertyType != SerializedPropertyType.ObjectReference ||
                                iterator.propertyPath == "m_GameObject" || iterator.propertyPath == "m_Father" || iterator.propertyPath.StartsWith("m_Children") ||
                                iterator.propertyPath == "m_CorrespondingSourceObject" || iterator.propertyPath == "m_PrefabInstance" || iterator.propertyPath == "m_PrefabAsset") continue;
                            Object reference = iterator.objectReferenceValue;
                            GameObject target = reference is GameObject go ? go : reference is Component c ? c.gameObject : null;
                            if (target == null || !nodes.ContainsKey(target.GetInstanceID()) || target == t.gameObject) continue;
                            links.Add(new Link { source = t.gameObject.GetInstanceID(), target = target.GetInstanceID(),
                                component = component.GetType().FullName, property = iterator.propertyPath });
                        }
                    }
                    catch (Exception error) { warnings.Add(path + ": " + PathOf(t) + "/" + component.GetType().Name + ": " + error.Message); }
                    if (component is Animator animator && animator.runtimeAnimatorController != null)
                    {
                        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)))
                        {
                            Transform target = string.IsNullOrEmpty(binding.path) ? t : t.Find(binding.path);
                            if (target != null && nodes.TryGetValue(target.gameObject.GetInstanceID(), out Node node)) node.animated = true;
                        }
                    }
                }
                scenes.Add(new SceneInfo { path = path, objects = nodes.Values.OrderBy(node => node.path).ToArray(), references = links.ToArray() });
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        var report = new Report { generatedUtc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
            enabledScenes = enabled, roots = roots.OrderBy(path => path).ToArray(), reachableAssets = reachable.OrderBy(path => path).ToArray(),
            prefabs = prefabs, scenes = scenes.ToArray(), warnings = warnings.ToArray() };
        Directory.CreateDirectory("Library/FruitLabEditor");
        File.WriteAllText("Library/FruitLabEditor/usage-audit.json", JsonUtility.ToJson(report, true));
        return "Read-only audit: " + prefabs.Length + " project prefabs, " + scenes.Count + " scenes, "
            + scenes.Sum(scene => scene.objects.Length) + " scene objects; warnings=" + warnings.Count + "; Library/FruitLabEditor/usage-audit.json";
    }

    private static string PathOf(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null) { transform = transform.parent; path = transform.name + "/" + path; }
        return path;
    }
}

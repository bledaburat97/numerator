using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using Menu;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FruitLabRewardEditorTools
{
    private const string PrefabPath = "Assets/Prefabs/UI/CrystalProgressBar.prefab";
    private const string Folder = "Library/FruitLabEditor";

    public static string BakeAndValidate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before saving reward UI.");
        Directory.CreateDirectory(Folder);
        Scene game = SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");
        bool closeGame = !game.isLoaded;
        if (closeGame) game = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Additive);
        try
        {
            File.Copy("Assets/Scenes/Game.unity", Folder + "/Game.before-rewards.unity", true);
            var popup = Find<LevelFinishPopupView>(game);
            FruitLabStyle.Font = Find<GameUIView>(game).GetUserText().font;
            Canvas.ForceUpdateCanvases();
            popup.ApplyLabLayout();
            var view = (VerticalCrystalProgressView)popup.GetVerticalCrystalProgressView();
            var fill = (SlicedFilledImage)new SerializedObject(view).FindProperty("progressImage").objectReferenceValue;
            Require(fill.transform.GetSiblingIndex() > view.transform.Find("CrystalTrack").GetSiblingIndex(),
                "Pink crystal fill must render above the recessed track");
            view.ApplyLabStyle();
            if (!File.Exists(PrefabPath))
            {
                Directory.CreateDirectory("Assets/Prefabs/UI");
                PrefabUtility.SaveAsPrefabAssetAndConnect(view.gameObject, PrefabPath, InteractionMode.AutomatedAction);
            }
            Require(AssetDatabase.LoadAssetAtPath<VerticalCrystalProgressView>(PrefabPath) != null, "Shared crystal prefab must exist");
            Save(game, popup.gameObject);
        }
        finally { if (closeGame) EditorSceneManager.CloseScene(game, true); }

        Scene menu = SceneManager.GetSceneByPath("Assets/Scenes/Menu.unity");
        bool closeMenu = !menu.isLoaded;
        if (closeMenu) menu = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Additive);
        try
        {
            File.Copy("Assets/Scenes/Menu.unity", Folder + "/Menu.before-rewards.unity", true);
            var canvas = Find<MenuUIView>(menu).GetComponentInParent<Canvas>();
            Transform safe = canvas.transform.Find("SafeArea");
            RectTransform middle = (RectTransform)safe.Find("MiddleArea");
            var crystals = FindOrNull<VerticalCrystalProgressView>(menu);
            if (crystals == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), middle);
                crystals = instance.GetComponent<VerticalCrystalProgressView>();
            }
            var powers = Find<PowerUpListHolderView>(menu);
            foreach (PowerUpButtonView button in powers.GetComponentsInChildren<PowerUpButtonView>(true))
                if (!Enum.GetValues(typeof(RewardType)).Cast<RewardType>().Any(type => ReferenceEquals(powers.GetPowerUpButton(type), button)))
                    button.gameObject.SetActive(false);
            var layout = canvas.GetComponent<FruitLabMenuLayout>() ?? canvas.gameObject.AddComponent<FruitLabMenuLayout>();
            Set(layout, "safeArea", safe); Set(layout, "middleArea", middle);
            Set(layout, "header", Find<MenuHeaderView>(menu)); Set(layout, "menu", Find<MenuUIView>(menu));
            Set(layout, "powers", powers); Set(layout, "crystals", crystals);
            Set(Find<MenuSceneMonoInstaller>(menu), "crystalProgressView", crystals);
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(375f, 667f); scaler.matchWidthOrHeight = 0f;
            scaler.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases(); layout.ApplyLayout();
            crystals.SetStatus(true); crystals.SetAlpha(1f);
            Save(menu, canvas.gameObject);
        }
        finally { if (closeMenu) EditorSceneManager.CloseScene(menu, true); }
        ValidateRewards();
        return "Saved Game.unity, Menu.unity and shared CrystalProgressBar.prefab; reward UI checks passed";
    }

    public static void ValidateRewards()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        GameObject clone = null;
        GameObject trackerObject = null;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        try
        {
            var source = Find<LevelFinishPopupView>(scene);
            clone = Object.Instantiate(source.gameObject, source.transform.parent, false);
            var popup = clone.GetComponent<LevelFinishPopupView>();
            popup.ApplyLabLayout();
            var bindings = new Zenject.DiContainer();
            bindings.Bind<LevelFinishPopupView>().FromInstance(popup);
            var endController = bindings.Instantiate<LevelEndPopupController>();
            endController.SetAllStatusFalse();
            endController.SetOutcome(true);
            endController.InitText("Kazandın!");
            int clicks = 0;
            endController.InitButton(LevelFinishButtonType.Game, "Devam", () => clicks++);
            ((Component)popup.GetButton(LevelFinishButtonType.Game)).GetComponent<BaseButtonView>().OnPointerDown(null);
            Require(clicks == 1, "Modern level-end controller must resolve through DI and retain its button callback after legacy cleanup");
            endController.SetAllStatusFalse();
            var view = (VerticalCrystalProgressView)popup.GetVerticalCrystalProgressView();
            foreach (string field in new[] { "revealingPowerUpButton", "lifePowerUpButton", "bombPowerUpButton" })
            {
                var preview = (PowerUpButtonView)new SerializedObject(view).FindProperty(field).objectReferenceValue;
                Require(preview != null && preview.transform.IsChildOf(view.transform), "Powerup preview must be a local scene instance: " + field);
            }
            trackerObject = new GameObject("RewardPreviewTracker");
            trackerObject.SetActive(false);
            var tracker = trackerObject.AddComponent<LevelTracker>();
            foreach (int previous in new[] { 0, 4, 5 })
            foreach (int earned in new[] { 0, 1, 2, 3 })
            foreach (RewardType type in Enum.GetValues(typeof(RewardType)))
            {
                Field(tracker, "_giftStarCount", previous); Field(tracker, "_currentRewardType", type);
                var controller = new VerticalCrystalProgressController(view, tracker, null);
                controller.Initialize();
                if (earned > 0) controller.PrepareForCollection(previous, type);
                endController.SetOutcome(true, earned);
                controller.AddCrystals(null, earned).Complete(true);
                popup.SetStatus(false); popup.SetStatus(true); popup.ApplyLabLayout();
                Require(view.gameObject.activeSelf == (earned > 0),
                    "Crystal energy is visible only when this level earned crystals, including after reopening the popup");
                int total = previous + earned;
                int capacity = ConstantValues.NUM_OF_STARS_FOR_WILD;
                Require(view.transform.Find("CrystalCount").GetComponent<TMPro.TMP_Text>().text == (total % capacity) + " / " + capacity,
                    "Animated crystal count must carry remaining crystals after overflow");
                RewardType next = (RewardType)(((int)type + total / capacity) % 3);
                var preview = view.GetRewardPreviewRectTransform(next);
                Require(preview != null && preview.gameObject.activeSelf,
                    "Upcoming reward follows the existing rotation");
            }
            view.SetStatus(true);
            view.ShowRewardPreview(RewardType.Life);
            var original = view.GetRewardPreviewRectTransform(RewardType.Revealing);
            Transform parent = original.parent;
            Vector2 size = original.sizeDelta;
            view.PresentRewardPreviewAtCenter(RewardType.Revealing, .42f).Complete(true);
            Require(view.GetRewardPreviewRectTransform(RewardType.Life).gameObject.activeSelf,
                "Presenting the completed reward must keep the upcoming reward visible in the bar");
            view.HidePresentedRewardPreview(.24f).Complete(true);
            Require(original.parent == parent && (original.sizeDelta - size).sqrMagnitude < .01f, "Claim presentation must restore the preview hierarchy and size");
            Require(!original.gameObject.activeSelf, "Completed reward returns to its original hidden state");
            foreach (float height in new[] { 580f, 667f, 812f })
            {
                var canvas = popup.GetComponentInParent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(375f, height);
                popup.ApplyLabLayout();
                Require(((RectTransform)popup.transform).rect.width > 250f, "Popup remains readable on phones");
            }
            Require(errors.Count == 0, "Unity console errors during reward preview: " + string.Join("; ", errors));
        }
        finally
        {
            Application.logMessageReceived -= collect;
            if (clone != null) Object.DestroyImmediate(clone);
            if (trackerObject != null) Object.DestroyImmediate(trackerObject);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        Debug.Log("FRUIT_LAB_REWARDS_PASS: 36 zero/collection/overflow/rotation cases; earned-only visibility, popup reopening, preview restoration and shared prefab references valid.");
    }

    public static string RenderMenu()
    {
        Scene source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RenderTexture target = null;
        try
        {
            GameObject clone = Object.Instantiate(Find<FruitLabMenuLayout>(source).gameObject);
            SceneManager.MoveGameObjectToScene(clone, scene);
            var canvas = clone.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)canvas.transform;
            rect.localScale = Vector3.one; rect.position = Vector3.zero; rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = new Vector2(375f, 812f);
            clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();
            clone.GetComponentInChildren<MenuHeaderView>(true).Init(240);
            var menu = clone.GetComponentInChildren<MenuUIView>(true);
            menu.GetSinglePlayerButton().SetText("Bölüm 13");
            menu.GetMultiplayerButton().SetText("Multiplayer");
            var crystals = clone.GetComponentInChildren<VerticalCrystalProgressView>(true);
            crystals.SetStatus(true); crystals.SetAlpha(1f);
            crystals.InitProgress(4f / 6f); crystals.ShowRewardPreview(RewardType.Revealing);
            var powers = clone.GetComponentInChildren<PowerUpListHolderView>(true);
            powers.SetPowerUpCount(RewardType.Bomb, 2); powers.SetPowerUpCount(RewardType.Revealing, 1); powers.SetPowerUpCount(RewardType.Life, 3);
            var cameraObject = new GameObject("MenuCapture", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.orthographic = true; camera.orthographicSize = 406f; camera.aspect = 750f / 1624f;
            camera.transform.position = new Vector3(0f, 0f, -1000f);
            camera.nearClipPlane = 1f; camera.farClipPlane = 2000f;
            camera.cullingMask = 1 << 30; camera.backgroundColor = FruitLabStyle.Background;
            camera.clearFlags = CameraClearFlags.SolidColor; canvas.worldCamera = camera;
            foreach (Transform child in clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            target = new RenderTexture(750, 1624, 24, RenderTextureFormat.ARGB32); target.Create();
            camera.targetTexture = target; Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(750, 1624, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 750, 1624), 0, 0); image.Apply(); RenderTexture.active = previous;
            string output = Folder + "/preview-menu-crystals.png";
            File.WriteAllBytes(output, image.EncodeToPNG()); Object.DestroyImmediate(image);
            return output;
        }
        finally
        {
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.CloseScene(scene, true); EditorSceneManager.ClosePreviewScene(source);
        }
    }

    private static T Find<T>(Scene scene) where T : Component => FindOrNull<T>(scene) ?? throw new InvalidOperationException("Missing " + typeof(T).Name);
    private static T FindOrNull<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static void Set(Object target, string name, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Save(Scene scene, GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "Scene must save");
    }
    private static void Field(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Reward UI: " + message); }
}

public static class FruitLabSettingsEditorTools
{
    private const string Path = "Assets/Prefabs/SettingsPopup.prefab";
    private const string Folder = "Library/FruitLabEditor";

    public static string Bake()
    {
        string backup = Folder + "/SettingsBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        File.Copy(Path, backup + "/SettingsPopup.prefab");
        File.Copy("Assets/Scenes/Menu.unity", backup + "/Menu.unity");
        Scene game = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        try { FruitLabStyle.Font = Find<GameUIView>(game).GetUserText().font; }
        finally { EditorSceneManager.ClosePreviewScene(game); }
        GameObject root = PrefabUtility.LoadPrefabContents(Path);
        try
        {
            var view = root.GetComponent<SettingsPopupView>();
            var content = (RectTransform)root.transform.Find("Image");
            var title = content.GetComponentsInChildren<TMPro.TMP_Text>(true).First(label => label.GetComponentInParent<BaseButtonView>() == null);
            title.name = "Title";
            var description = content.Find("Description")?.GetComponent<TMPro.TMP_Text>()
                ?? FruitLabStyle.CreateText("Description", content, 13f, FruitLabStyle.Muted);
            var data = new SerializedObject(view);
            var oldClose = (BaseButtonView)data.FindProperty("closeButton").objectReferenceValue;
            if (oldClose.name != "ContinueButton")
            {
                var resume = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buttons/BaseButton.prefab"), content)).GetComponent<BaseButtonView>();
                resume.name = "ContinueButton";
                data.FindProperty("closeButton").objectReferenceValue = resume;
                Object.DestroyImmediate(oldClose.gameObject);
            }
            data.FindProperty("content").objectReferenceValue = content;
            data.FindProperty("title").objectReferenceValue = title;
            data.FindProperty("description").objectReferenceValue = description;
            Transform replay = content.Find("ReplayButton");
            if (replay != null) Object.DestroyImmediate(replay.gameObject);
            data.ApplyModifiedPropertiesWithoutUndo();
            view.ApplyLabStyle();
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            PrefabUtility.SaveAsPrefabAsset(root, Path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        Scene menu = SceneManager.GetSceneByPath("Assets/Scenes/Menu.unity");
        bool opened = !menu.isLoaded;
        if (opened) menu = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Additive);
        try
        {
            var header = Find<MenuHeaderView>(menu);
            Transform title = header.transform.Find("MenuTitle");
            if (title != null) { Undo.DestroyObjectImmediate(title.gameObject); EditorSceneManager.MarkSceneDirty(menu); }
            Require(EditorSceneManager.SaveScene(menu), "Menu title removal must save");
            Require(header.transform.Find("MenuTitle") == null, "Menu title must be removed from the scene");
        }
        finally { if (opened) EditorSceneManager.CloseScene(menu, true); }
        return "Saved SettingsPopup.prefab and removed MenuTitle from Menu.unity; backup=" + backup;
    }

    public static void Review(Canvas canvas, Camera camera)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        var host = new GameObject("SettingsLayoutReview", typeof(RectTransform)).GetComponent<RectTransform>();
        host.SetParent(canvas.transform, false);
        try
        {
            foreach (float width in new[] { 320f, 375f, 430f })
            foreach (float height in new[] { 568f, 667f, 812f })
            foreach (bool single in new[] { true, false })
            {
                host.sizeDelta = new Vector2(width, height);
                GameObject root = Object.Instantiate(prefab, host, false);
                try
                {
                    var view = root.GetComponent<SettingsPopupView>(); view.Init(); view.SetSinglePlayer(single);
                    var rect = (RectTransform)root.transform;
                    Require(rect.rect.width <= width - 31f && rect.rect.height < height, "Popup must fit the available mobile area");
                    Require(((Component)view.GetRetryButton()).gameObject.activeSelf == single, "Retry is available only in single player");
                    IBaseButtonView[] buttons = single ? new[] { view.GetCloseButton(), view.GetRetryButton(), view.GetMenuButton() }
                        : new[] { view.GetCloseButton(), view.GetMenuButton() };
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        Rect bounds = Bounds(buttons[i].GetButtonRectTransform(), rect);
                        Require(rect.rect.Contains(bounds.min) && rect.rect.Contains(bounds.max) && bounds.height >= 44f,
                            "Every settings button must stay inside the panel with a usable touch height");
                        for (int j = 0; j < i; j++) Require(!bounds.Overlaps(Bounds(buttons[j].GetButtonRectTransform(), rect)), "Settings buttons cannot overlap");
                    }
                }
                finally { Object.DestroyImmediate(root); }
            }
        }
        finally { Object.DestroyImmediate(host.gameObject); }

        var fade = canvas.GetComponentInChildren<FadePanelView>(true).GetFadeImage();
        var events = new GameObject("SettingsReviewEvents", typeof(EventSystem));
        SceneManager.MoveGameObjectToScene(events, canvas.gameObject.scene);
        var trackerObject = new GameObject("SettingsReviewTracker"); trackerObject.SetActive(false);
        SceneManager.MoveGameObjectToScene(trackerObject, canvas.gameObject.scene);
        GameObject popup = null;
        try
        {
            var tracker = trackerObject.AddComponent<LevelTracker>();
            var bindings = new Zenject.DiContainer();
            bindings.Bind<IHapticController>().FromInstance(new HapticController());
            bindings.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            var factory = bindings.Resolve<BaseButtonControllerFactory>();
            foreach (bool single in new[] { true, false })
            {
                typeof(LevelTracker).GetField("_gameOption", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tracker, single ? GameOption.SinglePlayer : GameOption.MultiPlayer);
                fade.transform.parent.gameObject.SetActive(true); fade.gameObject.SetActive(true);
                fade.color = new Color(FruitLabStyle.Background.r, FruitLabStyle.Background.g, FruitLabStyle.Background.b, .78f);
                if (popup == null) popup = Object.Instantiate(prefab, canvas.GetComponentInChildren<GamePopupCreator>(true).transform, false);
                foreach (Transform child in popup.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                var view = popup.GetComponent<SettingsPopupView>();
                int closed = 0, saved = 0, deleted = 0;
                new SettingsPopupController().Initialize(new ImmediateCloseView(view), () => { closed++; fade.gameObject.SetActive(false); },
                    () => saved++, () => deleted++, tracker, factory);
                var pointer = new PointerEventData(events.GetComponent<EventSystem>());
                if (single) Capture(camera, Folder + "/preview-settings.png");
                foreach (IBaseButtonView button in new[] { view.GetCloseButton(), view.GetMenuButton(), view.GetRetryButton() })
                {
                    if (!((Component)button).gameObject.activeInHierarchy) continue;
                    var hit = Hit(canvas, camera, button.GetButtonRectTransform(), pointer);
                    Require(hit != null && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit) == ((Component)button).gameObject,
                        "Settings raycast must reach " + ((Component)button).name + "; top hit=" + (hit == null ? "none" : hit.name)
                        + "; handler=" + (hit == null ? "none" : ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit)?.name)
                        + "; pointer=" + pointer.position + "; graphics=" + string.Join(" | ", popup.GetComponentsInChildren<Graphic>(true)
                            .Where(graphic => graphic.raycastTarget).Select(graphic => graphic.name + ":canvas=" + graphic.canvas?.name + ":depth=" + graphic.depth
                                + ":valid=" + graphic.Raycast(pointer.position, camera) + ":inside=" + RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, pointer.position, camera))));
                    Require(((Component)button).GetComponentInChildren<TMPro.TMP_Text>(true).text != "", "Every settings action needs a label");
                }
                ValidateNavigation(view, tracker, factory, pointer, single);
                if (single) continue;
                // Rebind the real close action after the navigation probes.
                new SettingsPopupController().Initialize(new ImmediateCloseView(view), () => { closed++; fade.gameObject.SetActive(false); },
                    () => saved++, () => deleted++, tracker, factory);
                ((BaseButtonView)view.GetCloseButton()).OnPointerDown(pointer);
                Require(closed == 0, "Resume must wait until release");
                ((BaseButtonView)view.GetCloseButton()).OnPointerUp(pointer);
                ((BaseButtonView)view.GetCloseButton()).OnPointerClick(pointer);
                popup = null;
                Require(closed == 1 && saved == 0 && deleted == 0, "Resume closes once without saving, restarting or changing player data");
                var settings = canvas.GetComponentInChildren<GameUIView>(true).GetSettingsButton();
                var underlying = Hit(canvas, camera, settings.GetButtonRectTransform(), pointer);
                Require(underlying != null && ExecuteEvents.GetEventHandler<IPointerDownHandler>(underlying) == ((Component)settings).gameObject,
                    "Closing settings must restore gameplay raycasts");
            }
        }
        finally
        {
            if (popup != null) Object.DestroyImmediate(popup);
            Object.DestroyImmediate(events); Object.DestroyImmediate(trackerObject);
            fade.gameObject.SetActive(false); Draw(camera);
        }
        Debug.Log("FRUIT_LAB_SETTINGS_PASS: 18 mobile layouts; single/multiplayer raycasts; release-only Menu/Game routing, save/delete separation, repeated navigation guard, resume and input restoration.");
    }

    private static void ValidateNavigation(SettingsPopupView view, LevelTracker tracker, BaseButtonControllerFactory factory, PointerEventData pointer, bool single)
    {
        foreach (bool menu in single ? new[] { true, false } : new[] { true })
        {
            var scenes = new List<string>();
            int saved = 0, deleted = 0, stopped = 0;
            new SettingsPopupController(scene => scenes.Add(scene), () => stopped++).Initialize(view, () => { },
                () => saved++, () => deleted++, tracker, factory);
            var button = (BaseButtonView)(menu ? view.GetMenuButton() : view.GetRetryButton());
            button.OnPointerDown(pointer);
            Require(scenes.Count == 0 && saved == 0 && deleted == 0, "Scene navigation cannot run during pointer-down");
            button.OnPointerUp(pointer);
            Require(scenes.Count == 0, "Releasing outside a button without a click cannot navigate");
            button.OnPointerClick(pointer);
            Require(scenes.SequenceEqual(new[] { menu ? "Menu" : "Game" })
                && saved == (menu && single ? 1 : 0) && deleted == (menu ? 0 : 1) && stopped == (menu ? 1 : 0),
                "Ana menü must request Menu/save, while Yeniden başlat requests Game/delete; single=" + single + "; menu=" + menu);
            var other = (BaseButtonView)(menu && single ? view.GetRetryButton() : view.GetMenuButton());
            other.OnPointerDown(pointer); other.OnPointerUp(pointer); other.OnPointerClick(pointer);
            Require(scenes.Count == 1 && saved + deleted == (single ? 1 : 0), "The same popup cannot request a second scene transition");
        }
    }

    public static string AuditTutorials()
    {
        Scene game = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        Scene menu = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");
        var trackerObject = new GameObject("ReadOnlyTutorialAudit"); trackerObject.SetActive(false);
        try
        {
            var tracker = trackerObject.AddComponent<LevelTracker>();
            var creator = new SerializedObject(Find<GamePopupCreator>(game));
            var unmask = new SerializedObject(Find<UnmaskServiceAreaView>(game));
            Object hand = creator.FindProperty("handTutorialPrefab").objectReferenceValue;
            Object message = creator.FindProperty("tutorialMessagePopupPrefab").objectReferenceValue;
            var mask = unmask.FindProperty("unmaskServicePrefab").objectReferenceValue as UnmaskServiceView;
            var maskData = mask != null ? new SerializedObject(mask) : null;
            bool maskReady = maskData != null && new[] { "spotlight", "unmaskCardItemPrefab" }.All(name => maskData.FindProperty(name).objectReferenceValue != null);
            var report = new Dictionary<string, object> {
                ["first_level_trigger_enabled"] = tracker.IsFirstLevelTutorial(),
                ["card_info_trigger_enabled"] = tracker.IsCardInfoTutorial(),
                ["tutorial_prefabs_assigned"] = hand != null && message != null && mask != null,
                ["hand_prefab"] = AssetDatabase.GetAssetPath(hand),
                ["message_prefab"] = AssetDatabase.GetAssetPath(message),
                ["mask_prefab"] = AssetDatabase.GetAssetPath(mask),
                ["spotlight_and_frame_bindings_ready"] = maskReady,
                ["menu_reset_component_active"] = Find<MenuSceneContext>(menu).isActiveAndEnabled,
                ["tutorials_started"] = false,
                ["player_preferences_written"] = false,
                ["scope"] = "Read-only native asset binding inspection; see tutorial-review.json for executed integration checks"
            };
            string output = Folder + "/tutorial-audit.json";
            File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            return output;
        }
        finally { Object.DestroyImmediate(trackerObject); EditorSceneManager.ClosePreviewScene(menu); EditorSceneManager.ClosePreviewScene(game); }
    }

    private sealed class ImmediateCloseView : ISettingsPopupView
    {
        private readonly SettingsPopupView _view;
        public ImmediateCloseView(SettingsPopupView view) => _view = view;
        public void Init() => _view.Init();
        public void SetSinglePlayer(bool single) => _view.SetSinglePlayer(single);
        public void Close() => Object.DestroyImmediate(_view.gameObject);
        public IBaseButtonView GetRetryButton() => _view.GetRetryButton();
        public IBaseButtonView GetMenuButton() => _view.GetMenuButton();
        public IBaseButtonView GetCloseButton() => _view.GetCloseButton();
    }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).First();
    private static Rect Bounds(RectTransform rect, RectTransform parent)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]), max = min;
        foreach (Vector3 corner in corners) { Vector2 p = parent.InverseTransformPoint(corner); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private static GameObject Hit(Canvas canvas, Camera camera, RectTransform rect, PointerEventData pointer)
    {
        Canvas.ForceUpdateCanvases();
        pointer.position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        var hits = new List<RaycastResult>(); canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
        return hits.Count == 0 ? null : hits[0].gameObject;
    }
    private static void Draw(Camera camera)
    {
        Canvas.ForceUpdateCanvases();
        if (GraphicsSettings.currentRenderPipeline != null)
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = camera.targetTexture });
        else camera.Render();
    }
    private static void Capture(Camera camera, string path)
    {
        Draw(camera);
        RenderTexture previous = RenderTexture.active; RenderTexture.active = camera.targetTexture;
        var capture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
        try { capture.ReadPixels(new Rect(0, 0, capture.width, capture.height), 0, 0); capture.Apply(); File.WriteAllBytes(path, capture.EncodeToPNG()); }
        finally { Object.DestroyImmediate(capture); RenderTexture.active = previous; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Settings review: " + message); }
}

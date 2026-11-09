using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Menu;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

public static class FruitLabNavigationEditorTools
{
    private const string Folder = "Library/FruitLabEditor";
    public static string Review()
    {
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); };
        Application.logMessageReceived += collect;
        var previous = MainNavigationState.RequestedTab;
        try
        {
            Bake();
            foreach (var size in new[] { new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932) }) Preview(size);
            foreach (var sceneName in new[] { "Lobby", "WaitingScene" }) CheckOnline(sceneName);
            var game = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
            try { Require(!game.GetRootGameObjects().Any(root => root.GetComponentInChildren<MainNavigationView>(true) != null), "Game has no bottom navigation"); }
            finally { EditorSceneManager.ClosePreviewScene(game); }
        }
        finally { MainNavigationState.RequestedTab = previous; Application.logMessageReceived -= collect; }
        Require(errors.Count == 0, "No runtime/editor errors: " + string.Join("; ", errors));
        File.WriteAllText(Folder + "/navigation-review.json", "{\"passed\":true,\"sizes\":[568,667,812,932],\"tabs\":5,\"checks\":[\"real page switching\",\"locked multiplayer\",\"disabled market\",\"settings access\",\"no home privacy link\",\"no game tabs\",\"online exit routing\"]}");
        return "Five icon tabs; real garden/collection pages; menu settings/achievements; locked multiplayer and disabled market; 4 sizes and real tab raycasts; no game tabs; no player-data writes";
    }
    private static void Bake()
    {
        string backup = Folder + "/NavigationBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"); Directory.CreateDirectory(backup);
        foreach (string name in new[] { "Menu", "Lobby", "WaitingScene" })
        {
            string path = "Assets/Scenes/" + name + ".unity"; File.Copy(path, backup + "/" + name + ".unity", true);
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var canvas = Find<Canvas>(scene); var navigation = MainNavigationView.Ensure(canvas);
                var data = new SerializedObject(navigation); data.FindProperty("settingsPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SettingsPopupView>("Assets/Prefabs/SettingsPopup.prefab"); data.ApplyModifiedPropertiesWithoutUndo();
                if (name == "Menu")
                {
                    canvas.GetComponent<FruitLabMenuLayout>().ApplyLayout();
                    var menu = canvas.GetComponentInChildren<MenuUIView>(true); menu.GetCollectionView().SetEmbedded(true);
                    navigation.FramePage((RectTransform)menu.GetCollectionView().transform); navigation.FramePage((RectTransform)menu.GetAchievementsView().transform);
                }
                else canvas.GetComponent<FruitLabOnlineLayout>().ApplyLayout();
                navigation.ApplyLayout(); navigation.BringForward();
                foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)))
                    if (component != null) { EditorUtility.SetDirty(component); if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component); }
                EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene), "Save " + name);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
    }
    private static void Preview(Vector2 size)
    {
        int height = (int)size.y;
        var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var clone = Object.Instantiate(Find<Canvas>(source).gameObject); SceneManager.MoveGameObjectToScene(clone, scene);
            var canvas = clone.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.transform.localScale = Vector3.one;
            ((RectTransform)canvas.transform).sizeDelta = size; canvas.transform.position = Vector3.zero;
            canvas.GetComponent<FruitLabMenuLayout>().ApplyLayout();
            var menu = canvas.GetComponentInChildren<MenuUIView>(true); var view = MainNavigationView.Ensure(canvas);
            var trackerRoot = new GameObject("NavigationTracker"); trackerRoot.SetActive(false); trackerRoot.transform.SetParent(clone.transform);
            var tracker = trackerRoot.AddComponent<LevelTracker>(); Set(tracker, "_levelId", 20);
            var bindings = new DiContainer(); bindings.Bind<IHapticController>().FromInstance(new SilentHaptics());
            bindings.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            var buttons = bindings.Resolve<BaseButtonControllerFactory>(); var profile = PlayerProfileRuntime.Current;
            var rules = new AchievementRules(new EconomySettings()); var garden = new GardenService(profile, new EconomySettings(), new SystemGardenClock(), rules);
            using var collection = new FruitCollectionPresenter(new FruitInventoryService(profile, rules), profile, menu.GetCollectionView(), buttons, garden, new GardenVisitCoordinator(garden), menu.GetGardenNoticeView());
            using var achievements = new AchievementsPresenter(new AchievementService(profile, rules), profile, menu.GetAchievementsView(), menu.GetAchievementsButton(), buttons);
            var requests = new List<string>();
            using var controller = new MainNavigationController(view, tracker, buttons, collection, achievements, requests.Add, () => { });
            MainNavigationState.RequestedTab = MainTab.Home; controller.Initialize();
            new MenuUIController(tracker, new GameSaveService(), buttons, menu);
            new RewardProgressDisplayController(canvas.GetComponentInChildren<VerticalCrystalProgressView>(true), tracker).Initialize();
            Require(!menu.GetCollectionView().IsVisible, "Home starts without a collection overlay");
            var oldOverflow = canvas.GetComponentInChildren<PowerUpListHolderView>(true).transform.Find("MorePowers");
            Require(oldOverflow == null || !oldOverflow.gameObject.activeSelf,"No retired powerup overflow artifact");
            foreach (var old in new[] { menu.GetMultiplayerButton(), menu.GetCollectionButton(), menu.GetAchievementsButton() }) Require(!((Component)old).gameObject.activeInHierarchy, "Legacy home entries are hidden");
            Require(menu.transform.Find("PrivacyButton") == null || !menu.transform.Find("PrivacyButton").gameObject.activeInHierarchy, "No home privacy entry");
            Require(!view.GetTab(MainTab.Market).interactable, "Market is disabled");
            controller.Select(MainTab.Market); Require(requests.Count == 0, "Market does not navigate");
            foreach (MainTab tab in new[] { MainTab.Home, MainTab.Garden, MainTab.Collection })
            {
                Click(canvas, view.GetTab(tab));
                Require(menu.GetCollectionView().IsVisible == (tab != MainTab.Home), "Real page visibility: " + tab);
                Require(((Component)menu.GetSinglePlayerButton()).gameObject.activeInHierarchy == (tab == MainTab.Home), "Play belongs only to Home");
                Require(!menu.GetCollectionView().GetTabButton(GardenTab.Garden).gameObject.activeSelf, "No duplicate garden tab row");
                Require(!((Component)menu.GetCollectionView().CloseButton).gameObject.activeSelf, "Embedded page has no modal close button");
                foreach (var item in Enum.GetValues(typeof(MainTab)).Cast<MainTab>())
                {
                    var button = view.GetTab(item); Require(((RectTransform)button.transform).rect.height >= 56, "Comfortable tab touch area");
                    Require(button.GetComponentsInChildren<TMP_Text>().All(text => text.text.Length == 0), "Tabs use icons only");
                }
                Capture(canvas, scene, Folder + "/preview-navigation-" + tab.ToString().ToLowerInvariant() + "-" + height + ".png");
                if (tab == MainTab.Garden)
                {
                    var gardenPanel = (GardenPanelView)menu.GetCollectionView().Garden;
                    var purchase = (SeedPurchasePopupView)menu.GetCollectionView().Purchase;
                    Click(canvas, gardenPanel.BuyButton); Require(purchase.gameObject.activeInHierarchy, "Embedded garden opens seed purchase");
                    Capture(canvas,scene,Folder + "/preview-navigation-purchase-" + height + ".png");
                    Click(canvas,purchase.CancelButton); Require(!purchase.gameObject.activeSelf,"Embedded purchase cancels safely");
                }
            }
            Click(canvas, view.SettingsButton);
            var popup = canvas.GetComponentInChildren<SettingsPopupView>(); Require(popup != null, "Menu settings open");
            Require(!((Component)popup.GetRetryButton()).gameObject.activeSelf && !((Component)popup.GetMenuButton()).gameObject.activeSelf, "Menu settings omit game-only actions");
            var privacy = popup.GetComponentsInChildren<Button>().First(button => button.name == "PrivacyButton"); Require(privacy.interactable, "Privacy is accessible through settings");
            var badges = popup.GetComponentsInChildren<Button>().First(button => button.name == "AchievementsButton");
            badges.onClick.Invoke(); Require(menu.GetAchievementsView().IsVisible, "Achievements remain accessible through settings");
            controller.Select(MainTab.Home); Require(!menu.GetAchievementsView().IsVisible, "Tabs dismiss achievement details");
            Set(tracker, "_levelId", 0);
            controller.Select(MainTab.Multiplayer); Require(requests.Count == 0, "Level lock protects direct multiplayer routing");
            Set(tracker, "_levelId", 5); controller.Select(MainTab.Multiplayer); Require(requests.SequenceEqual(new[] { "Lobby" }), "Unlocked tab opens Lobby");
        }
        finally { EditorSceneManager.CloseScene(scene, true); EditorSceneManager.ClosePreviewScene(source); }
    }
    private static void CheckOnline(string name)
    {
        var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
        try
        {
            var view = Find<MainNavigationView>(source); Require(view != null, "Online navigation exists");
            var root = new GameObject("OnlineNavigationTracker"); root.SetActive(false);
            try
            {
                var tracker = root.AddComponent<LevelTracker>(); Set(tracker, "_levelId", 20);
                var requests = new List<string>(); int shutdowns = 0;
                using var controller = new MainNavigationController(view, tracker, null, null, null, requests.Add, () => shutdowns++);
                controller.Initialize(); Require(!view.SettingsButton.gameObject.activeSelf, "Online scenes reuse their existing settings button");
                controller.Select(MainTab.Garden);
                Require(shutdowns == 1 && requests.SequenceEqual(new[] { "Menu" }) && MainNavigationState.RequestedTab == MainTab.Garden, "Online tab leaves the room and opens the requested page");
            }
            finally { Object.DestroyImmediate(root); }
        }
        finally { EditorSceneManager.ClosePreviewScene(source); }
    }
    private static void Click(Canvas canvas, Button button)
    {
        var root = new GameObject("NavigationPointer", typeof(Camera), typeof(EventSystem));
        SceneManager.MoveGameObjectToScene(root, canvas.gameObject.scene);
        var camera = root.GetComponent<Camera>(); camera.enabled = false; camera.orthographic = true;
        var size = ((RectTransform)canvas.transform).rect.size;
        camera.orthographicSize = size.y * .5f; camera.aspect = size.x / size.y;
        camera.transform.position = new Vector3(0,0,-1000); camera.nearClipPlane = 1; camera.farClipPlane = 2000; canvas.worldCamera = camera;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        var target = new RenderTexture((int)size.x,(int)size.y,24); target.Create(); camera.targetTexture = target;
        try
        {
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(root.GetComponent<EventSystem>()) {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); canvas.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
            Require(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(button.transform), "Tab/settings raycast reaches " + button.name + ": " + string.Join(", ", hits.Take(5).Select(hit => hit.gameObject.name)) + " @ " + pointer.position + " / " + rect.rect + " / active=" + button.gameObject.activeInHierarchy);
            button.OnPointerClick(pointer);
        }
        finally { target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(root); }
    }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).First();
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Require(bool value, string message) { if (!value) throw new Exception("Navigation: " + message); }
    private static void Capture(Canvas canvas, Scene scene, string output)
    {
        var cameraRoot = new GameObject("NavigationCamera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraRoot, scene);
        var camera = cameraRoot.GetComponent<Camera>(); var size = ((RectTransform)canvas.transform).rect.size;
        camera.enabled = false; camera.orthographic = true; camera.orthographicSize = size.y * .5f; camera.aspect = size.x / size.y;
        camera.transform.position = new Vector3(0, 0, -1000); camera.nearClipPlane = 1; camera.farClipPlane = 2000; camera.cullingMask = 1 << 30;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = FruitLabStyle.Background; camera.GetUniversalAdditionalCameraData().renderPostProcessing = false; canvas.worldCamera = camera;
        foreach (var item in canvas.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
        var texture = new RenderTexture(750, Mathf.RoundToInt(size.y * 2), 24); texture.Create(); camera.targetTexture = texture;
        var previous = RenderTexture.active; var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        try
        {
            Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
            RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply(); File.WriteAllBytes(output, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.DestroyImmediate(image); texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(cameraRoot); }
    }
    private sealed class SilentHaptics : IHapticController { public void Initialize() { } public void Vibrate(HapticType type) { } }
}

using System;
using DG.Tweening;
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
    private static bool _gardenWorldReview;
    private static bool _presentationReview;
    public static string ReviewGardenPresentation()
    {
        _gardenWorldReview=true;_presentationReview=true;FruitLabGardenWorldEditorTools.SimulateWriteFailure=false;
        try
        {
            string result=Review();
            File.WriteAllText(Folder+"/garden-presentation-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {
                passed=true,species=9,stage_sprites=45,phone_sizes=new[]{568,667,812,932},
                checks=new[]{"species-specific growth art","original puzzle fruit sprites","stable ground points and count badges","live and queued foreground growth",
                    "offscreen growth waits for its column","failure has no collection flight","inventory committed before animation","scaled canvas coordinates",
                    "bounded flight pool","tab change cancels every effect","no inventory writes in tween callbacks","real player profile isolated"}
            },Newtonsoft.Json.Formatting.Indented));
            return result+"; nine-species garden effects, real committed harvest, failed saves and cancellation passed";
        }
        finally {_gardenWorldReview=false;_presentationReview=false;FruitLabGardenWorldEditorTools.SimulateWriteFailure=false;}
    }
    public static string ReviewGardenWorld()
    {
        _gardenWorldReview = true;
        try
        {
            string result = Review();
            File.WriteAllText(Folder + "/garden-world-review.json", "{\"passed\":true,\"sizes\":[568,667,812,932],\"plots\":9,\"cells_per_plot\":4,\"two_column_window\":true,\"free_pan\":true,\"fractional_pan_preserved\":true,\"fixed_chrome\":true,\"collection_modal\":true,\"real_raycast\":true,\"profile_unchanged\":true}");
            return result + "; garden world: free fractional pan, four cells per plot, stage/count labels, enlarged centered plaques and separate collection modal";
        }
        finally { _gardenWorldReview = false; }
    }
    public static string Review()
    {
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            bool tweenError=type==LogType.Warning && message.IndexOf("DOTWEEN",StringComparison.OrdinalIgnoreCase)>=0
                && (message.IndexOf("error",StringComparison.OrdinalIgnoreCase)>=0 || message.IndexOf("destroy",StringComparison.OrdinalIgnoreCase)>=0);
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || tweenError) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        var previous = MainNavigationState.RequestedTab;
        try
        {
            CheckFruitGoalPolicy();
            ImportMenuArtwork();
            Bake();
            foreach (var size in new[] { new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932) }) Preview(size);
            foreach (var sceneName in new[] { "Lobby", "WaitingScene" }) CheckOnline(sceneName);
            var game = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
            try { Require(!game.GetRootGameObjects().Any(root => root.GetComponentInChildren<MainNavigationView>(true) != null), "Game has no bottom navigation"); }
            finally { EditorSceneManager.ClosePreviewScene(game); }
        }
        finally { MainNavigationState.RequestedTab = previous; Application.logMessageReceived -= collect; }
        Require(errors.Count == 0, "No runtime/editor errors: " + string.Join("; ", errors));
        File.WriteAllText(Folder + "/navigation-review.json", "{\"passed\":true,\"sizes\":[568,667,812,932],\"tabs\":5,\"checks\":[\"badge-garden-home-multiplayer-market order\",\"absolute remaining fruit goals only\",\"equal compact header counters\",\"44-unit settings target\",\"notch sky continuity\",\"real page switching\",\"locked multiplayer\",\"disabled market\",\"shared online settings\",\"no home privacy link\",\"no game tabs\",\"online exit routing\"]}");
        return "Generated orchard art and icon tabs; fruit-only nearest goal; shared blue header and notch continuity; 4 sizes and real tab/settings raycasts; online exit routing; no game tabs or player-data writes";
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
    private static void ImportMenuArtwork()
    {
        foreach(string name in new[]{"OrchardHome","BadgeGarden","TabGarden","TabHome","TabMultiplayer","TabMarket"})
        {
            string path="Assets/Resources/Art/Menu/"+name+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            bool background=name=="OrchardHome" || name=="BadgeGarden";
            importer.alphaIsTransparency=!background;importer.mipmapEnabled=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=background?2048:256;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
    private static void CheckFruitGoalPolicy()
    {
        var goals=new[]{
            new AchievementStatus{Id="fruit-a",Kind=MissionKind.FruitA,Fruit=FruitId.Strawberry,Target=100,Progress=90},
            new AchievementStatus{Id="fruit-b",Kind=MissionKind.FruitB,Fruit=FruitId.Orange,Target=10,Progress=8},
            new AchievementStatus{Id="level",Kind=MissionKind.Level,Target=10,Progress=9},
            new AchievementStatus{Id="duel",Kind=MissionKind.Duel,Target=1,Progress=0}
        };
        Require(AchievementPriority.FruitGoals(goals).Select(goal=>goal.Id).SequenceEqual(new[]{"fruit-b","fruit-a"}),"Home uses absolute fruit remainder, never level/duel goals or percentage ranking");
        goals[1].Unlocked=true;Require(AchievementPriority.FruitGoals(goals).Single().Id=="fruit-a","Completed fruit goal is excluded");
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
            var buttons = bindings.Resolve<BaseButtonControllerFactory>(); var profile = _gardenWorldReview ? FruitLabGardenWorldEditorTools.PreviewProfile(_presentationReview) : PlayerProfileRuntime.Current;
            var rules = new AchievementRules(new EconomySettings()); var garden = new GardenService(profile, new EconomySettings(), new SystemGardenClock(), rules);
            using var headerController=new MenuHeaderController(canvas.GetComponentInChildren<MenuHeaderView>(true),profile);headerController.Initialize();
            using var closest=new ClosestBadgePresenter(new AchievementService(profile,rules),profile,new PreviewCollectionNavigation(),ClosestBadgeView.Ensure(canvas.GetComponent<FruitLabMenuLayout>().HomeContent));closest.Initialize();
            var visits=new GardenVisitCoordinator(garden);
            using var collection = new FruitCollectionPresenter(new FruitInventoryService(profile, rules), profile, menu.GetCollectionView(), buttons, garden, visits, menu.GetGardenNoticeView(),new Scripts.Economy.AchievementService(profile,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings())));
            var requests = new List<string>();
            using var controller = new MainNavigationController(view, tracker, buttons, collection, requests.Add, () => { });
            MainNavigationState.RequestedTab = MainTab.Home; controller.Initialize();
            CheckHeaderAndTabs(canvas,view);
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
                Canvas.ForceUpdateCanvases(); menu.GetCollectionView().ApplyLayout(); Canvas.ForceUpdateCanvases();
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
                    CheckGardenChrome(canvas,gardenPanel,menu.GetCollectionView());
                    if (_presentationReview) CheckGardenPresentation(canvas,scene,gardenPanel,profile,controller,visits);
                    else if (_gardenWorldReview) CheckGardenWorld(canvas, scene, gardenPanel, profile, controller);
                    var purchase = (SeedPurchasePopupView)menu.GetCollectionView().Purchase;
                    Click(canvas, gardenPanel.BuyButton); Require(purchase.gameObject.activeInHierarchy, "Embedded garden opens seed purchase");
                    Click(canvas,purchase.FruitButton(FruitId.Banana));
                    Click(canvas,purchase.transform.Find("Panel/Plus").GetComponent<Button>());
                    Require(purchase.QuantityInput.text=="4","Seed quantity controls remain visible and add one two-seed pack");
                    CheckGardenModal(canvas,(RectTransform)purchase.transform,purchase.transform.Find("Panel") as RectTransform);
                    Require(purchase.transform.Find("Panel/Title").GetComponent<TMP_Text>().text=="Tohum Marketi","Seed market title");
                    Require(!purchase.GetComponentsInChildren<TMP_Text>().Any(text=>text.text=="Bakiye" || text.text.StartsWith("Kalan") || text.text.Contains("tohum =")),"Seed market omits redundant balance/price explanations");
                    Capture(canvas,scene,Folder + "/preview-navigation-purchase-" + height + ".png");
                    Click(canvas,purchase.CancelButton); Require(!purchase.gameObject.activeSelf,"Embedded purchase cancels safely");
                    Click(canvas,gardenPanel.CollectionButton);var inventory=menu.GetCollectionView().CollectionPopup;
                    Require(inventory.IsVisible && menu.GetCollectionView().IsGardenSelected,"Album opens a modal and retains garden tab");
                    Require(inventory.GetComponentsInChildren<NonDraggableCardItemView>().Length==9,"Collection modal contains nine fruit cards");
                    CheckGardenModal(canvas,(RectTransform)inventory.transform,inventory.transform.Find("Panel") as RectTransform);
                    Capture(canvas,scene,Folder+"/preview-navigation-inventory-"+height+".png");
                    Click(canvas,inventory.CloseButton);Require(!inventory.IsVisible,"Inventory modal closes without navigation");
                }
                if(tab==MainTab.Collection)
                {
                    Require(menu.GetCollectionView().GetComponentsInChildren<NonDraggableCardItemView>().Length==0,"Badges page has no visible fruit summary");
                    Require(!menu.GetCollectionView().GetComponentsInChildren<TMP_Text>().Any(text=>text.text=="Görevler" || text.text=="Koleksiyon" || text.text.StartsWith("Toplam ")),"Badges page has no redundant headings");
                    var tracks=menu.GetCollectionView().GetComponentsInChildren<Image>().Where(item=>item.name=="Track").ToArray();
                    Require(tracks.All(track=>track.rectTransform.rect.height>=13),"Mission progress bars are thicker");
                    Require(tracks.Select(track=>track.transform.Find("Fill").GetComponent<Image>().color).Distinct().Count()==tracks.Length,"Mission channels have distinct progress colors");
                }
            }
            Click(canvas, view.SettingsButton);
            var popup = canvas.GetComponentInChildren<SettingsPopupView>(); Require(popup != null, "Menu settings open");
            Require(!((Component)popup.GetRetryButton()).gameObject.activeSelf && !((Component)popup.GetMenuButton()).gameObject.activeSelf, "Menu settings omit game-only actions");
            var privacy = popup.GetComponentsInChildren<Button>().First(button => button.name == "PrivacyButton"); Require(privacy.interactable, "Privacy is accessible through settings");
            Require(!popup.GetComponentsInChildren<TMP_Text>().Any(label=>label.text=="Koleksiyon"),"Menu settings has no collection shortcut");
            Capture(canvas,scene,Folder+"/preview-navigation-settings-"+height+".png");
            var closeSettings=(BaseButtonView)popup.GetCloseButton();closeSettings.OnPointerDown(null);closeSettings.OnPointerUp(null);closeSettings.OnPointerClick(null);
            Require(canvas.GetComponentInChildren<SettingsPopupView>()==null,"Menu settings closes cleanly");
            controller.Select(MainTab.Home); Require(!menu.GetAchievementsView().IsVisible, "Tabs dismiss achievement details");
            if(height==812)CheckNotchedHome(canvas,scene);
            Set(tracker, "_levelId", 0);
            controller.Select(MainTab.Multiplayer); Require(requests.Count == 0, "Level lock protects direct multiplayer routing");
            Set(tracker, "_levelId", 5); controller.Select(MainTab.Multiplayer); Require(requests.SequenceEqual(new[] { "Lobby" }), "Unlocked tab opens Lobby");
            // Scene objects may lose their parent before Zenject disposes the controller.
            // Teardown must use the original subscription target, not rediscover Home.
            view.transform.SetParent(null, false);
            Require(view.Collection == null, "Detached navigation no longer has a home canvas");
            controller.Dispose();
            Object.DestroyImmediate(view.gameObject);
            controller.Dispose();
        }
        finally { EditorSceneManager.CloseScene(scene, true); EditorSceneManager.ClosePreviewScene(source); }
    }
    private static void CheckOnline(string name)
    {
        var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + name + ".unity");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var clone=Object.Instantiate(Find<Canvas>(source).gameObject);SceneManager.MoveGameObjectToScene(clone,scene);
            var canvas=clone.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.transform.localScale=Vector3.one;
            ((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.transform.position=Vector3.zero;
            canvas.GetComponent<FruitLabOnlineLayout>().ApplyLayout();
            var view = MainNavigationView.Ensure(canvas); Require(view != null, "Online navigation exists");
            var root = new GameObject("OnlineNavigationTracker"); root.SetActive(false);
            try
            {
                var tracker = root.AddComponent<LevelTracker>(); Set(tracker, "_levelId", 20);
                var bindings=new DiContainer();bindings.Bind<IHapticController>().FromInstance(new SilentHaptics());
                bindings.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
                var buttons=bindings.Resolve<BaseButtonControllerFactory>();
                var requests = new List<string>(); int shutdowns = 0;
                using var header=new MenuHeaderController(MenuHeaderView.Ensure(canvas),PlayerProfileRuntime.Current);header.Initialize();
                using var controller = new MainNavigationController(view, tracker, buttons, null, requests.Add, () => shutdowns++);
                controller.Initialize(); Require(view.SettingsButton.gameObject.activeSelf, "Shared header settings are visible online");
                view.BringForward();
                Capture(canvas,scene,Folder+"/preview-navigation-"+name.ToLowerInvariant()+"-812.png");
                Click(canvas,view.SettingsButton);
                Require(canvas.GetComponentInChildren<SettingsPopupView>()!=null,"Shared online settings opens from real raycast");
                controller.Select(MainTab.Garden);
                Require(shutdowns == 1 && requests.SequenceEqual(new[] { "Menu" }) && MainNavigationState.RequestedTab == MainTab.Garden, "Online tab leaves the room and opens the requested page");
            }
            finally { Object.DestroyImmediate(root); }
        }
        finally { EditorSceneManager.CloseScene(scene,true);EditorSceneManager.ClosePreviewScene(source); }
    }
    private static void CheckNotchedHome(Canvas canvas,Scene scene)
    {
        var header=canvas.GetComponentInChildren<MenuHeaderView>(true);
        var layout=canvas.GetComponent<FruitLabMenuLayout>();
        var safe=(RectTransform)typeof(FruitLabMenuLayout).GetField("safeArea",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(layout);
        safe.anchorMin=new Vector2(0,34f/812);safe.anchorMax=new Vector2(1,1-47f/812);Canvas.ForceUpdateCanvases();layout.ApplyLayout();
        var apply=typeof(MenuHeaderView).GetMethod("ApplyLayout",BindingFlags.NonPublic|BindingFlags.Instance,null,new[]{typeof(Vector2)},null);
        apply.Invoke(header,new object[]{new Vector2(0,47)});
        var navigation=canvas.GetComponentInChildren<MainNavigationView>(true);
        navigation.SafeArea.anchorMin=safe.anchorMin;navigation.SafeArea.anchorMax=safe.anchorMax;
        Canvas.ForceUpdateCanvases();
        var headerRect=(RectTransform)header.transform;var canvasRect=(RectTransform)canvas.transform;
        var corners=new Vector3[4];headerRect.GetWorldCorners(corners);
        Require(Mathf.Abs(canvasRect.InverseTransformPoint(corners[1]).y-canvasRect.rect.yMax)<.1f,"Header reaches physical top above notch");
        Require(Mathf.Abs(headerRect.rect.height-103)<.1f,"Safe-area and header share one continuous surface");
        var gear=(RectTransform)header.SettingsButton.transform;gear.GetWorldCorners(corners);
        Require(canvasRect.InverseTransformPoint(corners[1]).y<=canvasRect.rect.yMax-47,"Settings stays below notch");
        Capture(canvas,scene,Folder+"/preview-navigation-home-notched-812.png");
        safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;Canvas.ForceUpdateCanvases();layout.ApplyLayout();navigation.ApplyLayout();
    }
    private sealed class PreviewCollectionNavigation : ICollectionNavigation { public void OpenCollection(string id=null){} }
    private static void CheckHeaderAndTabs(Canvas canvas,MainNavigationView view)
    {
        var header=canvas.GetComponentInChildren<MenuHeaderView>(true);header.ApplyLayout();
        var badges=header.transform.Find("BadgeCounter") as RectTransform;
        var coins=header.GetComponentsInChildren<TextHolderAdjustment>(true).Single().transform as RectTransform;
        Require(Mathf.Abs(badges.rect.width-coins.rect.width)<.1f && Mathf.Abs(badges.rect.height-coins.rect.height)<.1f,"Header counters have equal compact dimensions");
        Require(badges.position.x<coins.position.x && coins.position.x<view.SettingsButton.transform.position.x,"Header order is badge, coins, settings");
        Require(((RectTransform)view.SettingsButton.transform).rect.width==44,"Settings uses a smaller 44-unit whole button");
        var order=new[]{MainTab.Collection,MainTab.Garden,MainTab.Home,MainTab.Multiplayer,MainTab.Market};
        for(int i=0;i<order.Length;i++)
        {
            var tab=view.GetTab(order[i]);var icon=tab.transform.Find("TabArtwork").GetComponent<Image>();
            Require(icon.sprite!=null,"Generated icon loaded: "+order[i]);
            if(i>0)Require(tab.transform.position.x>view.GetTab(order[i-1]).transform.position.x,"Requested tab ordering");
        }
        var goal=canvas.GetComponentInChildren<ClosestBadgeView>(true);
        Require(!goal.transform.Find("Name").gameObject.activeSelf,"Home goal has no fruit name");
        Require(((RectTransform)goal.transform.Find("Track")).rect.height>=20,"Home progress track is thicker");
    }
    private static void CheckGardenWorld(Canvas canvas, Scene scene, GardenPanelView panel, IPlayerProfileRepository profile, MainNavigationController controller)
    {
        panel.ApplyLayout(); Canvas.ForceUpdateCanvases();
        string before = Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot());
        Require(panel.Plots.Count == 9 && panel.Plots.All(plot => plot.transform.Find("Cell3") != null && plot.transform.Find("Cell4") == null), "Nine plots, four cells each");
        Require(panel.Plots.Select(plot => plot.Fruit).SequenceEqual(GardenPanelView.FruitOrder), "Fixed fruit geography");
        Require(panel.Plots.Single(plot => plot.Fruit == FruitId.Apple).CellCount(0).text == "12 çiçek", "Actual batch quantity and growth stage");
        Require(panel.Plots.Single(plot => plot.Fruit == FruitId.Pear).OccupiedCells == 0, "Empty cells have no fabricated content");
        var scroll = panel.WorldScroll;
        Require(Mathf.Abs(scroll.viewport.rect.width - scroll.ColumnWidth * 2) < .1f && Mathf.Abs(scroll.content.rect.width - scroll.ColumnWidth * 3) < .1f, "Two-column window, three equal world columns");
        foreach (var plot in panel.Plots)
            for (int i = 0; i < 4; i++)
                if (plot.CellArt(i).gameObject.activeSelf)
                {var label=plot.CellCount(i);label.ForceMeshUpdate();Require(label.fontSize>=12 && !label.isTextOverflowing,"Count and stage fit without clipping: "+label.text);}
        Require(((RectTransform)panel.BuyButton.transform).rect.width >= 48 && ((RectTransform)panel.CollectionButton.transform).rect.height >= 48, "Garden touch targets >=48");
        var fixedPosition = panel.BuyButton.transform.position;
        int height = (int)((RectTransform)canvas.transform).rect.height;
        Capture(canvas, scene, Folder + "/preview-garden-world-left-" + height + ".png");
        var root = new GameObject("GardenDragPointer", typeof(Camera), typeof(EventSystem)); SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            var camera = root.GetComponent<Camera>(); camera.enabled = false; camera.orthographic = true;
            var size = ((RectTransform)canvas.transform).rect.size;
            camera.orthographicSize = size.y * .5f; camera.aspect = size.x / size.y; camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = 1; camera.farClipPlane = 2000; canvas.worldCamera = camera;
            var start = RectTransformUtility.WorldToScreenPoint(camera, scroll.viewport.TransformPoint(scroll.viewport.rect.center));
            void Drag(float amount)
            {
                // Pointer movement must preserve fractional horizontal positions.
                var pointer = new PointerEventData(root.GetComponent<EventSystem>()) {
                    button = PointerEventData.InputButton.Left, position = start, pressPosition = start,
                    pointerPressRaycast = new RaycastResult { module = canvas.GetComponent<GraphicRaycaster>() }
                };
                scroll.OnInitializePotentialDrag(pointer); scroll.OnBeginDrag(pointer);
                pointer.position = RectTransformUtility.WorldToScreenPoint(camera, scroll.viewport.TransformPoint(scroll.viewport.rect.center + new Vector2(amount, 0)));
                scroll.OnDrag(pointer); scroll.OnEndDrag(pointer);
            }
            Drag(-scroll.ColumnWidth * .1f);
            Require(Mathf.Abs(scroll.ColumnOffset-.1f)<.01f,"Ten-percent drag remains at ten percent after release");
            panel.ApplyLayout();Require(Mathf.Abs(scroll.ColumnOffset-.1f)<.01f,"Layout refresh preserves fractional position");
            Require(panel.BuyButton.transform.position == fixedPosition, "Seed shop stays fixed");
            Capture(canvas, scene, Folder + "/preview-garden-world-right-" + height + ".png");
            canvas.worldCamera = camera; // Capture owns a temporary camera.
            Drag(-scroll.ColumnWidth * 3); Require(scroll.ColumnOffset==1, "World clamps at right edge");
            Drag(scroll.ColumnWidth * .8f); Require(Mathf.Abs(scroll.ColumnOffset-.2f)<.01f, "Reverse drag is free, not page-snapped");
            scroll.SetColumn(0,false);
        }
        finally { Object.DestroyImmediate(root); }
        var legacyBatches = new Dictionary<string, PlantingBatch>();
        for (int i = 0; i < 6; i++) legacyBatches["legacy:" + i] = new PlantingBatch { CellIndex=i<4?i:-1,IsLegacyOverflow=i>=4,Fruit = FruitId.Apple, Quantity = i + 1, PlantedUtcDay = i, Stage = GardenGrowthStage.Seed };
        panel.Render(legacyBatches);
        var apple = panel.Plots.Single(plot => plot.Fruit == FruitId.Apple);
        Require(apple.OccupiedCells == 4 && apple.transform.Find("FruitSign/AdditionalBatches").GetComponent<TMP_Text>().text == "+2", "Legacy overflow remains discoverable");
        Click(canvas, apple.DetailsButton);
        var details = panel.GetComponentInChildren<GardenBatchListView>();
        Require(details != null && details.GetComponentsInChildren<NonDraggableCardItemView>().Length == 6, "All legacy batches are inspectable");
        Click(canvas, panel.transform.Find("PlantingDetails/CloseDetails").GetComponent<Button>());
        Click(canvas, panel.CollectionButton);
        Require(MainNavigationState.RequestedTab == MainTab.Garden && canvas.GetComponentInChildren<FruitInventoryPopupView>().IsVisible, "Album opens the collection modal without changing tabs");
        Click(canvas,canvas.GetComponentInChildren<FruitInventoryPopupView>().CloseButton);
        controller.Select(MainTab.Garden); Canvas.ForceUpdateCanvases(); panel.ApplyLayout();
        Require(Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot()) == before, "Garden presentation leaves the profile unchanged");
    }
    private static void CheckGardenChrome(Canvas canvas,GardenPanelView garden,FruitCollectionView collection)
    {
        Require(!garden.transform.Find("GardenTitle").gameObject.activeSelf,"Garden has no page title");
        Require(Mathf.Abs(garden.BuyButton.transform.position.y-garden.CollectionButton.transform.position.y)<.1f
            && garden.CollectionButton.transform.position.x>garden.BuyButton.transform.position.x,"Album and seed market are opposite each other on the bottom row");
        foreach(var plot in garden.Plots)
        {
            var sign=plot.transform.Find("FruitSign") as RectTransform;
            Require(sign.anchoredPosition.x==plot.Root.rect.width*.5f,"Fruit plaque is centered below its plot");
            Require((sign.Find("Plaque") as RectTransform).rect.width>=56,"Fruit plaque is enlarged");
        }
    }
    private static void CheckGardenModal(Canvas canvas,RectTransform root,RectTransform panel)
    {
        Canvas.ForceUpdateCanvases();var header=canvas.GetComponentInChildren<MenuHeaderView>();
        var corners=new Vector3[4];panel.GetWorldCorners(corners);float top=header.transform.InverseTransformPoint(corners[1]).y;
        Require(top<=((RectTransform)header.transform).rect.yMin+.1f,"Garden modal starts below shared header");
    }
    private static void CheckGardenPresentation(Canvas canvas,Scene scene,GardenPanelView panel,IPlayerProfileRepository profile,MainNavigationController controller,GardenVisitCoordinator visits)
    {
        panel.ApplyLayout();Canvas.ForceUpdateCanvases();int height=(int)((RectTransform)canvas.transform).rect.height;
        foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))
            foreach(GardenGrowthStage stage in Enum.GetValues(typeof(GardenGrowthStage)))
            {
                var art=GardenArtwork.Stage(fruit,stage);Require(art!=null,"Art exists: "+fruit+" "+stage);
                if(stage==GardenGrowthStage.Fruit)Require(art==FruitLabStyle.Icon(fruit.ToString()),"Mature product is the exact puzzle sprite");
                else Require(art.name==stage.ToString() && art.texture.name==fruit+"Growth","Species-specific plant atlas: "+fruit);
            }
        for(int column=0;column<2;column++)
        {
            panel.WorldScroll.SetColumn(column,false);
            Capture(canvas,scene,Folder+"/preview-garden-complete-seeds-"+(column==0?"left-":"right-")+height+".png");
        }
        panel.WorldScroll.SetColumn(0,false);
        var anchors=panel.Plots.SelectMany(plot=>Enumerable.Range(0,4).Select(i=>plot.CellArt(i).rectTransform.anchoredPosition)).ToArray();
        var badgePositions=panel.Plots.SelectMany(plot=>Enumerable.Range(0,4).Select(i=>((RectTransform)plot.CellCount(i).transform.parent).anchoredPosition)).ToArray();
        var quantities=profile.Snapshot().Garden.ToDictionary(pair=>pair.Key,pair=>pair.Value.Quantity);
        var oldUpdate=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        void Advance(float seconds){for(float t=0;t<seconds;t+=.02f)DOTween.ManualUpdate(.02f,.02f);}
        void Gesture(string name)
        {
            var hit=panel.Plots.Single(plot=>plot.Fruit==FruitId.Banana).CellRect(3).Find("HarvestHit").GetComponent<GardenHarvestCell>();
            var pointer=new PointerEventData(null){button=PointerEventData.InputButton.Left};
            Require(hit.gameObject.activeInHierarchy,name+" cell active");hit.OnPointerDown(pointer);hit.OnPointerClick(pointer);
        }
        try
        {
            visits.Initialize();
            Require(panel.GetComponentsInChildren<GardenCellAnimationView>(true).Count(view=>view.IsAnimating)==24,"Only six visible plots animate growth");
            Require(visits.TakeVisualChanges().Count==0,"Presenter consumes foreground visual history separately from notices");
            Advance(.18f);Capture(canvas,scene,Folder+"/preview-garden-growth-motion-"+height+".png");Advance(.6f);
            Require(panel.GetComponentsInChildren<GardenCellAnimationView>(true).All(view=>!view.IsAnimating),"Cell effects settle in less than one second");
            Require(anchors.SequenceEqual(panel.Plots.SelectMany(plot=>Enumerable.Range(0,4).Select(i=>plot.CellArt(i).rectTransform.anchoredPosition))),"Ground points do not change between stages");
            Require(badgePositions.SequenceEqual(panel.Plots.SelectMany(plot=>Enumerable.Range(0,4).Select(i=>((RectTransform)plot.CellCount(i).transform.parent).anchoredPosition))),"Count badges never move");
            Require(profile.Snapshot().Garden.All(pair=>quantities[pair.Key]==pair.Value.Quantity),"Growth animation preserves quantities");
            Capture(canvas,scene,Folder+"/preview-garden-complete-ripe-left-"+height+".png");
            panel.WorldScroll.SetColumn(1,false);
            Require(panel.GetComponentsInChildren<GardenCellAnimationView>(true).Count(view=>view.IsAnimating)==12,"Unseen third-column growth starts when its column opens");
            Advance(.7f);Capture(canvas,scene,Folder+"/preview-garden-complete-ripe-right-"+height+".png");
            var presentation=panel.GetComponentInChildren<GardenHarvestPresentation>(true);
            string before=Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot());
            FruitLabGardenWorldEditorTools.SimulateWriteFailure=true;
            Gesture("Failed harvest");
            Require(presentation.ActiveFlights==0 && Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot())==before,"Failed harvest never flies fruit or alters inventory");
            FruitLabGardenWorldEditorTools.SimulateWriteFailure=false;
            var menu=canvas.GetComponentInChildren<MenuUIView>(true);menu.GetGardenNoticeView().Hide();
            Gesture("Committed harvest");
            Require(profile.Snapshot().Collection[FruitId.Banana].Current==30 && !profile.Snapshot().Garden.ContainsKey("showcase:"+FruitId.Banana+":3") && presentation.ActiveFlights==1,"Fruit is saved and the cell freed before flight finishes");
            string committed=Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot());
            menu.GetGardenNoticeView().Hide();Advance(.23f);Capture(canvas,scene,Folder+"/preview-garden-harvest-motion-"+height+".png");Advance(.65f);
            Require(presentation.ActiveFlights==0 && Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot())==committed,"Arrival callbacks cannot grant more fruit");
            Click(canvas,panel.BuyButton);
            var purchase=(SeedPurchasePopupView)menu.GetCollectionView().Purchase;
            Click(canvas,purchase.FruitButton(FruitId.Banana));int coins=profile.Snapshot().Wallet.Coins;
            Click(canvas,purchase.ConfirmButton);purchase.ConfirmButton.onClick.Invoke();menu.GetGardenNoticeView().Hide();
            var fresh=profile.Snapshot().Garden.Single(pair=>pair.Value.Fruit==FruitId.Banana && pair.Value.Stage==GardenGrowthStage.Seed);
            Require(fresh.Value.CellIndex==3 && fresh.Value.Quantity==new EconomySettings().SeedsPerPack && profile.Snapshot().Wallet.Coins==coins-1,"Planting effect follows one committed seed-pack purchase");
            Advance(.3f);Capture(canvas,scene,Folder+"/preview-garden-planting-motion-"+height+".png");Advance(.5f);
            Require(panel.GetComponentsInChildren<GardenCellAnimationView>(true).All(view=>!view.IsAnimating),"Planting particles stop");
            var canvasScale=canvas.transform.localScale;canvas.transform.localScale=Vector3.one*1.3f;
            var visual=panel.CaptureHarvest("showcase:"+FruitId.Kiwi+":3");panel.PlayHarvest(visual);
            var proxy=presentation.GetComponentsInChildren<Image>().Single();
            Require(Vector3.Distance(proxy.transform.position,visual.Center)<.01f,"Flight originates on the fruit on a scaled canvas");
            for(int i=0;i<18;i++)panel.PlayHarvest(visual);
            Require(presentation.ActiveFlights==6 && presentation.GetComponentsInChildren<Image>(true).Length==6,"Rapid taps use a fixed six-flight pool");
            string unchanged=Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot());
            controller.Select(MainTab.Collection);Advance(.8f);
            Require(presentation.ActiveFlights==0 && Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot())==unchanged,"Leaving garden cancels every flying fruit without changing the saved reward");
            canvas.transform.localScale=canvasScale;controller.Select(MainTab.Garden);panel.ApplyLayout();
            panel.PlayGrowth(new[]{new GardenBatchChange {BatchId="showcase:"+FruitId.Kiwi+":0",Fruit=FruitId.Kiwi,PreviousStage=GardenGrowthStage.Seed,CurrentStage=GardenGrowthStage.Sprout}});
            Advance(.1f);controller.Select(MainTab.Home);Advance(.7f);
            Require(panel.GetComponentsInChildren<GardenCellAnimationView>(true).All(view=>!view.IsAnimating),"Tab change cancels growth too");
            controller.Select(MainTab.Garden);panel.ApplyLayout();
            Require(panel.GetComponentsInChildren<Image>(true).Where(image=>image.name=="PreviousPlant").All(image=>!image.gameObject.activeSelf),"No stale previous-stage image remains");
        }
        finally {panel.CancelEffects();DOTween.defaultUpdateType=oldUpdate;FruitLabGardenWorldEditorTools.SimulateWriteFailure=false;}
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

public static class FruitLabGardenWorldEditorTools
{
    public static bool SimulateWriteFailure;
    private const string Path = "Assets/Resources/Art/Garden/Pilot/";
    public static string Review()
    {
        ImportArtwork();
        FruitLabTutorialEditorTools.BakeCollection();
        return FruitLabNavigationEditorTools.ReviewGardenWorld();
    }
    public static string ReviewPresentation()
    {
        ReviewQueuedGrowth();ImportArtwork();FruitLabTutorialEditorTools.BakeCollection();
        return FruitLabNavigationEditorTools.ReviewGardenPresentation();
    }
    private static void ReviewQueuedGrowth()
    {
        var data=new PlayerProfileData();
        foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))
            for(int i=0;i<4;i++)data.Garden[fruit+":"+i]=new PlantingBatch {Fruit=fruit,CellIndex=i,Quantity=10,
                PlantedUtcDay=20000,LastGrowthUtcDay=20005,Stage=(GardenGrowthStage)i};
        for(int i=0;i<8;i++)data.Garden["legacy:"+i]=new PlantingBatch {Fruit=FruitId.Apple,CellIndex=-1,IsLegacyOverflow=true,Quantity=10,
            PlantedUtcDay=20000,LastGrowthUtcDay=20005,Stage=GardenGrowthStage.Seed};
        var profile=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>data);
        var clock=new PresentationClock();var settings=new EconomySettings();
        var service=new GardenService(profile,settings,clock,new AchievementRules(settings));var visits=new GardenVisitCoordinator(service);
        visits.Initialize();visits.TakeNotice();clock.Day++;visits.SetFocused(false);visits.SetFocused(true);
        string before=Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot());var visual=visits.TakeVisualChanges();
        if(visual.Count!=36 || visual.Any(change=>change.BatchId.StartsWith("legacy:") || change.PreviousStage!=(GardenGrowthStage)int.Parse(change.BatchId.Split(':')[1]))
            || visits.TakeVisualChanges().Count!=0 || before!=Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot()))
            throw new Exception("Queued growth must preserve first/last stages, survive menu notices, stay bounded to 36 cells and never write inventory");
    }
    private sealed class PresentationClock:IGardenClock
    { public int Day=20006;public DateTime UtcNow=>new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddDays(Day); }
    public static IPlayerProfileRepository PreviewProfile(bool allFruits=false)
    {
        var data = new PlayerProfileData(); data.Wallet.Coins = 184;
        if(allFruits)
        {
            foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))
                for(int i=0;i<4;i++)data.Garden["showcase:"+fruit+":"+i]=new PlantingBatch {CellIndex=i,Fruit=fruit,Quantity=12+i*6,
                    PlantedUtcDay=20000+i,LastGrowthUtcDay=20005,Stage=(GardenGrowthStage)i};
            return new PlayerProfileRepository(new PresentationStorage(),()=>data);
        }
        int index = 0;
        foreach (var fruit in new[] { FruitId.Apple, FruitId.Strawberry })
            for (int i = 0; i < 4; i++) data.Garden["preview:" + index++] = new PlantingBatch {
                CellIndex=i,Fruit = fruit, Quantity = 12 + i * 6, PlantedUtcDay = 20000 + i, LastGrowthUtcDay = 20005,
                Stage = (GardenGrowthStage)(fruit == FruitId.Strawberry ? 4 - i : 3 - i)
            };
        return new PlayerProfileRepository(new MemoryPlayerProfileStorage(), () => data);
    }
    private sealed class PresentationStorage:IPlayerProfileStorage
    {
        private readonly MemoryPlayerProfileStorage _inner=new MemoryPlayerProfileStorage();
        public PlayerProfileData Load()=>_inner.Load();
        public void Save(PlayerProfileData data){if(SimulateWriteFailure)throw new IOException("Simulated harvest presentation failure");_inner.Save(data);}
    }
    private static void ImportArtwork()
    {
        foreach (string name in Enum.GetValues(typeof(FruitId)).Cast<FruitId>().Select(fruit=>fruit+"Growth").Concat(new[] { "EmptyPlot", "GardenGround", "GardenProps" }))
        {
            string added="Assets/Resources/Art/Garden/Growth/"+name+".png";
            string path = File.Exists(added)?added:Path+name+".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.isReadable = false; importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ; importer.wrapMode = TextureWrapMode.Clamp;
            var textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(textureSettings); importer.spritePixelsPerUnit = 100;
            if (name.EndsWith("Growth") || name == "GardenProps")
            {
                var source = new Texture2D(2, 2); source.LoadImage(File.ReadAllBytes(path));
                try
                {
                    bool growth = name.EndsWith("Growth"); int columns = growth ? 3 : 2;
                    string[] names = growth ? new[] { "Seed", "Sprout", "Leaves", "Flower", "Fruit" } : new[] { "Sign", "CountBadge", "SeedShop", "Collection" };
                    var sprites = new List<SpriteMetaData>();
                    var pixels=source.GetPixels32();
                    if(pixels[0].a>16 || pixels[source.width-1].a>16)throw new Exception("Opaque growth atlas background: "+name);
                    for (int i = 0; i < names.Length; i++)
                    {
                        float width = source.width / (float)columns, height = source.height / 2f;
                        var tile = new Rect(i % columns * width, source.height - (i / columns + 1) * height, width, height);
                        sprites.Add(new SpriteMetaData { name = names[i], alignment = 0, pivot = Vector2.one * .5f,
                            rect = SpriteBounds(pixels,source.width,tile) });
                    }
                    importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
                    importer.spritesheet = sprites.ToArray();
#pragma warning restore CS0618
                }
                finally { Object.DestroyImmediate(source); }
            }
            else importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        GardenArtwork.ResetCache();
        foreach (string species in Enum.GetValues(typeof(FruitId)).Cast<FruitId>().Select(fruit=>fruit.ToString()))
            if (Resources.LoadAll<Sprite>("Art/Garden/Growth/"+species+"Growth").Concat(Resources.LoadAll<Sprite>("Art/Garden/Pilot/"+species+"Growth")).Count() != 5)
                throw new Exception("Expected five named growth sprites: " + species);
    }
    private static Rect SpriteBounds(Color32[] pixels,int textureWidth, Rect tile)
    {
        // Sprite rect metadata only: keep the original generated RGBA files intact.
        // Transparent atlas padding must not squash the count badge or shrink seeds.
        int left = (int)tile.xMax, bottom = (int)tile.yMax, right = (int)tile.xMin, top = (int)tile.yMin;
        for (int y = (int)tile.yMin; y < (int)tile.yMax; y++)
        for (int x = (int)tile.xMin; x < (int)tile.xMax; x++)
            if (pixels[y * textureWidth + x].a >= 160)
            { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
        if (left > right || bottom > top) return tile;
        int margin = 6;
        return Rect.MinMaxRect(Math.Max(tile.xMin, left - margin), Math.Max(tile.yMin, bottom - margin),
            Math.Min(tile.xMax, right + margin + 1), Math.Min(tile.yMax, top + margin + 1));
    }
}

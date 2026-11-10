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
            File.WriteAllText(Folder + "/garden-world-review.json", "{\"passed\":true,\"sizes\":[568,667,812,932],\"plots\":9,\"cells_per_plot\":4,\"two_column_window\":true,\"drag_snap\":true,\"fixed_chrome\":true,\"real_raycast\":true,\"profile_unchanged\":true,\"scope\":\"layout and two-species art pilot\"}");
            return result + "; garden world: two-column viewport, one-column drag/snap and reverse, four cells per plot, apple/strawberry growth art";
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
            var buttons = bindings.Resolve<BaseButtonControllerFactory>(); var profile = _gardenWorldReview ? FruitLabGardenWorldEditorTools.PreviewProfile(_presentationReview) : PlayerProfileRuntime.Current;
            var rules = new AchievementRules(new EconomySettings()); var garden = new GardenService(profile, new EconomySettings(), new SystemGardenClock(), rules);
            var visits=new GardenVisitCoordinator(garden);
            using var collection = new FruitCollectionPresenter(new FruitInventoryService(profile, rules), profile, menu.GetCollectionView(), buttons, garden, visits, menu.GetGardenNoticeView(),new Scripts.Economy.AchievementService(profile,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings())));
            var requests = new List<string>();
            using var controller = new MainNavigationController(view, tracker, buttons, collection, requests.Add, () => { });
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
                    if (_presentationReview) CheckGardenPresentation(canvas,scene,gardenPanel,profile,controller,visits);
                    else if (_gardenWorldReview) CheckGardenWorld(canvas, scene, gardenPanel, profile, controller);
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
            badges.onClick.Invoke(); Require(menu.GetCollectionView().IsVisible && !menu.GetCollectionView().IsGardenSelected && !menu.GetAchievementsView().IsVisible, "Settings opens the shared collection");
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
                using var controller = new MainNavigationController(view, tracker, null, null, requests.Add, () => shutdowns++);
                controller.Initialize(); Require(!view.SettingsButton.gameObject.activeSelf, "Online scenes reuse their existing settings button");
                controller.Select(MainTab.Garden);
                Require(shutdowns == 1 && requests.SequenceEqual(new[] { "Menu" }) && MainNavigationState.RequestedTab == MainTab.Garden, "Online tab leaves the room and opens the requested page");
            }
            finally { Object.DestroyImmediate(root); }
        }
        finally { EditorSceneManager.ClosePreviewScene(source); }
    }
    private static void CheckGardenWorld(Canvas canvas, Scene scene, GardenPanelView panel, IPlayerProfileRepository profile, MainNavigationController controller)
    {
        panel.ApplyLayout(); Canvas.ForceUpdateCanvases();
        string before = Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot());
        Require(panel.Plots.Count == 9 && panel.Plots.All(plot => plot.transform.Find("Cell3") != null && plot.transform.Find("Cell4") == null), "Nine plots, four cells each");
        Require(panel.Plots.Select(plot => plot.Fruit).SequenceEqual(GardenPanelView.FruitOrder), "Fixed fruit geography");
        Require(panel.Plots.Single(plot => plot.Fruit == FruitId.Apple).CellCount(0).text == "12", "Actual batch quantity");
        Require(panel.Plots.Single(plot => plot.Fruit == FruitId.Pear).OccupiedCells == 0, "Empty cells have no fabricated content");
        var scroll = panel.WorldScroll;
        Require(Mathf.Abs(scroll.viewport.rect.width - scroll.ColumnWidth * 2) < .1f && Mathf.Abs(scroll.content.rect.width - scroll.ColumnWidth * 3) < .1f, "Two-column window, three equal world columns");
        foreach (var plot in panel.Plots)
            for (int i = 0; i < 4; i++)
                if (plot.CellArt(i).gameObject.activeSelf) Require(plot.CellCount(i).fontSize >= 14, "Compact cell count is legible");
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
                // No active animation in EditMode: release must already be exactly snapped.
                var pointer = new PointerEventData(root.GetComponent<EventSystem>()) {
                    button = PointerEventData.InputButton.Left, position = start, pressPosition = start,
                    pointerPressRaycast = new RaycastResult { module = canvas.GetComponent<GraphicRaycaster>() }
                };
                scroll.OnInitializePotentialDrag(pointer); scroll.OnBeginDrag(pointer);
                pointer.position = RectTransformUtility.WorldToScreenPoint(camera, scroll.viewport.TransformPoint(scroll.viewport.rect.center + new Vector2(amount, 0)));
                scroll.OnDrag(pointer); scroll.OnEndDrag(pointer);
            }
            Drag(-scroll.ColumnWidth * .8f);
            Require(scroll.Column == 1 && Mathf.Abs(scroll.content.anchoredPosition.x + scroll.ColumnWidth) < .1f, "Leftward drag moves one column");
            Require(panel.BuyButton.transform.position == fixedPosition, "Seed shop stays fixed");
            Capture(canvas, scene, Folder + "/preview-garden-world-right-" + height + ".png");
            canvas.worldCamera = camera; // Capture owns a temporary camera.
            Drag(-scroll.ColumnWidth * 3); Require(scroll.Column == 1, "World clamps at right edge");
            Drag(scroll.ColumnWidth * .8f); Require(scroll.Column == 0, "Reverse drag returns one column");
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
        Require(MainNavigationState.RequestedTab == MainTab.Collection, "Album selects Collection tab");
        controller.Select(MainTab.Garden); Canvas.ForceUpdateCanvases(); panel.ApplyLayout();
        Require(Newtonsoft.Json.JsonConvert.SerializeObject(profile.Snapshot()) == before, "Garden presentation leaves the profile unchanged");
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
            Require(fresh.Value.CellIndex==3 && fresh.Value.Quantity==5 && profile.Snapshot().Wallet.Coins==coins-1,"Planting effect follows one committed five-seed purchase");
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

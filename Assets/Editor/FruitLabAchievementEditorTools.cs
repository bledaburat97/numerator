using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using Menu;
using Newtonsoft.Json;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object=UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeAchievements()
    {
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Menu.unity");bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity",OpenSceneMode.Additive);
        try
        {
            var menu=Find<MenuUIView>(scene);var parent=menu.GetCollectionView().transform.parent;
            FruitLabStyle.Font=menu.GetComponentInChildren<TMP_Text>(true).font;
            var previous=menu.GetAchievementsView();if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var root=new GameObject("Achievements",typeof(RectTransform),typeof(AchievementsView));
            var view=root.GetComponent<AchievementsView>();root.transform.SetParent(parent,false);
            FruitLabStyle.Stretch((RectTransform)root.transform);
            var rules=new AchievementRules(PlayerProfileRuntime.LoadSettings());view.Render(rules.Read(new PlayerProfileData()));view.ApplyLayout();Layer(root.transform);
            view.SetVisible(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Achievements.prefab");
            Object.DestroyImmediate(root);
            root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Achievements.prefab"),parent);
            view=root.GetComponent<AchievementsView>();FruitLabStyle.Stretch((RectTransform)root.transform);
            var entry=menu.GetAchievementsButton() as BaseButtonView ?? Button(menu.transform,"AchievementsButton",false);
            entry.SetText("Başarılar");Set(menu,"achievementsButton",entry);Set(menu,"achievementsView",view);
            Find<FruitLabMenuLayout>(scene).ApplyLayout();
            foreach(var component in scene.GetRootGameObjects().SelectMany(item=>item.GetComponentsInChildren<Component>(true)))
                if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Achievements menu saves");
            return "Saved editable Achievements.prefab and menu button";
        }
        finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }

    public static void ReviewAchievements(Canvas canvas,Camera camera)
    {
        string before=Profile();var errors=new List<string>();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            FruitLabAchievementRuleChecks.Review(Folder,PlayerProfileRuntime.LoadSettings(),PlayerProfileRuntime.CreateRewardRules());ReviewAchievementUi(canvas,camera);ReviewAchievementTarget(canvas,camera);
            Require(before==Profile(),"Achievement review preserves real PlayerPrefs");
            Require(errors.Count==0,"No Unity errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/achievements-review.json",JsonConvert.SerializeObject(new {passed=true,checks=new[]{
                "shared single/duel/garden grants", "source and lifetime targets", "multiple unlocks in one commit", "durable reward receipts", "restart and replay", "failed write rollback and retry", "backfill and config edits", "adjustments excluded", "four mobile UI sizes", "real menu/close raycasts", "live badge and powerup count refresh", "level-end goal layout", "real prefs unchanged"}},Formatting.Indented));
        }
        finally {Application.logMessageReceived-=collect;}
    }

    private static void ReviewAchievementUi(Canvas gameCanvas,Camera camera)
    {
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");GameObject clone=null,events=null;float ortho=camera.orthographicSize;
        gameCanvas.gameObject.SetActive(false);
        try
        {
            clone=Object.Instantiate(Find<FruitLabMenuLayout>(source).gameObject);SceneManager.MoveGameObjectToScene(clone,camera.gameObject.scene);
            var canvas=clone.GetComponent<Canvas>();
            // Isolate legacy modal/presenter checks; footer routing and raycasts are covered by navigation-review.
            clone.GetComponentInChildren<MainNavigationView>(true)?.gameObject.SetActive(false);canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rect=(RectTransform)clone.transform;rect.localScale=Vector3.one;rect.position=Vector3.zero;
            var menu=clone.GetComponentInChildren<MenuUIView>(true);var view=menu.GetAchievementsView();
            var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{var data=new PlayerProfileData();data.Progress.LevelId=17;return data;});var rules=new AchievementRules(PlayerProfileRuntime.LoadSettings());
            var service=new AchievementService(profiles,rules);var inventory=new FruitInventoryService(profiles,rules);
            var bindings=new DiContainer();bindings.Bind<IHapticController>().To<HapticController>().AsSingle();
            bindings.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
            events=new GameObject("AchievementUiEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(events,camera.gameObject.scene);var eventSystem=events.GetComponent<EventSystem>();
            var trackerRoot=new GameObject("AchievementUiTracker");trackerRoot.SetActive(false);trackerRoot.transform.SetParent(clone.transform,false);
            var tracker=trackerRoot.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;Field(tracker,"_levelId",17);
            var garden=new GardenService(profiles,new EconomySettings(),new SystemGardenClock(),rules);
            using var collection=new FruitCollectionPresenter(inventory,profiles,menu.GetCollectionView(),bindings.Resolve<BaseButtonControllerFactory>(),garden,new GardenVisitCoordinator(garden),menu.GetGardenNoticeView());
            new MenuUIController(tracker,new MemorySave(),bindings.Resolve<BaseButtonControllerFactory>(),menu);
            var powers=clone.GetComponentInChildren<PowerUpListHolderView>(true);
            using var powerCounts=new MenuPowerUpCountHolderController(powers,tracker,profiles);powerCounts.Initialize();
            using var header=new MenuHeaderController(clone.GetComponentInChildren<MenuHeaderView>(true),profiles);header.Initialize(tracker);
            using(var presenter=new AchievementsPresenter(service,profiles,view,menu.GetAchievementsButton(),bindings.Resolve<BaseButtonControllerFactory>()))
            {
                foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
                {
                    rect.sizeDelta=size;clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();camera.orthographicSize=Mathf.Max(size.y/2,size.x/(2*camera.aspect));
                    presenter.Open();Require(view.IsVisible,"Presenter opens achievements; the settings entry is covered by navigation-review");
                    Canvas.ForceUpdateCanvases();view.ApplyLayout();Canvas.ForceUpdateCanvases();
                    var scroll=view.GetComponentInChildren<ScrollRect>(true);scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
                    var badge=(RectTransform)scroll.content.Find("Badge2");var corners=new Vector3[4];badge.GetWorldCorners(corners);
                    Require(scroll.content.childCount==3,"Saved rows are rebound instead of duplicated at runtime");
                    var visible=scroll.viewport.rect;visible.xMin-=.1f;visible.yMin-=.1f;visible.xMax+=.1f;visible.yMax+=.1f;
                    Require(corners.All(corner=>visible.Contains((Vector2)scroll.viewport.InverseTransformPoint(corner))),"Last badge can be fully scrolled into view: "+size+" "+visible+" "+string.Join(",",corners.Select(corner=>scroll.viewport.InverseTransformPoint(corner).ToString())));
                    Require(((RectTransform)view.CloseButton.transform).rect.height>=44,"Close target stays at least 44 units");
                    TapGarden(canvas,camera,eventSystem,view.CloseButton);Require(!view.IsVisible,"Close restores menu interaction");
                }
                rect.sizeDelta=new Vector2(375,812);clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();camera.orthographicSize=Mathf.Max(406,375/(2*camera.aspect));
                presenter.Open();scrollTop(view);Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-achievements-empty.png");
                inventory.Grant("partial",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,8}});
                Require(view.GetComponentsInChildren<TMP_Text>().Any(label=>label.text=="8 / 10"),"Visible badge progress refreshes after shared grants");
                Capture(camera,Folder+"/preview-achievements-progress.png");
                inventory.Grant("harvest-badges",FruitGainSource.Garden,new Dictionary<FruitId,long>{{FruitId.Strawberry,2},{FruitId.Apple,40}});
                Require(view.GetComponentsInChildren<TMP_Text>().Count(label=>label.text=="Kazanıldı")==3,"All earned badges appear without per-fruit popups");
                Require(((TMP_Text)Get(powers,"lifePowerUpCount")).text=="1" && ((TMP_Text)Get(powers,"helperPowerUpCount")).text=="1","Badge rewards update menu power counts immediately");
                Capture(camera,Folder+"/preview-achievements-earned.png");presenter.Close();Capture(camera,Folder+"/preview-menu-achievements.png");
                presenter.Dispose();inventory.Grant("disposed",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Apple,1}});
            }
        }
        finally {if(events!=null)Object.DestroyImmediate(events);if(clone!=null)Object.DestroyImmediate(clone);EditorSceneManager.ClosePreviewScene(source);gameCanvas.gameObject.SetActive(true);camera.orthographicSize=ortho;}
    }
    private static void scrollTop(AchievementsView view)=>view.GetComponentInChildren<ScrollRect>(true).verticalNormalizedPosition=1;

    private static void ReviewAchievementTarget(Canvas canvas,Camera camera)
    {
        foreach(var size in new[]{new Vector2(320,568),new Vector2(375,812)})
        foreach(int crystals in new[]{0,2})
        {
            ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
            using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=4,NumOfCards=9,MaxNumOfTries=8}))
            {
                game.BuildActors();var view=canvas.GetComponentInChildren<LevelFinishPopupView>(true);
                view.SetOutcome(true,crystals);view.SetAchievementTarget("Meyve Koleksiyoncusu: 48/50");view.SetText("Kazandın!");view.SetStatus(true);view.GetText().rectTransform.localScale=Vector3.one;
                view.GetButton(LevelFinishButtonType.Game).SetText("Bölüm 2");view.GetButton(LevelFinishButtonType.Menu).SetText("Menü");
                Canvas.ForceUpdateCanvases();var goal=view.GetSubtitle();
                Require(goal.gameObject.activeSelf && goal.fontSize>=16 && !goal.isTextOverflowing,"Closest level-end goal is visible and readable");
                if(crystals>0)
                {
                    var bar=(RectTransform)((Component)view.GetVerticalCrystalProgressView()).transform;
                    Require(bar.anchoredPosition.y+bar.rect.yMax<=goal.rectTransform.anchoredPosition.y+goal.rectTransform.rect.yMin+.1f,"Goal does not overlap crystal progress");
                }
                Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-achievement-goal-"+(int)size.y+"-"+crystals+".png");view.SetStatus(false);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DG.Tweening;
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
    private const string CollectionPath="Assets/Prefabs/FruitCollection.prefab";
    public static string BakeCollection()
    {
        var source=SceneManager.GetSceneByPath("Assets/Scenes/Menu.unity");bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity",OpenSceneMode.Additive);
        try
        {
            var menu=Find<MenuUIView>(source);
            FruitLabStyle.Font=menu.GetComponentInChildren<TMP_Text>(true).font;
            var root=new GameObject("FruitCollection",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(FruitCollectionView));
            try
            {
                var view=root.GetComponent<FruitCollectionView>();
                FruitLabStyle.AddSurface(root,FruitLabStyle.Background).raycastTarget=true;
                var title=FruitLabStyle.CreateText("Title",root.transform,27,FruitLabStyle.Mint);title.text="Bahçem";title.fontStyle=FontStyles.Bold;
                var section=FruitLabStyle.CreateText("Section",root.transform,18,FruitLabStyle.Text);section.text="Koleksiyon";
                var summary=FruitLabStyle.CreateText("Summary",root.transform,14,FruitLabStyle.Text);
                foreach(var label in new[]{title,section,summary}){label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;}
                var cells=new List<FruitCollectionCell>();
                foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))
                {
                    var cell=new GameObject(fruit.ToString(),typeof(RectTransform));cell.transform.SetParent(root.transform,false);
                    FruitLabStyle.AddSurface(cell,FruitLabStyle.Panel).raycastTarget=false;
                    FruitLabStyle.Rim((RectTransform)cell.transform,.35f);
                    var card=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NonDraggableCardItem.prefab"),cell.transform);
                    card.name="FruitCard";
                    var name=FruitLabStyle.CreateText("Name",cell.transform,13,FruitLabStyle.Text);name.alignment=TextAlignmentOptions.Center;
                    var count=FruitLabStyle.CreateText("Amount",cell.transform,22,FruitLabStyle.Mint);count.fontStyle=FontStyles.Bold;count.alignment=TextAlignmentOptions.Center;
                    name.raycastTarget=count.raycastTarget=false;name.textWrappingMode=TextWrappingModes.Normal;
                    cells.Add(new FruitCollectionCell {Fruit=fruit,Root=(RectTransform)cell.transform,Card=card.GetComponent<NonDraggableCardItemView>(),Name=name,Amount=count});
                }
                var close=Button(root.transform,"CloseButton",false);close.SetText("Kapat");
                Field(view,"rect",root.transform);Field(view,"title",title);Field(view,"section",section);Field(view,"summary",summary);Field(view,"closeButton",close);Field(view,"cells",cells.ToArray());
                Field(view,"cardPrefab",AssetDatabase.LoadAssetAtPath<NonDraggableCardItemView>("Assets/Prefabs/NonDraggableCardItem.prefab"));
                ((RectTransform)root.transform).sizeDelta=new Vector2(375,812);
                view.Render(new PlayerProfileData().Collection);
                view.RenderAchievements(new AchievementRules(PlayerProfileRuntime.LoadSettings()).Read(new PlayerProfileData()));view.ApplyLayout();
                ConnectGardenCardPrefabs(root);
                foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.gameObject.layer=5;
                root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,CollectionPath);
            }
            finally {Object.DestroyImmediate(root);}
            var previous=menu.GetCollectionView();if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var collection=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CollectionPath),(Transform)Get(Find<FruitLabMenuLayout>(source),"safeArea"));
            collection.name="FruitCollection";FruitLabStyle.Stretch((RectTransform)collection.transform);
            var entry=menu.GetCollectionButton() as BaseButtonView;
            if(entry==null)entry=Button(menu.transform,"CollectionButton",false);
            entry.SetText("Bahçem");
            Set(menu,"collectionButton",entry);Set(menu,"collectionView",collection.GetComponent<FruitCollectionView>());
            menu.GetGardenNoticeView().ApplyLayout();menu.GetGardenNoticeView().Hide();
            Find<FruitLabMenuLayout>(source).ApplyLayout();
            ClosestBadgeView.Ensure(Find<FruitLabMenuLayout>(source).HomeContent).Render(new AchievementRules(PlayerProfileRuntime.LoadSettings()).Read(new PlayerProfileData()).OrderBy(goal=>goal.Target).FirstOrDefault());
            foreach(var component in source.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Component>(true)))
                if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(source);Require(EditorSceneManager.SaveScene(source),"Collection menu saves");
        }
        finally {if(opened)EditorSceneManager.CloseScene(source,true);}
        return "Saved FruitCollection.prefab and Bahçem menu entry";
    }

    private static void ConnectGardenCardPrefabs(GameObject root)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NonDraggableCardItem.prefab");
        foreach(var card in root.GetComponentsInChildren<NonDraggableCardItemView>(true))
        {
            if(PrefabUtility.IsPartOfPrefabInstance(card))continue;
            var old=(RectTransform)card.transform;var parent=old.parent;
            if(!Enum.TryParse<FruitId>(parent.name,out var fruit) && !Enum.TryParse<FruitId>(old.name,out fruit))continue;
            string name=old.name;int sibling=old.GetSiblingIndex();var position=old.localPosition;var rotation=old.localRotation;var scale=old.localScale;
            var size=old.sizeDelta;var min=old.anchorMin;var max=old.anchorMax;var pivot=old.pivot;
            Object.DestroyImmediate(card.gameObject);
            var replacement=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);replacement.name=name;replacement.transform.SetSiblingIndex(sibling);
            var rect=(RectTransform)replacement.transform;rect.anchorMin=min;rect.anchorMax=max;rect.pivot=pivot;rect.sizeDelta=size;
            rect.localPosition=position;rect.localRotation=rotation;rect.localScale=scale;
            var view=replacement.GetComponent<NonDraggableCardItemView>();view.Init((int)fruit);view.SetAlpha(1);
            foreach(var graphic in replacement.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
            foreach(var component in replacement.GetComponentsInChildren<Component>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }

    private static void BindCollectionTestRewards(DiContainer container)
    {
        container.Bind<IPlayerProfileRepository>().FromInstance(PlayerProfileRuntime.Current);
        container.Bind<WalletRewardRules>().FromInstance(PlayerProfileRuntime.CreateRewardRules());
        container.Bind<AchievementRules>().FromInstance(new AchievementRules(new EconomySettings()));
        container.Bind<IAchievementService>().To<AchievementService>().AsSingle();
        container.Bind<IGameWinRewardService>().To<GameWinRewardService>().AsSingle();
    }
    private static IGameWinRewardService CollectionTestRewards(Fixture game)=>CollectionFixtureRewards(game,out _);
    private static IGameWinRewardService CollectionFixtureRewards(Fixture game,out IPlayerProfileRepository repository)
    {
        var profile=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{
            var data=new PlayerProfileData();data.Progress.LevelId=game.Tracker.GetLevelId();
            data.Wallet.Coins=game.Tracker.GetCoinCount();data.Wallet.CrystalProgress=game.Tracker.GetCrystalProgressCount();data.Wallet.NextReward=game.Tracker.GetCurrentRewardType();
            foreach(RewardType type in Enum.GetValues(typeof(RewardType)))data.Wallet.PowerUps[type]=game.Tracker.GetPowerUpCount(type);
            return data;
        });
        profile.Changed+=()=>typeof(LevelTracker).GetMethod("ApplyProfile",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game.Tracker,new object[]{profile.Snapshot()});
        repository=profile;return new GameWinRewardService(profile,PlayerProfileRuntime.CreateRewardRules(),new AchievementRules(new EconomySettings()));
    }

    public static void ReviewCollection(Canvas canvas,Camera camera)
    {
        string before=Profile();var errors=new List<string>();var oldUpdate=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        Application.LogCallback collect=(text,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(text);};
        Application.logMessageReceived+=collect;
        try
        {
            using(GameTelemetry.CaptureForTests(_=>{},()=>0))
            {
                ReviewCollectionRewards();
                ReviewCollectionRounds(canvas,camera);
                ReviewCollectionLayouts(canvas,camera);
            }
            Require(before==Profile(),"Collection review preserves real PlayerPrefs");
            Require(errors.Count==0,"No Unity errors in collection review: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/collection-review.json",JsonConvert.SerializeObject(new {passed=true,
                checks=new[]{"atomic wallet/progression/fruit award","durable duplicate receipt","different match identities","source totals","failed write and retry","old profile upgrade","normal/bomb/growing/fail rounds","round snapshot identity","nine visible fruit cells","four mobile sizes","zero and large counts","menu entry/close raycasts","live refresh and disposed presenter","real PlayerPrefs unchanged"}},Formatting.Indented));
        }
        finally {Application.logMessageReceived-=collect;DOTween.defaultUpdateType=oldUpdate;}
    }

    private static void ReviewCollectionRewards()
    {
        var initial=new PlayerProfileData();initial.Progress.LevelId=20;initial.Wallet.Coins=10;initial.Wallet.CrystalProgress=5;
        var storage=new MemoryPlayerProfileStorage();var profiles=new PlayerProfileRepository(storage,()=>initial.Copy());
        var rewards=new GameWinRewardService(profiles,PlayerProfileRuntime.CreateRewardRules(),new AchievementRules(new EconomySettings()));
        string single=Guid.NewGuid().ToString("N"),match=Guid.NewGuid().ToString("N");
        var result=rewards.SinglePlayer(single,20,new[]{1,3,4},2,1);
        var data=profiles.Snapshot();
        Require(data.Progress.LevelId==21 && data.Wallet.Coins==12 && data.Wallet.CrystalProgress==0
            && data.Collection[FruitId.Apple].Current==1 && data.Collection[FruitId.Strawberry].Current==1 && data.Collection[FruitId.Banana].Current==0
            && data.Wallet.PowerUps.Values.Sum()==1,"Success commits wallet, crystals, progression and only answer fruit together");
        var fresh=new GameWinRewardService(new PlayerProfileRepository(storage,()=>throw new Exception("No remigration")),PlayerProfileRuntime.CreateRewardRules(),new AchievementRules(new EconomySettings()));
        var repeated=fresh.SinglePlayer(single,20,new[]{1,3,4},2,1);
        Require(repeated.IsReplay && repeated.PreviousCoins==10 && repeated.NextLevelNumber==22,"A fresh service returns the original durable outcome");
        bool conflict=false;try {fresh.SinglePlayer(single,20,new[]{2,3,4},2,1);}catch(InvalidOperationException){conflict=true;}
        Require(conflict,"An identity cannot be reused for another answer");
        rewards.Duel(match,1,new[]{1,2,3},RewardType.Bomb);
        rewards.Duel(match,2,new[]{1,2,3},RewardType.Life);
        rewards.Duel(Guid.NewGuid().ToString("N"),1,new[]{1,2,3},RewardType.Revealing);
        data=profiles.Snapshot();
        Require(data.Collection[FruitId.Apple].Current==4 && data.Collection[FruitId.Apple].EarnedBySource[FruitGainSource.SinglePlayer]==1
            && data.Collection[FruitId.Apple].EarnedBySource[FruitGainSource.Duel]==3 && data.Progress.LevelId==21,"Duel rematches and new matches share inventory without changing solo progression");
        long revision=data.Revision;Require(rewards.Duel(match,1,new[]{1,2,3},RewardType.Bomb).IsReplay && profiles.Snapshot().Revision==revision,"Repeated duel outcome grants neither powerup nor fruit twice");

        string path=Folder+"/CollectionTests/"+Guid.NewGuid().ToString("N")+".json";bool fail=false;
        var atomic=new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("Interrupted collection write");});
        var disk=new PlayerProfileRepository(atomic,()=>initial.Copy());disk.Snapshot();
        var diskRewards=new GameWinRewardService(disk,PlayerProfileRuntime.CreateRewardRules(),new AchievementRules(new EconomySettings()));string identity=Guid.NewGuid().ToString("N");
        fail=true;bool failed=false;
        try {diskRewards.SinglePlayer(identity,20,new[]{1,2},1,1);}catch(IOException){failed=true;}
        Require(failed && disk.Snapshot().Wallet.Coins==10 && disk.Snapshot().Collection[FruitId.Apple].Current==0 && disk.Snapshot().Transactions.Count==0,"Failed writes cannot partially grant fruit or currency");
        fail=false;diskRewards.SinglePlayer(identity,20,new[]{1,2},1,1);
        var reloaded=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("No reset"));
        Require(reloaded.Snapshot().Collection[FruitId.Apple].Current==1 && new GameWinRewardService(reloaded,PlayerProfileRuntime.CreateRewardRules(),new AchievementRules(new EconomySettings())).SinglePlayer(identity,20,new[]{1,2},1,1).IsReplay,"Atomic receipt survives a fresh disk load");
        var old=initial.Copy();old.Version=1;
        PlayerProfileValidation.Upgrade(old);PlayerProfileValidation.Validate(old);
        Require(old.Version==PlayerProfileData.CurrentVersion && old.Wallet.Coins==10,"Version one upgrades without resetting balances");
    }

    private static void ReviewCollectionRounds(Canvas canvas,Camera camera)
    {
        foreach(string mode in new[]{"normal","bomb","growing-early","growing-full","fail"})
        {
            ClearPreviewActors(canvas);
            bool growing=mode.StartsWith("growing");
            var definition=new LevelData {NumOfBoardHolders=growing?2:4,NumOfCards=9,MaxNumOfTries=10,Mechanic=growing?LevelMechanic.Growing:LevelMechanic.Normal};
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20);Field(game.Tracker,"_bombPowerUpCount",1);game.BuildActors();game.Gate.Clear();
                var award=CollectionFixtureRewards(game,out var profiles);
                var completion=new SinglePlayerRoundCompletion(game.Tracker,game.Round,(MemorySave)Get(game,"_memory"),game._saves,game.Target,award,new Scripts.Economy.AchievementService(profiles,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings())));
                game.Results.LevelSuccessEvent+=(s,e)=>completion.Complete(true);
                if(mode=="bomb")
                {
                    var bomb=new BombPowerUpController(game.PowerMessages,game.Board,game.Placement,game.Target,game._info,game._counts,game.Round);
                    var flags=TutorialProgress.Keys.ToDictionary(key=>key,key=>1);
                    using(TutorialProgress.UseTestValues(flags)) {game._powers.Refresh();Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Bomb));Tick(.5f);}
                    Require(game.Target.GetTargetCardsList().Count==3,"Bomb changes the awarded answer size");
                }
                if(mode=="growing-full")
                {
                    using(var rules=GrowingRules(game,definition,GrowthFor(game,definition)))
                    {
                        rules.Attach(new LevelMechanicsPresenter(game.View,game.Board),null);rules.Begin(LevelMechanic.Growing);
                        for(int miss=0;miss<4;miss++)SubmitSplit(game,game.Target.GetTargetCardsList().AsEnumerable().Reverse().ToArray());
                        Require(game.Target.GetTargetCardsList().Count==4,"Growth opens exactly four awardable fruit slots");
                    }
                }
                var answer=game.Target.GetTargetCardsList().ToArray();
                if(mode=="normal")
                {
                    SubmitSplit(game,answer.Reverse().ToArray());
                    Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out var snapshot)
                        && snapshot.RoundId==game._saves.GetLevelSaveData().RoundId,"Real checkpoint snapshots keep the round identity");
                    var restored=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(snapshot));
                    game._saves.SetLevelSaveDataAsSaved(restored);((MemorySave)Get(game,"_memory")).Save(restored);
                }
                if(mode=="fail") {Require(completion.Complete(false).EarnedFruits.Count==0 && profiles.Snapshot().Collection.Values.All(value=>value.Current==0),"Failure awards and presents no fruit");continue;}
                for(int slot=0;slot<answer.Length;slot++)Require(game.Placement.TryPlaceCardOnBoard(answer[slot]-1,slot),"Winning collection placement");
                Tick(.5f);game.Check();
                Require(completion.Complete(true).EarnedFruits.SequenceEqual(answer),"The end presenter receives the actual awarded answer: "+mode);
                Require(profiles.Snapshot().Collection.Values.Sum(value=>value.Current)==answer.Length
                    && answer.All(fruit=>profiles.Snapshot().Collection[(FruitId)fruit].Current==1),"The real winning event awards the current answer before animation: "+mode);
                string id=game._saves.GetLevelSaveData().RoundId;
                var saved=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(game._saves.GetLevelSaveData()));
                Require(saved.RoundId==id && !string.IsNullOrWhiteSpace(id),"Round identity survives serialization");
            }
        }
    }

    private static void ReviewCollectionLayouts(Canvas gameCanvas,Camera camera)
    {
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");GameObject clone=null,events=null;
        float oldOrtho=camera.orthographicSize;
        gameCanvas.gameObject.SetActive(false);
        try
        {
            clone=Object.Instantiate(Find<FruitLabMenuLayout>(source).gameObject);SceneManager.MoveGameObjectToScene(clone,camera.gameObject.scene);
            var canvas=clone.GetComponent<Canvas>();
            // Isolate legacy modal/presenter checks; footer routing and raycasts are covered by navigation-review.
            clone.GetComponentInChildren<MainNavigationView>(true)?.gameObject.SetActive(false);canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rect=(RectTransform)clone.transform;rect.localScale=Vector3.one;rect.position=Vector3.zero;
            events=new GameObject("CollectionTestEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(events,camera.gameObject.scene);
            var menu=clone.GetComponentInChildren<MenuUIView>(true);var view=menu.GetCollectionView();
            var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>new PlayerProfileData());var inventory=new FruitInventoryService(profiles,new AchievementRules(new EconomySettings()));
            var bindings=new DiContainer();bindings.Bind<IHapticController>().To<HapticController>().AsSingle();
            bindings.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
            using(var presenter=new FruitCollectionPresenter(inventory,profiles,view,bindings.Resolve<BaseButtonControllerFactory>(),new GardenService(profiles,new EconomySettings(),new SystemGardenClock(),new AchievementRules(new EconomySettings())),new GardenVisitCoordinator(new GardenService(profiles,new EconomySettings(),new SystemGardenClock(),new AchievementRules(new EconomySettings()))),menu.GetGardenNoticeView(),new Scripts.Economy.AchievementService(profiles,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings()))))
            {
                var trackerRoot=new GameObject("CollectionMenuTracker");trackerRoot.SetActive(false);trackerRoot.transform.SetParent(clone.transform,false);
                var tracker=trackerRoot.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;
                var ui=new MenuUIController(tracker,new MemorySave(),bindings.Resolve<BaseButtonControllerFactory>(),menu);
                foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
                {
                    camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2f*camera.aspect));
                    rect.sizeDelta=size;clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();Layer(clone.transform);Draw(camera);
                    presenter.Open();Require(view.IsVisible,"Presenter opens the garden screen; footer entry is covered by navigation-review");view.SelectTab(GardenTab.Collection);
                    view.ApplyLayout();Layer(clone.transform);Draw(camera);
                    var cells=(FruitCollectionCell[])Get(view,"cells");Require(cells.Length==9,"All nine fruits are available in the scrollable collection");
                    foreach(var cell in cells)
                    {
                        var points=new Vector3[4];cell.Root.GetWorldCorners(points);
                        var content=cell.Root.parent as RectTransform;var contentBounds=content.rect;
                        contentBounds.xMin-=.1f;contentBounds.yMin-=.1f;contentBounds.xMax+=.1f;contentBounds.yMax+=.1f;
                        Require(points.All(point=>contentBounds.Contains(content.InverseTransformPoint(point))),"Collection cells fit their scroll content");
                        foreach(var label in new[]{cell.Name,cell.Amount}){label.ForceMeshUpdate();Require(!label.isTextOverflowing,"Fruit name and quantity stay readable: "+label.text);}
                    }
                    var collectionScroll=view.GetComponentsInChildren<ScrollRect>().Single();
                    collectionScroll.verticalNormalizedPosition=1;Layer(clone.transform);Draw(camera);
                    var lastCorners=new Vector3[4];cells.Last().Root.GetWorldCorners(lastCorners);var visibleBounds=collectionScroll.viewport.rect;
                    visibleBounds.xMin-=.2f;visibleBounds.yMin-=.2f;visibleBounds.xMax+=.2f;visibleBounds.yMax+=.2f;
                    Require(lastCorners.All(point=>visibleBounds.Contains(collectionScroll.viewport.InverseTransformPoint(point))),"Last fruit row is fully reachable by scrolling");
                    collectionScroll.verticalNormalizedPosition=1;Draw(camera);
                    if(size.x==375 && size.y==812)Capture(camera,Folder+"/preview-collection-empty.png");
                    var data=new PointerEventData(events.GetComponent<EventSystem>()) {position=RectTransformUtility.WorldToScreenPoint(camera,menu.GetSinglePlayerButton().GetButtonRectTransform().position)};
                    var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(data,hits);
                    Require(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(view.transform),"Collection consumes taps before underlying play buttons");
                    TapCollectionButton(canvas,camera,events.GetComponent<EventSystem>(),(BaseButtonView)view.CloseButton);Require(!view.IsVisible,"Close restores the menu without loading another scene");
                }
                rect.sizeDelta=new Vector2(375,812);camera.orthographicSize=Mathf.Max(406,375/(2f*camera.aspect));clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();presenter.Open();view.SelectTab(GardenTab.Collection);
                inventory.Grant("ui-grant",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,1000},{FruitId.Apple,2}});
                var updated=(FruitCollectionCell[])Get(view,"cells");
                Require(updated.Single(cell=>cell.Fruit==FruitId.Strawberry).Amount.text=="1.000","Open collection refreshes from the common inventory");
                Layer(clone.transform);Capture(camera,Folder+"/preview-collection-earned.png");presenter.Close();
                Capture(camera,Folder+"/preview-menu-collection-entry.png");
                presenter.Dispose();inventory.Grant("ui-after-close",FruitGainSource.Duel,new Dictionary<FruitId,long>{{FruitId.Apple,1}});
            }
        }
        finally {if(events!=null)Object.DestroyImmediate(events);if(clone!=null)Object.DestroyImmediate(clone);EditorSceneManager.ClosePreviewScene(source);gameCanvas.gameObject.SetActive(true);camera.orthographicSize=oldOrtho;}
    }
    private static void TapCollectionButton(Canvas canvas,Camera camera,EventSystem events,BaseButtonView button)
    {
        Layer(canvas.transform);Draw(camera);
        var rect=button.GetButtonRectTransform();
        var pointer=new PointerEventData(events) {position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();
        foreach(var raycaster in canvas.GetComponentsInChildren<GraphicRaycaster>())
            if(raycaster.isActiveAndEnabled)raycaster.Raycast(pointer,hits);
        hits=hits.OrderByDescending(hit=>hit.sortingOrder).ThenByDescending(hit=>hit.depth).ThenBy(hit=>hit.distance).ToList();
        Require(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject)==button.gameObject,
            "Collection entry and close receive actual UI raycasts: "+button.name+"; hits="+string.Join(",",hits.Take(6).Select(hit=>hit.gameObject.name+"/"+hit.sortingOrder+"/"+hit.depth)));
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
}

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeCollectionHub()
    {
        const string path="Assets/Resources/Art/UI/Badge.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=256;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        typeof(FruitLabStyle).GetField("_icons",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
        Require(FruitLabStyle.Icon("Badge")!=null,"Reusable badge sprite imported");
        return BakeCollection();
    }
    public static void ReviewCollectionHub(Canvas gameCanvas,Camera camera)
    {
        string before=Profile();var errors=new List<string>();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            FruitLabAchievementRuleChecks.Review(Folder,PlayerProfileRuntime.LoadSettings(),PlayerProfileRuntime.CreateRewardRules());
            ReviewCollectionRewards();ReviewCollectionRounds(gameCanvas,camera);
            ReviewCollectionHubUi(gameCanvas,camera);
            Require(before==Profile(),"Real player preferences are unchanged");
            Require(errors.Count==0,"No collection errors: "+string.Join("; ",errors));
            File.WriteAllText(Folder+"/collection-hub-review.json",JsonConvert.SerializeObject(new {passed=true,goals=5,sizes=new[]{568,667,812,932},
                checks=new[]{"single/duel/manual harvest lifetime totals","exclude adjustments and unharvested crops","five badge-only goals","legacy reward receipts preserved","restart and failed-write rollback","home closest unfinished target","home click focuses badge","garden album and footer reset to fruit summary","one scroll and reused rows","pending to earned live movement","all completed hides home goal","real pointer clicks","real player preferences unchanged"}},Formatting.Indented));
        }
        finally {Application.logMessageReceived-=collect;}
    }
    private static void ReviewCollectionHubUi(Canvas gameCanvas,Camera camera)
    {
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");GameObject clone=null,events=null;
        float ortho=camera.orthographicSize;var previous=MainNavigationState.RequestedTab;
        gameCanvas.gameObject.SetActive(false);
        try
        {
            clone=Object.Instantiate(Find<FruitLabMenuLayout>(source).gameObject);SceneManager.MoveGameObjectToScene(clone,camera.gameObject.scene);
            var canvas=clone.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rect=(RectTransform)clone.transform;rect.localScale=Vector3.one;rect.position=Vector3.zero;
            events=new GameObject("CollectionHubEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(events,camera.gameObject.scene);
            var eventSystem=events.GetComponent<EventSystem>();var menu=clone.GetComponentInChildren<MenuUIView>(true);var view=menu.GetCollectionView();
            var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>new PlayerProfileData());
            var settings=PlayerProfileRuntime.LoadSettings();var rules=new AchievementRules(settings);var service=new AchievementService(profiles,rules);
            var inventory=new FruitInventoryService(profiles,rules);var garden=new GardenService(profiles,settings,new SystemGardenClock(),rules);
            var bindings=new DiContainer();bindings.Bind<IHapticController>().To<HapticController>().AsSingle();
            bindings.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
            var buttons=bindings.Resolve<BaseButtonControllerFactory>();
            using var collection=new FruitCollectionPresenter(inventory,profiles,view,buttons,garden,new GardenVisitCoordinator(garden),menu.GetGardenNoticeView(),service);
            var trackerRoot=new GameObject("CollectionHubTracker");trackerRoot.SetActive(false);trackerRoot.transform.SetParent(clone.transform,false);
            var tracker=trackerRoot.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;Field(tracker,"_levelId",17);
            var navigation=MainNavigationView.Ensure(canvas);
            using var controller=new MainNavigationController(navigation,tracker,buttons,collection,_=>{});
            MainNavigationState.RequestedTab=MainTab.Home;controller.Initialize();
            new MenuUIController(tracker,new MemorySave(),buttons,menu);
            var home=ClosestBadgeView.Ensure(clone.GetComponent<FruitLabMenuLayout>().HomeContent);
            using var closest=new ClosestBadgePresenter(service,profiles,controller,home);closest.Initialize();
            inventory.Grant("initial-single",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,8}});
            inventory.Grant("initial-duel",FruitGainSource.Duel,new Dictionary<FruitId,long>{{FruitId.Apple,1}});
            inventory.Grant("admin-not-collected",FruitGainSource.Adjustment,new Dictionary<FruitId,long>{{FruitId.Apple,500}});
            inventory.Spend("spend-one",new Dictionary<FruitId,long>{{FruitId.Strawberry,1}});
            foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
            {
                rect.sizeDelta=size;clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();navigation.ApplyLayout();
                camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));
                controller.Select(MainTab.Home);Layer(clone.transform);Draw(camera);
                Require(home.gameObject.activeInHierarchy && home.GetComponentsInChildren<TMP_Text>().Any(text=>text.text=="9/10"),"One nearest home goal counts duel and retains spent fruit");
                var card=(RectTransform)home.transform;var play=menu.GetSinglePlayerButton().GetButtonRectTransform();
                Require(card.rect.height>=48 && card.rect.height<play.rect.height && card.rect.width<play.rect.width,"Home target is smaller than Play with a comfortable tap area");
                var powerRect=clone.GetComponentInChildren<PowerUpListHolderView>(true).GetRectTransform();
                var bounds=new Vector3[4];powerRect.GetWorldCorners(bounds);
                float powerBottom=card.parent.InverseTransformPoint(bounds[0]).y;
                float cardTop=card.anchoredPosition.y+card.rect.height*.5f;
                Require(cardTop<=powerBottom+((RectTransform)card.parent).rect.height*.5f-8,"Home goal does not overlap the power-up strip");
                foreach(var candidate in service.GetAchievements())
                {
                    home.Render(candidate);
                    foreach(var text in home.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Require(!text.isTextOverflowing,"Home text fits: "+text.text);}
                }
                home.Render(service.Closest());
                Capture(camera,Folder+"/preview-collection-home-"+(int)size.y+".png");
                TapGarden(canvas,camera,eventSystem,home.GetComponent<Button>());view.ApplyLayout();Layer(clone.transform);Draw(camera);
                var scroll=view.GetComponentsInChildren<ScrollRect>().Single();
                var focused=scroll.content.Find("Achievement-garden-owner-10") as RectTransform;
                Require(view.IsVisible && !view.IsGardenSelected && scroll.verticalNormalizedPosition<.99f,"Home opens and scrolls directly to the selected badge");
                var rowCorners=new Vector3[4];focused.GetWorldCorners(rowCorners);
                Require(rowCorners.All(point=>Expanded(scroll.viewport.rect,1).Contains((Vector2)scroll.viewport.InverseTransformPoint(point))),"Focused goal is fully visible at every phone size");
                Capture(camera,Folder+"/preview-collection-focused-"+(int)size.y+".png");
                TapGarden(canvas,camera,eventSystem,navigation.GetTab(MainTab.Collection));
                Require(scroll.verticalNormalizedPosition>.99f,"Collection footer opens at fruit summary");
                var cells=(FruitCollectionCell[])Get(view,"cells");
                Require(cells.Length==9 && cells.Single(cell=>cell.Fruit==FruitId.Strawberry).Amount.text=="8" && cells.Single(cell=>cell.Fruit==FruitId.Apple).Amount.text=="1","Fruit grid uses eligible lifetime totals, including duel but excluding adjustment");
                Require(scroll.content.GetComponentsInChildren<TMP_Text>().Any(text=>text.text=="Sıradaki rozetler"),"Pending badges share the fruit scroll");
                foreach(var text in view.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Require(!text.isTextOverflowing,"Collection text fits: "+text.text);}
                Capture(camera,Folder+"/preview-collection-hub-"+(int)size.y+".png");
                scroll.verticalNormalizedPosition=0;Layer(clone.transform);Draw(camera);
                Capture(camera,Folder+"/preview-collection-bottom-"+(int)size.y+".png");
                TapGarden(canvas,camera,eventSystem,navigation.GetTab(MainTab.Garden));
                TapGarden(canvas,camera,eventSystem,((GardenPanelView)view.Garden).CollectionButton);
                Require(view.IsVisible && !view.IsGardenSelected && scroll.verticalNormalizedPosition>.99f,"Garden album routes to the common collection at fruit summary");
            }
            inventory.Grant("complete-nearest",FruitGainSource.Garden,new Dictionary<FruitId,long>{{FruitId.Apple,1}});
            Require(service.GetAchievements().Count(goal=>goal.Unlocked)==1,"Completed harvest unlocks the ten-fruit badge");
            var content=view.GetComponentsInChildren<ScrollRect>().Single().content;
            var owner=content.Find("Achievement-garden-owner-10");
            Require(owner.gameObject.activeSelf && !owner.Find("Progress").gameObject.activeSelf,"Completed badge moves to the earned collection without progress or power-up claim");
            Require(content.Cast<Transform>().Count(child=>child.name.StartsWith("Achievement-"))==5,"Repeated page opens and awards reuse exactly five badge rows");
            controller.OpenCollection("garden-owner-10");Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-collection-earned.png");
            inventory.Grant("finish-all",FruitGainSource.Duel,new Dictionary<FruitId,long>{{FruitId.Strawberry,92},{FruitId.Watermelon,100},{FruitId.Apple,1298}});
            controller.Select(MainTab.Home);Require(!home.gameObject.activeSelf,"Home card disappears when all five badges have been earned");
            Require(profiles.Snapshot().Wallet.PowerUps.Values.All(amount=>amount==0),"No extra power-ups are awarded for the five badge-only goals");
            controller.OpenCollection();Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-collection-all-earned.png");
        }
        finally
        {
            MainNavigationState.RequestedTab=previous;
            if(events!=null)Object.DestroyImmediate(events);if(clone!=null)Object.DestroyImmediate(clone);
            EditorSceneManager.ClosePreviewScene(source);gameCanvas.gameObject.SetActive(true);camera.orthographicSize=ortho;
        }
    }
    private static Rect Expanded(Rect rect,float amount){rect.xMin-=amount;rect.yMin-=amount;rect.xMax+=amount;rect.yMax+=amount;return rect;}
}

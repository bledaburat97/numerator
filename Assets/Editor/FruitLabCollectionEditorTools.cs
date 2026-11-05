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
                ((RectTransform)root.transform).sizeDelta=new Vector2(375,812);
                view.Render(new PlayerProfileData().Collection);view.ApplyLayout();
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
            Find<FruitLabMenuLayout>(source).ApplyLayout();
            foreach(var component in source.GetRootGameObjects().SelectMany(go=>go.GetComponentsInChildren<Component>(true)))
                if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(source);Require(EditorSceneManager.SaveScene(source),"Collection menu saves");
        }
        finally {if(opened)EditorSceneManager.CloseScene(source,true);}
        return "Saved FruitCollection.prefab and Bahçem menu entry";
    }

    private static void BindCollectionTestRewards(DiContainer container)
    {
        container.Bind<IPlayerProfileRepository>().FromInstance(PlayerProfileRuntime.Current);
        container.Bind<WalletRewardRules>().FromInstance(PlayerProfileRuntime.CreateRewardRules());
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
        repository=profile;return new GameWinRewardService(profile,PlayerProfileRuntime.CreateRewardRules());
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
        var rewards=new GameWinRewardService(profiles,PlayerProfileRuntime.CreateRewardRules());
        string single=Guid.NewGuid().ToString("N"),match=Guid.NewGuid().ToString("N");
        var result=rewards.SinglePlayer(single,20,new[]{1,3,4},2,1);
        var data=profiles.Snapshot();
        Require(data.Progress.LevelId==21 && data.Wallet.Coins==12 && data.Wallet.CrystalProgress==0
            && data.Collection[FruitId.Apple].Current==1 && data.Collection[FruitId.Strawberry].Current==1 && data.Collection[FruitId.Banana].Current==0
            && data.Wallet.PowerUps.Values.Sum()==1,"Success commits wallet, crystals, progression and only answer fruit together");
        var fresh=new GameWinRewardService(new PlayerProfileRepository(storage,()=>throw new Exception("No remigration")),PlayerProfileRuntime.CreateRewardRules());
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
        var diskRewards=new GameWinRewardService(disk,PlayerProfileRuntime.CreateRewardRules());string identity=Guid.NewGuid().ToString("N");
        fail=true;bool failed=false;
        try {diskRewards.SinglePlayer(identity,20,new[]{1,2},1,1);}catch(IOException){failed=true;}
        Require(failed && disk.Snapshot().Wallet.Coins==10 && disk.Snapshot().Collection[FruitId.Apple].Current==0 && disk.Snapshot().Transactions.Count==0,"Failed writes cannot partially grant fruit or currency");
        fail=false;diskRewards.SinglePlayer(identity,20,new[]{1,2},1,1);
        var reloaded=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("No reset"));
        Require(reloaded.Snapshot().Collection[FruitId.Apple].Current==1 && new GameWinRewardService(reloaded,PlayerProfileRuntime.CreateRewardRules()).SinglePlayer(identity,20,new[]{1,2},1,1).IsReplay,"Atomic receipt survives a fresh disk load");
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
                var completion=new SinglePlayerRoundCompletion(game.Tracker,game.Round,(MemorySave)Get(game,"_memory"),game._saves,game.Target,award);
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
                if(mode=="fail") {completion.Complete(false);Require(profiles.Snapshot().Collection.Values.All(value=>value.Current==0),"Failure awards no fruit");continue;}
                for(int slot=0;slot<answer.Length;slot++)Require(game.Placement.TryPlaceCardOnBoard(answer[slot]-1,slot),"Winning collection placement");
                Tick(.5f);game.Check();
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
            var canvas=clone.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rect=(RectTransform)clone.transform;rect.localScale=Vector3.one;rect.position=Vector3.zero;
            events=new GameObject("CollectionTestEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(events,camera.gameObject.scene);
            var menu=clone.GetComponentInChildren<MenuUIView>(true);var view=menu.GetCollectionView();
            var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>new PlayerProfileData());var inventory=new FruitInventoryService(profiles);
            var bindings=new DiContainer();bindings.Bind<IHapticController>().To<HapticController>().AsSingle();
            bindings.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
            using(var presenter=new FruitCollectionPresenter(inventory,profiles,view,bindings.Resolve<BaseButtonControllerFactory>()))
            {
                var trackerRoot=new GameObject("CollectionMenuTracker");trackerRoot.SetActive(false);trackerRoot.transform.SetParent(clone.transform,false);
                var tracker=trackerRoot.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;
                var ui=new MenuUIController(tracker,new MemorySave(),bindings.Resolve<BaseButtonControllerFactory>(),menu,presenter);
                foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
                {
                    camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2f*camera.aspect));
                    rect.sizeDelta=size;clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();Layer(clone.transform);Draw(camera);
                    TapCollectionButton(canvas,camera,events.GetComponent<EventSystem>(),(BaseButtonView)menu.GetCollectionButton());Require(view.IsVisible,"Bahçem opens the collection");
                    view.ApplyLayout();Layer(clone.transform);Draw(camera);
                    var cells=(FruitCollectionCell[])Get(view,"cells");Require(cells.Length==9,"All nine fruits are visible");
                    foreach(var cell in cells)
                    {
                        var points=new Vector3[4];cell.Root.GetWorldCorners(points);
                        Require(points.All(point=>((RectTransform)view.transform).rect.Contains(view.transform.InverseTransformPoint(point))),"Collection cells fit mobile bounds");
                        foreach(var label in new[]{cell.Name,cell.Amount}){label.ForceMeshUpdate();Require(!label.isTextOverflowing,"Fruit name and quantity stay readable: "+label.text);}
                    }
                    if(size.x==375 && size.y==812)Capture(camera,Folder+"/preview-collection-empty.png");
                    var data=new PointerEventData(events.GetComponent<EventSystem>()) {position=RectTransformUtility.WorldToScreenPoint(camera,menu.GetSinglePlayerButton().GetButtonRectTransform().position)};
                    var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(data,hits);
                    Require(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(view.transform),"Collection consumes taps before underlying play buttons");
                    TapCollectionButton(canvas,camera,events.GetComponent<EventSystem>(),(BaseButtonView)view.CloseButton);Require(!view.IsVisible,"Close restores the menu without loading another scene");
                }
                rect.sizeDelta=new Vector2(375,812);camera.orthographicSize=Mathf.Max(406,375/(2f*camera.aspect));clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();presenter.Open();
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

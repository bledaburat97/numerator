using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Game;
using Scripts;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewManualNotes(Canvas canvas,Camera camera)=>ReviewUltraNotes(canvas,camera);

    private static void ValidateMultiplayerNotes(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);var level=new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=9};
        using(var game=new Fixture(canvas,camera,level))
        {
            Field(game.Tracker,"_levelId",7);Field(game.Tracker,"_gameOption",GameOption.MultiPlayer);game.BuildActors();game.Gate.Clear();game.UI.InitializeForMultiplayer();
            game.UI.SetButtonClickable(false,GameUIButtonType.Check);
            using(var assistance=AssistanceFor(game,level,out var view))
            using(var editor=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,assistance,game.Initial,game.Board))
            {
                assistance.Refresh();
                var history=canvas.GetComponentInChildren<ResultAreaView>(true);
                float rowHeight=history.GetRowHeight();
                Require(editor.TrySelect(0),"Multiplayer notes do not require the local player's turn");
                Require(Mathf.Abs(history.GetRowHeight()-rowHeight)<.01f,"Showing the multiplayer toolbar clips history without shrinking rows");
                ClickPositionIndicator(game,editor.GetPositionButton(3));editor.GetButton(2).onClick.Invoke();
                var layout=canvas.GetComponent<FruitLabGameLayout>();
                Require(editor.IsSelecting && layout.PowerUpArea.gameObject.activeSelf && assistance.State.Presence(1)==FruitPresence.Present,"Multiplayer opens the private edit row while keeping the selection");
                Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-multiplayer-notes.png");
                editor.Cancel();Require(!layout.PowerUpArea.gameObject.activeSelf,"Dismissing restores the multiplayer layout");
                Require(game.Placement.TryPlaceCardOnBoard(0,0),"Annotations never restrict card arrangement on remote turns");game.Check();
                Require(game.Round.GetTriedCardsList().Count==0,"Editing does not enable an opponent-turn guess");
            }
            game.View.SetOpponentInfoStatus(false);
        }
    }

    private static void ValidateNoteLayouts(Canvas canvas,Camera camera)
    {
        foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
        for(int slots=2;slots<=5;slots++)for(int fruits=5;fruits<=9;fruits++)
        {
            ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
            var level=new LevelData {NumOfBoardHolders=slots,NumOfCards=fruits,MaxNumOfTries=9};
            using(var game=new Fixture(canvas,camera,level))
            {
                Field(game.Tracker,"_levelId",7);game.BuildActors();game.Gate.Clear();
                using(var assistance=AssistanceFor(game,level,out var view))
                using(var editor=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,assistance,game.Initial,game.Board))
                {
                    var history=canvas.GetComponentInChildren<ResultAreaView>(true);var scroll=history.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                    assistance.Refresh();
                    for(int attempt=0;attempt<12;attempt++)
                    {
                        var cards=Enumerable.Range(0,slots).Select(slot=>(slot+attempt)%fruits+1).ToList();
                        int correct=cards.Where((fruit,slot)=>fruit==slot+1).Count();
                        game._area.AddResultBlock(new ResultBlockModel {finalNumbers=cards,correctPosCount=correct,wrongPosCount=cards.Count(fruit=>fruit<=slots)-correct});
                    }
                    assistance.Refresh();scroll.verticalNormalizedPosition=1;scroll.onValueChanged.Invoke(Vector2.up);
                    float ordinaryHeight=history.GetRowHeight(),ordinaryViewport=scroll.viewport.rect.height;
                    var beforeCorners=new Vector3[4];scroll.viewport.GetWorldCorners(beforeCorners);
                    Vector3 ordinaryTop=beforeCorners[1],ordinaryScale=history.transform.lossyScale;
                    var firstRow=(RectTransform)history.GetVisibleRow(0).transform;
                    Vector2 ordinaryRowSize=firstRow.rect.size;
                    var firstCard=history.GetVisibleRow(0).GetComponentsInChildren<BaseCardItemView>(true).First();
                    Vector2 ordinaryCardSize=((RectTransform)firstCard.transform).rect.size;
                    Require(editor.TrySelect(0),"Notebook can open across board/fruit layouts");
                    scroll.viewport.GetWorldCorners(beforeCorners);
                    Require(Mathf.Abs(history.GetRowHeight()-ordinaryHeight)<.01f
                        && (firstRow.rect.size-ordinaryRowSize).sqrMagnitude<.01f
                        && (((RectTransform)firstCard.transform).rect.size-ordinaryCardSize).sqrMagnitude<.01f
                        && (history.transform.lossyScale-ordinaryScale).sqrMagnitude<.0001f,"Editing preserves row, fruit and canvas scale");
                    Require(Vector3.Distance(ordinaryTop,beforeCorners[1])<.01f && ordinaryViewport-scroll.viewport.rect.height>1,
                        "Editing raises only the lower viewport boundary while its top stays fixed");
                    history.SetScrollPositionToBottom();Require(history.GetVisibleRow(11)!=null && scroll.enabled && scroll.vertical,"Clipped history can still reach the last guess");
                    scroll.verticalNormalizedPosition=1;scroll.onValueChanged.Invoke(Vector2.up);
                    Require(history.GetVisibleRow(0)!=null,"Clipped history can still reach the first guess");
                    var area=canvas.GetComponent<FruitLabGameLayout>().BoardArea;var corners=new Vector3[4];
                    for(int slot=0;slot<slots;slot++)
                    {
                        var button=editor.GetPositionButton(slot);button.GetRectTransform().GetWorldCorners(corners);
                        Vector2 min=area.InverseTransformPoint(corners[0]),max=area.InverseTransformPoint(corners[2]);
                        Require(button.GetRectTransform().rect.height==16 && min.y>=area.rect.yMin-.1f && max.y<=area.rect.yMax+.1f,"Thin position controls fit inside the reserved board footer");
                        ClickPositionIndicator(game,button);Require(editor.IsSelecting,"Repeated position clicks retain selection at every layout");
                    }
                    if(slots==5 && fruits==9){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-notes-5-9-"+(int)size.x+"-"+(int)size.y+".png");}
                    history.SetScrollPositionToBottom();
                    editor.Cancel();
                    Require(Mathf.Abs(history.GetRowHeight()-ordinaryHeight)<.01f && Mathf.Abs(scroll.viewport.rect.height-ordinaryViewport)<.01f
                        && history.GetVisibleRow(11)!=null,"Dismissing restores the original viewport without resizing its rows");
                    var content=(RectTransform)history.transform;
                    Require(content.anchoredPosition.y<=Mathf.Max(0,content.rect.height-scroll.viewport.rect.height)+.1f,"Dismissing clamps the old scroll end to avoid a bottom gap");
                }
            }
        }
        ((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
    }

    public static void ReviewSessionContracts(Canvas canvas, Camera camera)
    {
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        try
        {
            ValidateSessionAliases();
            int outcomes = 0;
            for (int slots = 2; slots <= 5; slots++)
            foreach (bool won in new[] {true,false})
            foreach (bool rewardsActive in new[] {true,false})
            {
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=slots,NumOfCards=9,MaxNumOfTries=9}))
                {
                    Field(game.Tracker,"_levelId",20);Field(game.Tracker,"_starCount",11);
                    Field(game.Tracker,"_giftStarCount",ConstantValues.NUM_OF_STARS_FOR_WILD-1);
                    game.BuildActors();game.Gate.Clear();
                    if (!rewardsActive)
                        for(int i=0;i<game.Round.GetLifeBarRewardInfoList().Count;i++)game.Round.SetLifeBarRewardStatus(i,false);
                    game.Round.AddTriedCards(Enumerable.Range(1,slots).ToList());
                    game.Round.GetActiveRewardCounts(out int coins,out int crystals);
                    var reward=game.Tracker.GetCurrentRewardType();int stock=game.Tracker.GetPowerUpCount(reward);
                    var saves=(MemorySave)Get(game,"_memory");saves.Save(new LevelSaveData());
                    var rewards=CollectionFixtureRewards(game,out var resultProfiles);
                    var completion=new SinglePlayerRoundCompletion(game.Tracker,game.Round,saves,game._saves,game.Target,rewards,new Scripts.Economy.AchievementService(resultProfiles,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings())));
                    var result=completion.Complete(won);
                    Require(result.Won==won && result.PreviousCoins==11 && result.PreviousCrystalProgress==ConstantValues.NUM_OF_STARS_FOR_WILD-1
                        && result.PreviousReward==reward && result.GuessCount==1,"Outcome captures the pre-award state independently of animations");
                    Require(game.Tracker.GetLevelId()==(won?21:20) && game.Tracker.GetCoinCount()==11+(won?coins:0)
                        && result.EarnedCoins==(won?coins:0) && result.EarnedCrystals==(won?crystals:0) && saves.Data==null,
                        "Only success awards earned tokens and progress; both outcomes clear the checkpoint before presentation");
                    Require(game.Tracker.GetPowerUpCount(reward)==stock+(won && crystals>0?1:0),"Crossing crystal capacity grants the powerup before any animation or claim button");
                    int balance=game.Tracker.GetCoinCount(),progress=game.Tracker.GetCrystalProgressCount();
                    Require(ReferenceEquals(result,completion.Complete(won)) && ReferenceEquals(result,completion.Complete(!won))
                        && game.Tracker.GetLevelId()==(won?21:20) && game.Tracker.GetCoinCount()==balance && game.Tracker.GetCrystalProgressCount()==progress,
                        "Duplicate or conflicting completion cannot award twice");

                    var popupView=canvas.GetComponentInChildren<LevelFinishPopupView>(true);
                    var popup=new LevelEndPopupController(popupView);
                    var popups=new ContractPopups();var animations=new ContractEndAnimations();var progression=new ContractProgression();
                    var bar=new VerticalCrystalProgressController(popupView.GetVerticalCrystalProgressView(),game.Tracker,new HapticController());
                    using(var presenter=new SinglePlayerEndPresenter(popups,game.UI,game._area,game.Target,new ContractCardInfo(),bar,popup,animations,animations))
                    {
                        presenter.Present(result,progression);
                        Require(popups.Ends==0 && animations.Success==won && (animations.Coins,animations.Crystals)==(result.EarnedCoins,result.EarnedCrystals),
                            "Presenter consumes the captured outcome and passes exact earned counts to the animation");
                        var crystalObject=(Component)popupView.GetVerticalCrystalProgressView();
                        Require(crystalObject.gameObject.activeSelf==(won && crystals>0),"Result popup shows the powerup bar only when crystals were earned");
                        popup.ChangeFadeButtons(0,1).Complete(true);
                        var button=((Component)popupView.GetButton(LevelFinishButtonType.Game)).GetComponentInChildren<BaseButtonView>(true);
                        Click(button);
                        Require(won?progression.Next==1 && progression.Retries==0:progression.Retries==1 && progression.Next==0,
                            "Real result button invokes the singleplayer progression contract");
                        presenter.PrepareRound();Tick(.5f);
                        Require(popups.RewardTips==0 && !animations.Sequence.IsActive(),"New rounds cancel pending end callbacks and animations");
                        presenter.Present(result,progression);Tick(.5f);
                        Require(popups.RewardTips==(won?1:0),"Reward explanation appears only after a completed success presentation");
                        presenter.Present(result,progression);presenter.Dispose();Tick(.5f);
                        Require(popups.RewardTips==(won?1:0) && game.Tracker.GetCoinCount()==balance && game.Tracker.GetCrystalProgressCount()==progress,
                            "Disposed or replayed presentation cannot alter rewards or open late tutorials");
                    }
                    completion.BeginRound();completion.Complete(false);
                    Require(game.Tracker.GetLevelId()==(won?21:20),"The next failed round starts a fresh completion without advancing progress");
                    outcomes++;
                }
            }
            ValidateSessionLifecycle(canvas,camera);
            ValidateAlternatePresentations(canvas,camera);
            Require(errors.Count==0,"No Unity errors in session contracts review: "+string.Join(" | ",errors));
            Debug.Log("FRUIT_LAB_SESSION_CONTRACTS_PASS: "+outcomes+" outcomes; real-installer new/resumed games, first-level cards and settings, deferred powerup lookups, zero/full rewards, overflow, duplicate completion, real buttons, cancellation, alternate HUD/result/indicator implementations.");
        }
        finally {DOTween.defaultUpdateType=previous;Application.logMessageReceived-=collect;}
    }

    private static void ValidateSessionAliases()
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        try
        {
            foreach(var mode in new[]{GameOption.SinglePlayer,GameOption.MultiPlayer})
            {
                using var isolatedProfile=Scripts.Economy.PlayerProfileRuntime.UseForTests(
                    new Scripts.Economy.PlayerProfileRepository(new Scripts.Economy.MemoryPlayerProfileStorage(),()=>{
                        var profile=new Scripts.Economy.PlayerProfileData();profile.Progress.LevelId=20;
                        foreach(RewardType type in Enum.GetValues(typeof(RewardType)))profile.Wallet.PowerUps[type]=3;
                        return profile;
                    }));
                var root=new GameObject("ContractBindingTracker");root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);
                ILevelSession session=null;PositionAssistanceController assistance=null;DisposableManager owner=null;
                try
                {
                    var tracker=root.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;Field(tracker,"_gameOption",mode);
                    Field(tracker,"_levelId",20);Field(tracker,"_revealingPowerUpCount",3);Field(tracker,"_bombPowerUpCount",3);
                    var container=new DiContainer();
                    container.Bind<Context>().FromInstance(Find<SceneContext>(scene));
                    container.Bind<ILevelTracker>().FromInstance(tracker);container.Bind<IGameSaveService>().FromInstance(new MemorySave());
                    BindCollectionTestRewards(container);
                    container.Bind<Scripts.Economy.PowerUpUseJournal>().AsSingle();
                    container.Bind<IHapticController>().To<HapticController>().AsSingle();
                    container.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
                    var installer=Find<GameMonoInstaller>(scene);
                    typeof(MonoInstallerBase).GetProperty("Container",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(installer,container);
                    installer.InstallBindings();container.Rebind<IGamePopupCreator>().FromInstance(new ContractPopups());
                    session=container.Resolve<ILevelSession>();
                    Require(mode==GameOption.SinglePlayer?session is SinglePlayerSession:session is MultiplayerSession,"Installer selects the session for the requested mode");
                    Require(ReferenceEquals(container.Resolve<IGameUIController>(),container.Resolve<IMultiplayerHudPresentation>())
                        && ReferenceEquals(container.Resolve<ILevelEndPopupController>(),container.Resolve<IMultiplayerResultPresentation>())
                        && ReferenceEquals(container.Resolve<IInitialCardAreaController>(),container.Resolve<IInitialCardAreaPresentation>()),
                        "Capabilities alias the same live UI, popup and card controller instances");
                    if(mode==GameOption.SinglePlayer)Require(ReferenceEquals(session,container.Resolve<ISinglePlayerProgression>()),"Progression belongs to the active singleplayer session");
                    else Require(!(session is ISinglePlayerProgression),"Multiplayer does not expose singleplayer progression");
                    assistance=container.Resolve<PositionAssistanceController>();container.ResolveRoots();
                    var lifetimes=container.ResolveAll<IDisposable>();
                    Require(lifetimes.Count==lifetimes.Distinct().Count() && lifetimes.Count(value=>ReferenceEquals(value,session))==1,
                        "Scene services and the active session have exactly one disposal registration");
                    Require(lifetimes.OfType<HintProvider>().Count()==1 && lifetimes.OfType<ResultManager>().Count()==1
                        && lifetimes.OfType<PositionAssistanceView>().Count()==1 && lifetimes.OfType<TutorialBootstrapper>().Count()==1,
                        "Input, hint, note-view and tutorial subscriptions belong to the scene container");
                    owner=container.Instantiate<DisposableManager>();
                    if(mode==GameOption.SinglePlayer)ValidateInstallerStartup(container,session);
                }
                finally {if(owner!=null)owner.Dispose();else{assistance?.Dispose();session?.Dispose();}UnityEngine.Object.DestroyImmediate(root);}
            }
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
    }

    private static void ValidateInstallerStartup(DiContainer container,ILevelSession session)
    {
        // SceneContext injects scene components separately from resolving its bindings.
        container.Inject(container.Resolve<ILevelDataCreator>());
        container.Inject(container.Resolve<ITargetNumberCreator>());
        container.Resolve<IGamePowerUpAreaController>().Initialize();
        // FromMethod has already returned and its pooled InjectContext was reset.
        // Use the real scene installer: hand-built service closures miss this failure.
        session.Begin(LevelEntryMode.New);
        for(int i=0;i<35;i++)Tick(.1f);
        container.Resolve<TutorialInputGate>().Clear();
        Require(session.State==LevelFlowState.Playing,"Real installer finishes the game-scene intro");
        var uses=container.Resolve<PowerUpUseService>();
        Require(uses.CanUse(RewardType.Revealing,0),"Deferred ice-state lookup survives InjectContext disposal");
        var profile=Scripts.Economy.PlayerProfileRuntime.Current;
        profile.Update(data=>data.Wallet.PowerUps[RewardType.Revealing]=3);
        Require(uses.TryApply(RewardType.Revealing,0)==PowerUpUseResult.Applied,
            "Deferred snapshot lookup resolves and commits a powerup after game startup");
        var save=container.Resolve<IGameSaveService>().GetSavedLevel();
        Require(save!=null && save.PowerUpUseRevision==1 && save.CardItemInfoList.Any(card=>card.isLocked),
            "Live installer stores the revealed card and powerup revision");
        session.Begin(LevelEntryMode.Resume);
        container.Resolve<TutorialInputGate>().Clear();
        Require(session.State==LevelFlowState.Playing && !uses.CanUse(RewardType.Revealing,0),
            "Re-entering the saved game restores the locked card without a startup error");
        var tracker=(LevelTracker)container.Resolve<ILevelTracker>();
        container.Resolve<IGameSaveService>().DeleteSave();Field(tracker,"_levelId",0);
        session.Begin(LevelEntryMode.New);for(int i=0;i<35;i++)Tick(.1f);
        Require(session.State==LevelFlowState.Playing
            && container.Resolve<IBoardHolderCountManager>().GetBoardHolderCount()==2
            && container.Resolve<ITargetNumberCreator>().GetTargetCardsList().SequenceEqual(new[]{4,1})
            && container.Resolve<ICardItemInfoManager>().GetCardItemInfoList().Count==5,
            "Level one creates its tutorial board, fixed answer and all five fruit cards");
        var ui=container.Resolve<IGameUIController>();int opened=0;
        EventHandler onSettings=(_,__)=>opened++;ui.OpenSettings+=onSettings;
        try
        {
            var view=(IGameUIView)Get(ui,"_view");Click((BaseButtonView)view.GetSettingsButton());
            Require(opened==1,"Settings remains clickable after a new game-scene startup");
        }
        finally {ui.OpenSettings-=onSettings;}
    }

    private static void ValidateSessionLifecycle(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);
        var definition=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9,Mechanic=LevelMechanic.Normal};
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",1);
            var saves=(MemorySave)Get(game,"_memory");
            LevelSaveData checkpoint;
            using(var session=CreateContractSession(game,definition))
            {
                session.Begin(LevelEntryMode.New);Tick(.5f);game.Gate.Clear();
                Require(session.State==LevelFlowState.Playing,"The selected singleplayer session starts without an orchestrator");
                game.Target.RevealMultiplayerAnswer(new[]{1,2,3});
                for(int i=0;i<3;i++)Require(game.Placement.TryPlaceCardOnBoard(i,i),"Winning board placement");
                Tick(.4f);game.Check();
                var popups=(ContractPopups)Get(Get(session,"_endPresenter"),"_popups");
                Require(popups.Ends==1 && popups.LastOutcome && popups.EndLevel==1,"The completed tutorial belongs to the old level before progress advances");
                Require(session.IsGameOver && session.State==LevelFlowState.EndingSuccess && game.Tracker.GetLevelId()==2 && saves.Data==null,
                    "Real winning guesses commit before the success animation completes");
                for(int i=0;i<120;i++)Tick(.1f);
                ClickContractGameButton(canvas);Require(session.State==LevelFlowState.Transitioning,"Next-level action enters the transition");
                for(int i=0;i<15;i++)Tick(.1f);game.Gate.Clear();
                Require(!session.IsGameOver && session.State==LevelFlowState.Playing && game.Tracker.GetLevelId()==2,"Next-level action builds a fresh playable round");
                game._guess.FailRound();session.FinishRound(false);for(int i=0;i<35;i++)Tick(.1f);
                Require(popups.Ends==2 && !popups.LastOutcome && popups.EndLevel==2,"Failure ends its tutorial once without advancing the level");
                Require(session.State==LevelFlowState.EndingFail && saves.Data==null,"Failure clears the old checkpoint without advancing progress");
                ClickContractGameButton(canvas);for(int i=0;i<20;i++)Tick(.1f);game.Gate.Clear();
                Require(session.State==LevelFlowState.Playing && game.Tracker.GetLevelId()==2 && game.Round.GetTriedCardsList().Count==0,
                    "Retry rebuilds the same level and resets its round state");
                var answer=game.Target.GetTargetCardsList();
                for(int i=0;i<3;i++)Require(game.Placement.TryPlaceCardOnBoard(answer[(i+1)%3]-1,i),"Resume checkpoint guess placement");
                Tick(.4f);game.Check();Tick(1.5f);
                var snapshots=new GameSaveSnapshotProvider(game.Target,game._info,session,game._counts,game.Round);
                Field(snapshots,"_tracker",game.Tracker);Field(snapshots,"_levels",new TestLevel(definition));Field(snapshots,"_saveData",game._saves);
                Require(snapshots.TryCreateSnapshot(out checkpoint),"Active sessions remain saveable through ILevelSession");
                saves.Save(checkpoint);
            }
            game.Initial.ClearInitialCardHolders();game.Board.ClearBoardHolders();game._area.History.Clear();
            using(var resumed=CreateContractSession(game,definition))
            {
                resumed.Begin(LevelEntryMode.Resume);game.Gate.Clear();
                Require(resumed.State==LevelFlowState.Playing && !resumed.IsGameOver
                    && game.Round.GetRemainingGuessCount()==checkpoint.RemainingGuessCount
                    && game.Target.GetTargetCardsList().SequenceEqual(checkpoint.TargetCards)
                    && game.Round.GetTriedCardsList().Count==checkpoint.TriedCardsList.Count,
                    "Resume restores the actual checkpoint after the session contract change");
            }
        }
    }

    private static SinglePlayerSession CreateContractSession(Fixture game,LevelData definition,Scripts.Economy.IPlayerProfileStorage storage=null)
    {
        var popups=new ContractPopups {Level=game.Tracker.GetLevelId};var cardInfo=new ContractCardInfo();
        var popup=new LevelEndPopupController(game.Canvas.GetComponentInChildren<LevelFinishPopupView>(true));
        var life=new LifeBarController(game.Canvas.GetComponentInChildren<LifeBarView>(true));
        var fade=new FadePanelController(game.Canvas.GetComponentInChildren<FadePanelView>(true));
        var bar=new VerticalCrystalProgressController(game.Canvas.GetComponentInChildren<VerticalCrystalProgressView>(true),game.Tracker,new HapticController());
        var failure=new LevelFailAnimationManager(popup,game.Initial,fade,game.UI,life,game._area,game.Board,game.Placement);
        var success=new LevelSuccessAnimationManager(popup,fade,new LevelSuccessBoardAnimationController(game.Board,game.Placement),
            new LevelSuccessCleanupAnimationController(game.Initial,game.UI,life,game._area,game.Placement),
            new LevelSuccessRewardCollectionAnimationController(life,game.UI,bar,popup));
        var builder=new RoundBuilder(new TestLevel(definition),game._saves,game.Board,game._counts,game.Target,game._area,life,game.Initial,game.UI,game._powers,
            (ICardItemLocator)Get(game.Placement,"_cardItemLocator"),game._guess,game._info,cardInfo,game.Placement,(ICardInteractionManager)Get(game,"_interaction"),game.Results,game.Round,game._area.History);
        var saves=(MemorySave)Get(game,"_memory");
        var rewards=CollectionFixtureRewards(game,out var profiles,storage);
        var session=new SinglePlayerSession(game.Tracker,new TestLevel(definition),saves,game._saves,game._counts,popups,game._guess,game.Results,game.Round,
            new SinglePlayerRoundCompletion(game.Tracker,game.Round,saves,game._saves,game.Target,rewards,new Scripts.Economy.AchievementService(profiles,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings()))),new SinglePlayerEndPresenter(popups,game.UI,game._area,game.Target,cardInfo,bar,popup,success,failure),
            null,builder,new LevelPresentation(popups,game._powers,bar,new ContractStartAnimation(),popup,failure,fade));
        game.Results.TurnFlow.Outcome=session;return session;
    }
    private static void ClickContractGameButton(Canvas canvas)
    {
        var button=canvas.GetComponentInChildren<LevelFinishPopupView>(true).GetButton(LevelFinishButtonType.Game);
        Click(((Component)button).GetComponentInChildren<BaseButtonView>(true));
    }

    private static void ValidateAlternatePresentations(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);
        var level=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9};
        using(var game=new Fixture(canvas,camera,level))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();
            var cards=new ContractInitialPresentation(game.Initial);
            using(var view=new PositionAssistanceView(cards,game.View,canvas.GetComponentInChildren<ResultAreaView>(true),game.UI,null))
            {
                var state=new PositionDeductionService(7,3);state.ApplyGuess(1,new[]{1,2,3},0,3);
                view.SetEnabled(true);view.Render(state,null);
                Require(cards.Rendered==7 && cards.Layouts>0 && cards.GetHolderView(0).Indicators[0].IsExcluded,
                    "A presentation implementation that is not InitialCardAreaController receives all indicators and fruit states");
                view.SetEnabled(false);view.Render(null,null);
                Require(!canvas.GetComponentInChildren<InitialCardAreaView>(true).HasPositionIndicators,"Disabling the alternate presentation resets its layout");
            }

            var popup=new LevelEndPopupController(canvas.GetComponentInChildren<LevelFinishPopupView>(true));
            var hud=new ContractMultiplayerHud();var result=new ContractMultiplayerResult();var network=new ContractMultiplayer();
            var presenter=new MultiplayerRoundPresenter(new LifeBarController(canvas.GetComponentInChildren<LifeBarView>(true)),game.Initial,game.UI,popup,network,hud,result);
            presenter.BeginWaiting();Require(hud.Text.Contains("bekleniyor"),"Waiting status is delivered through the HUD contract");
            network.PublicState=new MultiplayerPublicState(1,1,MultiplayerPhase.Playing,1,0,60,0);
            presenter.RefreshState(network.State);
            Require(hud.LocalTurn && hud.Text=="Sıra sende" && hud.Deadline==60,"Local turn and timer reach an alternate HUD implementation");
            network.PublicState=new MultiplayerPublicState(1,2,MultiplayerPhase.Playing,2,0,120,0);
            presenter.RefreshState(network.State);
            Require(!hud.LocalTurn && hud.Text=="Sıra: Oyuncu 2" && hud.Deadline==120 && game.Placement.TryPlaceCardOnBoard(0,0),
                "Remote turns retain card arrangement and deliver the correct HUD deadline");
            int guesses=game.Round.GetTriedCardsList().Count;game.Check();
            Require(game.Round.GetTriedCardsList().Count==guesses,"Guess submission remains disabled on the remote turn");
            presenter.ShowError("Deneme hatası");Require(hud.Text=="Deneme hatası","Request errors reach the alternate HUD");
            network.PublicState=new MultiplayerPublicState(1,2,MultiplayerPhase.Finished,2,1,0,2);
            presenter.RefreshState(network.State);Require(hud.Text=="Yeni oyun teklifi var","Rematch notice reaches the HUD contract");
            presenter.ShowOutcome(new MultiplayerOutcome(1,1,new[]{1,2,3},false,RewardType.Bomb),true);
            Require(result.Won && result.Reward==RewardType.Bomb,"Winner rewards reach the result contract without a concrete popup cast");
            presenter.ShowOutcome(new MultiplayerOutcome(1,2,new[]{1,2,3},false,RewardType.Bomb),false);
            Require(!result.Won && result.Reward==null,"A losing result does not show the opponent's reward as earned");
            presenter.ShowDisconnected();Require(result.Disconnect==network.DisconnectMessage,"Disconnect details reach the result contract");
        }
    }

    private sealed class ContractProgression : ISinglePlayerProgression
    {
        public int Retries,Next;
        public void Retry()=>Retries++;
        public void GoToNextLevel()=>Next++;
    }
    private sealed class ContractStartAnimation : ILevelStartAnimationManager
    {
        public Sequence StartLevelStartAnimation()=>DOTween.Sequence().AppendInterval(.2f);
    }
    private sealed class ContractEndAnimations : ILevelSuccessAnimationManager,ILevelFailAnimationManager
    {
        public void Cancel() { }
        public bool Success;public int Coins,Crystals;public Sequence Sequence;
        public Sequence SuccessLevelAnimation(int earnedCoinCount,int earnedCrystalCount,int previousCoinCount,int previousCrystalProgressCount,RewardType rewardTypeBeforeCollection,RewardType? directReward=null,Scripts.Economy.CrystalRewardPlan crystalPlan=null)
        {Success=true;Coins=earnedCoinCount;Crystals=earnedCrystalCount;return Sequence=DOTween.Sequence().AppendInterval(.25f);}
        public Sequence FailLevelAnimation(){Success=false;Coins=Crystals=0;return Sequence=DOTween.Sequence().AppendInterval(.25f);}
        public Sequence FadeOutResultArea(float duration)=>DOTween.Sequence().AppendInterval(duration);
        public Sequence SendBoardHolders(float duration)=>DOTween.Sequence().AppendInterval(duration);
    }
    private sealed class ContractCardInfo : ICardItemInfoPopupController
    {
        public event EventHandler<ProbabilityButtonClickedEventArgs> ProbabilityButtonClickedEvent {add{} remove{}}
        public event EventHandler<HolderIndicatorClickedEventArgs> HolderIndicatorClickedEvent {add{} remove{}}
        public IBaseButtonController GetProbabilityButton(ProbabilityType type)=>null;
        public IBaseButtonController GetHolderIndicatorButton(int index)=>null;
        public void Initialize(){}public void Unsubscribe(){}public void ClearCardHolderIndicatorButtons(){}
    }
    private sealed class ContractPopups : IGamePopupCreator
    {
        public bool LastOutcome;public int RewardTips,Ends,EndLevel;public Func<int> Level;
        public void EndTutorials(bool success){LastOutcome=success;Ends++;EndLevel=Level?.Invoke()??-1;}
        public void ShowRewardTutorials(int coins,int crystals)=>RewardTips++;
        public void Initialize(){}public void PrepareTutorials(){}public void StartTutorials(){}
        public void CreateNewGameOfferPopup(){}public void CreateDisconnectionPopup(){}
        public void CloseNotAbleToMovePopup(){}public void CloseNewGameOfferPopup(){}
        public RectTransform GetSafeAreaRectTransform()=>null;public RectTransform GetCanvasRectTransform()=>null;
    }
    private sealed class ContractInitialPresentation : IInitialCardAreaPresentation
    {
        private readonly IInitialCardAreaPresentation _inner;public int Rendered,Layouts;
        public ContractInitialPresentation(IInitialCardAreaPresentation inner)=>_inner=inner;
        public int GetCardCount()=>_inner.GetCardCount();
        public IInitialHolderView GetHolderView(int index)=>_inner.GetHolderView(index);
        public void SetPositionIndicators(bool enabled)=>_inner.SetPositionIndicators(enabled);
        public void SetCardPresence(int index,FruitPresence presence){Rendered++;_inner.SetCardPresence(index,presence);}
        public void RefreshLayout(){Layouts++;_inner.RefreshLayout();}
    }
    private sealed class ContractMultiplayerHud : IMultiplayerHudPresentation
    {
        public void SetMultiplayerAvatar(int avatar) { }
        public string Text;public double Deadline;public bool LocalTurn;
        public void SetMultiplayerStatus(string text,double deadline){Text=text;Deadline=deadline;}
        public void SetMultiplayerTurn(string text,double deadline,bool localTurn){Text=text;Deadline=deadline;LocalTurn=localTurn;}
        public void TickPresentation(){}
    }
    private sealed class ContractMultiplayerResult : IMultiplayerResultPresentation
    {
        public bool Won;public RewardType? Reward;public string Disconnect;
        public void SetMultiplayerResult(bool won,bool disconnected,RewardType? reward){Won=won;Reward=reward;}
        public void SetMultiplayerOpponentLeft(string message=null)=>Disconnect=message;
    }
    private sealed class ContractMultiplayer : IMultiplayerGameController
    {
        public RewardSaveState RewardState=>RewardSaveState.Completed;
        public void RetryReward(){}
        public event EventHandler<RewardSaveState> RewardSaveChanged {add{}remove{}}
        public MultiplayerPublicState PublicState;
        public ulong LocalPlayerId=>1;public double ServerTime=>0;public MultiplayerPublicState State=>PublicState;
        public bool IsLocalTurn=>State.Phase==MultiplayerPhase.Playing && State.CurrentPlayer==LocalPlayerId;
        public bool LocalRematchRequested=>false;public string StatusNotice=>"";public string DisconnectMessage=>"Kurucu ayrıldı";
        public int GetPlayerAvatar(ulong player) => 0;
        public string GetPlayerName(ulong player)=>"Oyuncu "+player;
        public void Initialize(){}public void BoardReady(int round){}public void SubmitGuess(int[] cards){}public void RequestRematch(){}public void Stop(){}
        public event EventHandler<int> RoundPreparing {add{} remove{}}
        public event EventHandler<MultiplayerFeedback> GuessAccepted {add{} remove{}}
        public event EventHandler<MultiplayerPublicState> StateChanged {add{} remove{}}
        public event EventHandler<MultiplayerOutcome> MatchEnded {add{} remove{}}
        public event EventHandler<string> RequestRejected {add{} remove{}}
    }
}

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewModelPresentation(Canvas canvas,Camera camera)
    {
        var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        string prefs=Profile();
        try
        {
            ReviewSceneFreeMechanics();
            ClearPreviewActors(canvas);
            var definition=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9};
            using(var game=new Fixture(canvas,camera,definition))
            {
                game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3});
                Field(game.Round,"_remainingGuessCount",1);int failed=0;game._guess.LevelFailEvent+=(s,e)=>failed++;
                for(int card=3;card<6;card++)game.Place(card);
                game.Check();
                Require(failed==1 && game.Round.GetRemainingGuessCount()==0,"The final wrong guess fails synchronously before the life animation advances");
                game._guess.Initialize();Tick(1.2f);game._guess.FailRound();
                Require(failed==1,"A layout rebuild and tween cancellation cannot lose or duplicate failure");
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3});
                var query=(IBoardPlacementQuery)Get(game.Placement,"_boardPlacementQuery");
                var hints=new HintProvider(game._guess,game.Target,game._info,game.Initial,query,game.Placement,game.Locks);
                var save=(MemorySave)Get(game,"_memory");var di=new DiContainer();
                di.Bind<IGameSaveSnapshotProvider>().FromInstance(GrowingSnapshot(game,definition));
                Field(hints,"_snapshots",di.Resolve<LazyInject<IGameSaveSnapshotProvider>>());Field(hints,"_saves",save);
                var token=canvas.GetComponentsInChildren<CrystalView>(true).First();
                bool started=(bool)typeof(HintProvider).GetMethod("TryApplyRevealHint",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(hints,new object[]{token});
                Require(started,"A crystal prepares a reveal");
                int card=game._info.GetCardItemInfoList().FindIndex(fact=>fact.isLocked);int slot=game._info.GetCardItemInfoList()[card].possibleCardHolderIndicatorIndexes.Single();
                Require(save.Data.CardItemInfoList[card].isLocked && query.GetPlacedCardIndexesOnBoard()[slot]==card,"The reveal and board placement are committed and saved before flight");
                DOTween.KillAll(false);
                game._saves.SetLevelSaveDataAsSaved(save.Data);game.Initial.ClearInitialCardHolders();game.Board.ClearBoardHolders();game.BuildActors();
                Require(game._info.GetCardItemInfoList()[card].isLocked && query.GetPlacedCardIndexesOnBoard()[slot]==card,"Cancelling the crystal flight and resuming retains the granted reveal");
                int otherSlot=(slot+1)%3;
                Require(!game.Placement.PresentLockedCard(otherSlot,otherSlot),"A stale presentation cannot create a new logical lock");
                Require(game.Placement.TryPlaceCardOnBoard(6,otherSlot),"Place an unlocked card before a reveal");Tick(.4f);
                Require(game.Placement.TryRevealAndLockCard(otherSlot,otherSlot),"Reveal commits the correct card over an occupied slot");Tick(.4f);
                Require(!query.TryGetOccupiedBoardHolderIndexOfCard(6,out _) && game.Initial.GetRectTransformOfCardItem(6).parent!=game.Board.GetRectTransformOfGarden(otherSlot),"The displaced card returns visually without a second model mutation");
                var history=game._area.History;history.Add(new ResultBlockModel{finalNumbers=new List<int>{4,2,3},correctPosCount=2,wrongPosCount=0});
                history.Reveal(new GuessEvaluation(1,new[]{4,2,3},new[]{1,2,3}));
                game._area.RemoveResultBlocks();
                Require(history.HistoryCount==1 && history.GetHistoryEntry(0).hintStates!=null,"Clearing visual result rows preserves public evidence");
                using(var notes=AssistanceFor(game,definition,out _,false))
                {notes.ActivateUltra();Require(notes.State.Presence(4)==FruitPresence.Absent,"Ultra reads model evidence while the result screen is empty");}
                var progressView=canvas.GetComponentInChildren<VerticalCrystalProgressView>(true);
                progressView.ApplyLabStyle();
                var progress=new VerticalCrystalProgressController(progressView,game.Tracker,null);
                var planned=new Scripts.Economy.CrystalRewardPlan{Capacity=6,PreviousProgress=5,FinalProgress=2,InitialReward=RewardType.Ultra,NextReward=RewardType.Pair,PowerUps=new List<RewardType>{RewardType.Ultra}};
                Field(game.Tracker,"_currentRewardType",RewardType.Bomb);
                progress.PrepareForCollection(5,planned.InitialReward);progress.AddCrystals(null,planned).Complete(true);
                Require(progressView.transform.Find("CrystalCount").GetComponent<TMPro.TMP_Text>().text=="2 / 6","The bar consumes the prepared final state rather than current wallet rules");
                var popupView=canvas.GetComponentInChildren<LevelFinishPopupView>(true);var popup=new LevelEndPopupController(popupView);
                var rewardView=PowerUpRewardView.Ensure(canvas);
                using(var rewards=new PowerUpRewardPresenter(rewardView,game.Tracker,null))
                {
                    Field(popup,"_rewardPresentation",rewards);int closed=0;
                    popup.PresentPowerUpRewards(new[]{RewardType.Ultra,RewardType.Helper});popup.AfterRewardPresentation(()=>closed++);
                    Tick(.5f);rewardView.ClaimButton.onClick.Invoke();Tick(1.2f);Tick(.5f);
                    Require(rewards.IsPresenting && closed==0,"The second awarded power is presented before continuation");
                    rewardView.ClaimButton.onClick.Invoke();Tick(.9f);
                    Require(!rewards.IsPresenting && closed==1,"An ordered reward batch finishes once after its final claim");
                }
                var settings=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<SettingsPopupView>("Assets/Prefabs/SettingsPopup.prefab"),canvas.transform);
                try
                {
                    int privacy=0;var controller=new SettingsPopupController(_=>{},()=>{},()=>privacy++);
                    var buttons=(BaseButtonControllerFactory)Get(game,"_buttons");
                    for(int repeat=1;repeat<=2;repeat++)
                    {
                        controller.Initialize(settings,()=>{},()=>{},()=>{},game.Tracker,buttons);
                        settings.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button=>button.name=="PrivacyButton").onClick.Invoke();
                        Require(privacy==repeat,"Privacy requests are routed by the controller once, including view reuse");
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(settings.gameObject);}
            }
            Require(prefs==Profile(),"Model/presentation regressions preserve real player preferences");
            System.IO.File.WriteAllText(Folder+"/mvp-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=true,checks=new[]{"failure before animation","layout cancellation","crystal commit and resume before callback","history survives view clear","scene-free growth and dynamite","card facts notify without view","authoritative crystal reward plan"}},Newtonsoft.Json.Formatting.Indented));
        }
        finally{DOTween.defaultUpdateType=previous;}
    }
    private static void ReviewSceneFreeMechanics()
    {
        var tracker=new RulesTracker();
        void Create(LevelMechanic mechanic,out TestLevel level,out LevelSaveDataManager save,out RoundStateManager round,out BoardHolderCountManager counts,out BoardCardIndexManager slots,out CardItemInfoManager info,out RulesTarget target)
        {
            level=new TestLevel(new LevelData{NumOfBoardHolders=mechanic==LevelMechanic.Growing?2:5,NumOfCards=9,MaxNumOfTries=9,Mechanic=mechanic});
            save=new LevelSaveDataManager(level);save.CreateDefaultLevelSaveData();
            round=new RoundStateManager(level,save);round.Initialize();counts=new BoardHolderCountManager(save,level);counts.Initialize();
            slots=new BoardCardIndexManager();slots.InitializeCardIndexesOnBoardHolders(counts.GetBoardHolderCount());
            info=new CardItemInfoManager(save,counts);info.Initialize();target=new RulesTarget(Enumerable.Range(1,counts.GetBoardHolderCount()));
        }
        Create(LevelMechanic.Growing,out var level,out var save,out var round,out var counts,out var slots,out var info,out var target);
        save.GetLevelSaveData().GrowingBoard.FullTarget=new List<int>{1,2,3,4};
        var growth=new GrowingBoardService(level,tracker,save,target,counts,slots,info,round);
        Require(!growth.TryAdvance(new GuessEvaluation(1,new[]{8,9},target.GetTargetCardsList())) && growth.TryAdvance(new GuessEvaluation(2,new[]{8,9},target.GetTargetCardsList())),"Growth calculates the two-miss boundary without a scene presenter");
        Require(counts.GetBoardHolderCount()==3 && target.GetTargetCardsList().Count==3 && slots.GetPlacedCardIndexesOnBoard().Count==3,"All growth state is complete without spawning a holder");
        Create(LevelMechanic.Dynamite,out level,out save,out round,out counts,out slots,out info,out target);
        save.GetLevelSaveData().Dynamite.BoardIndex=1;
        using(var danger=new DynamiteService(level,tracker,save,target,info,new CardLockService(info,slots,slots),round))
        {
            danger.Prepare();danger.Resolve(new GuessEvaluation(1,new[]{8,2,9,6,7},target.GetTargetCardsList()));
            Require(danger.Current.Disarmed && info.GetCardItemInfoList()[1].isLocked && slots.GetPlacedCardIndexesOnBoard()[1]==1,"Dynamite disarms and locks the model without card GameObjects");
        }
        Create(LevelMechanic.Dynamite,out level,out save,out round,out counts,out slots,out info,out target);save.GetLevelSaveData().Dynamite.BoardIndex=1;
        using(var danger=new DynamiteService(level,tracker,save,target,info,new CardLockService(info,slots,slots),round))
        {for(int turn=1;turn<=4;turn++)danger.Resolve(new GuessEvaluation(turn,new[]{6,7,8,9,1},target.GetTargetCardsList()));Require(danger.Current.Exploded && round.GetRemainingGuessCount()==0,"Dynamite exhausts logical lives without a presenter or tween");}
        int changes=0;info.Changed+=()=>changes++;info.OnProbabilityButtonClicked(0,ProbabilityType.Certain);info.OnHolderIndicatorClicked(0,0);
        Require(changes==2,"Card information publishes both note edits without accessing scene objects");
        var profile=new Scripts.Economy.PlayerProfileData();profile.Wallet.CrystalProgress=1;
        var rules=new Scripts.Economy.WalletRewardRules(2,(r,l)=>true,(r,l)=>r==RewardType.Revealing?RewardType.Ultra:RewardType.Pair);
        var grants=rules.ApplySingle(profile,0,4,out var plan);
        Require(grants.SequenceEqual(plan.PowerUps) && plan.PowerUps.SequenceEqual(new[]{RewardType.Revealing,RewardType.Ultra}) && plan.FinalProgress==1 && plan.NextReward==RewardType.Pair,"Economics returns the ordered awarded tools and final bar state");
        var repository=new Scripts.Economy.PlayerProfileRepository(new Scripts.Economy.MemoryPlayerProfileStorage(),()=>{var data=new Scripts.Economy.PlayerProfileData();data.Wallet.CrystalProgress=1;return data;});
        var achievements=new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings());
        string id=Guid.NewGuid().ToString("N");var awarded=new Scripts.Economy.GameWinRewardService(repository,rules,achievements).SinglePlayer(id,0,new[]{1,2},0,4);
        var changed=new Scripts.Economy.WalletRewardRules(99,(r,l)=>throw new Exception("Replay must not recalculate reward rules"),(r,l)=>throw new Exception("Replay must not select another reward"));
        var replay=new Scripts.Economy.GameWinRewardService(repository,changed,achievements).SinglePlayer(id,0,new[]{1,2},0,4);
        Require(replay.IsReplay && Newtonsoft.Json.JsonConvert.SerializeObject(replay.Crystals)==Newtonsoft.Json.JsonConvert.SerializeObject(awarded.Crystals),"The recorded presentation plan survives receipt replay and rule changes");
    }
    private sealed class RulesTarget : ITargetNumberCreator
    {
        private List<int> _cards;public RulesTarget(IEnumerable<int> cards){_cards=cards.ToList();}
        public List<int> GetTargetCardsList()=>_cards;public IReadOnlyList<int> GetHistoryTargetCards()=>_cards;public IReadOnlyList<int> GetProtectedTargetCards()=>_cards;
        public bool TryAppendTargetCard(int card){_cards.Add(card);return true;}public bool TryRemoveLastTargetCard(){_cards.RemoveAt(_cards.Count-1);return true;}
        public void ApplyCommittedTarget(IReadOnlyList<int> cards)=>_cards=cards.ToList();public void SetTargetNumber(int count){}public void CreateMultiplayerTargetNumber(){}
    }
    private sealed class RulesTracker : ILevelTracker
    {
        public GameOption GetGameOption()=>GameOption.SinglePlayer;public int GetLevelId()=>20;
        public void GrantIntroducedPowerUps(){}public void EnsurePlaytestPowerUp(RewardType type){}public void ClearPlayerPrefs(){}public void SavePlayerPrefs(){}
        public void SetGameOption(GameOption option){}public void SetMultiplayerLevelDifficulty(Difficulty value){}public int GetNumberOfBoardCardsInMultiplayer()=>5;
        public bool IsFirstLevelTutorial()=>false;public bool IsCardInfoTutorial()=>false;public void IncrementLevelId(int a,int b){}public void IncrementLevelIdWithRewards(int a,int b){}
        public void IncreaseRevealingPowerUpCount(){}public void IncreaseLifePowerUpCount(){}public void IncreaseBombPowerUpCount(){}
        public int GetGiftStarCount()=>0;public int GetCrystalProgressCount()=>0;public int GetStarCount()=>0;public int GetCoinCount()=>0;
        public int GetRevealingPowerUpCount()=>0;public int GetLifePowerUpCount()=>0;public int GetBombPowerUpCount()=>0;public int GetPowerUpCount(RewardType type)=>0;public RewardType GetCurrentRewardType()=>RewardType.Revealing;
    }
}

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewEventFlows(Canvas canvas,Camera camera)
    {
        string prefs=Profile();var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        var unexpected=new List<string>();int injected=0;
        Application.LogCallback log=(message,stack,type)=>{
            if(type!=LogType.Exception && type!=LogType.Error && type!=LogType.Assert)return;
            if(message.Contains("Injected"))injected++;else unexpected.Add(message);
        };
        Application.logMessageReceived+=log;
        try
        {
            ReviewTurnNotifications(canvas,camera);
            ReviewTerminalMechanic(canvas,camera);
            ReviewRewardWriteFailures(canvas,camera);
            ReviewModalCancellation(canvas,camera);
            ReviewNetworkObserverFailures(canvas,camera);
            ReviewProfileAreas(canvas,camera);
            Require(injected>=5,"Throwing observers and transport failures were exercised");
            Require(unexpected.Count==0,"No unexpected event-flow errors: "+string.Join(" | ",unexpected));
            Require(prefs==Profile(),"Event-flow regressions preserve real player preferences");
            System.IO.File.WriteAllText(Folder+"/event-flow-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=true,injected_errors=injected,checks=new[]{"rules finish before UI notifications","single win file-write failure and same-ID retry","pending win checkpoint","duel result published despite failed reward write","durable duel outbox and replay","normal and pair drag cancelled by information modal","late drag rejected","critical powerup recovery precedes broken observer","network command precedes broken observer","typed profile areas ignore telemetry-only updates"}},Newtonsoft.Json.Formatting.Indented));
        }
        finally{Application.logMessageReceived-=log;DOTween.defaultUpdateType=previous;}
    }
    private static void ReviewTurnNotifications(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);var definition=new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9,Mechanic=LevelMechanic.Helper};
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3});
            using(var rules=RulesFor(game,new LevelMechanicsController(game.Results,game.Helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker)))
            {
                rules.Begin(LevelMechanic.Helper);int notifications=0;
                game._area.History.Changed+=()=>{
                    notifications++;int turn=game.Round.GetTriedCardsList().Count;
                    Require(game.Round.GetRemainingGuessCount()==9-turn,"UI sees already-consumed lives");
                    if(turn==3)Require(game._saves.GetLevelSaveData().RevealedGuessNumbers.Contains(3) && game._area.History.GetHistoryEntry(2).hintStates!=null,"UI sees completed helper rules and evidence");
                    throw new Exception("Injected result observer");
                };
                game.Results.WrongGuessEvent+=(s,e)=>throw new Exception("Injected wrong-guess observer");
                for(int turn=0;turn<3;turn++){for(int card=3;card<6;card++)game.Place(card);game.Check();Tick(.5f);}
                Require(notifications==3 && game.Round.GetRemainingGuessCount()==6,"A broken observer cannot prevent a complete turn");
            }
        }
    }
    private sealed class CountPreparedHints : IHintProvider
    {public int Count;public Action<IRewardTokenView> PrepareHint(bool reveal){Count++;return null;}}
    private static void ReviewTerminalMechanic(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);var definition=new LevelData{NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=9,Mechanic=LevelMechanic.Dynamite};
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3,4,5});
            game._saves.GetLevelSaveData().Dynamite.BoardIndex=1;var danger=DangerFor(game,definition);
            using(var rules=DangerRules(game,definition,danger))
            {
                rules.Begin(LevelMechanic.Dynamite);var hints=new CountPreparedHints();game.Results.TurnFlow.Hints=hints;
                for(int turn=1;turn<=4;turn++)
                {
                    if(turn==4)Field(game.Round,"_remainingGuessCount",4);
                    for(int index=0;index<5;index++)Require(game.Placement.TryPlaceCardOnBoard(new[]{5,6,7,8,0}[index],index),"Prepare a dynamite miss");
                    game.Check();Tick(.5f);
                }
                Require(danger.Current.Exploded && game.Round.GetRemainingGuessCount()==0 && hints.Count==0,"Terminal mechanics finish before crystal help and cannot be rescued by that turn's hint");
            }
        }
    }
    private static void ReviewRewardWriteFailures(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);var definition=new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9};
        string path=Folder+"/EventAwards/"+Guid.NewGuid().ToString("N")+".json";System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        bool fail=false;var storage=new Scripts.Economy.AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new System.IO.IOException("Injected reward file write");});
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",1);
            using(var session=CreateContractSession(game,definition,storage))
            {
                var di=new DiContainer();di.Bind<IGameSaveSnapshotProvider>().FromInstance(GrowingSnapshot(game,definition));Field(session,"_snapshots",di.Resolve<LazyInject<IGameSaveSnapshotProvider>>());
                session.Begin(LevelEntryMode.New);Tick(.5f);game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3});
                string id=game._saves.GetLevelSaveData().RoundId;fail=true;
                for(int index=0;index<3;index++)Require(game.Placement.TryPlaceCardOnBoard(index,index),"Prepare the winning guess");
                game.Check();
                var save=(MemorySave)Get(game,"_memory");
                Require(session.IsGameOver && session.RewardState==RewardSaveState.RetryRequired && save.Data.HasPendingWin() && save.Data.RoundId==id,"Failed award write preserves the won state and a durable same-ID result checkpoint");
                Require(canvas.GetComponentInChildren<LevelFinishPopupView>(true).GetComponentsInChildren<TMPro.TMP_Text>().Any(text=>text.text=="Ödül kaydedilemedi."),"The player sees reward save failure");
                session.GoToNextLevel();Require(session.State==LevelFlowState.EndingSuccess,"Progress cannot skip an unsettled reward");
                session.Dispose();
                using(var resumed=CreateContractSession(game,definition,storage))
                {
                    Field(resumed,"_snapshots",di.Resolve<LazyInject<IGameSaveSnapshotProvider>>());
                    resumed.Begin(LevelEntryMode.Resume);
                    Require(resumed.IsGameOver && resumed.RewardState==RewardSaveState.RetryRequired && game._saves.GetLevelSaveData().RoundId==id,"Reopening a won round restores its unsettled same-ID reward");
                    fail=false;resumed.RetryReward();resumed.RetryReward();
                    var profile=storage.Load();
                    Require(resumed.RewardState==RewardSaveState.Completed && profile.Progress.LevelId==2 && profile.Transactions.Keys.Count(key=>key=="single:"+id)==1
                        && profile.Collection[Scripts.Economy.FruitId.Apple].LifetimeEarned==1 && save.Data==null,"Retry records the exact won round once, then clears its checkpoint");
                }
            }
        }
        double clock=0;int attempts=0;
        var retry=new RewardSettlement<int>(()=>{if(++attempts==1)throw new System.IO.IOException("Injected automatic retry");return 7;},()=>clock);
        retry.Retry();clock=2;retry.Tick();retry.Tick();Require(retry.State==RewardSaveState.Completed && attempts==2 && retry.Value==7,"Automatic retries run without another win event");
        var iceLevel=new TestLevel(new LevelData{NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=9,Mechanic=LevelMechanic.Ice});
        var iceSave=new LevelSaveDataManager(iceLevel);iceSave.CreateDefaultLevelSaveData();var pendingIce=iceSave.GetLevelSaveData();
        pendingIce.PendingWin=true;pendingIce.TargetCards=new List<int>{1,2,3,4,5};pendingIce.TriedCardsList=new List<List<int>>{new List<int>{1,2,3,4,5}};
        pendingIce.Ice=new IceRoundState{TurnNumber=1};var iceRound=new RoundStateManager(iceLevel,iceSave);iceRound.Initialize();
        var ice=new IceBoardService(iceLevel,new RulesTracker(),iceSave,iceRound);ice.PrepareTurn();
        Require(!ice.IsFrozen && pendingIce.Ice.TurnNumber==1,"Resuming a won ice round cannot freeze a new turn before presenting its reward");
    }
    private static void ReviewModalCancellation(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);using(var game=new Fixture(canvas,camera,new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9}))
        {
            game.BuildActors();game.Gate.Clear();
            using(var notes=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,null,game.Initial,game.Board))
            {
                var events=((GameObject)Get(game,"_events")).GetComponent<UnityEngine.EventSystems.EventSystem>();
                game.Initial.TryGetCardItem(0,out var card);var pointer=new UnityEngine.EventSystems.PointerEventData(events){pointerId=11,position=new Vector2(100,200)};
                var move=card.GetCardMoveHandler();move.OnPointerDown(pointer);
                var drag=typeof(CardViewHandler).GetMethod("OnDrag",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                drag.Invoke(card.GetCardViewHandler(),new object[]{pointer});Require(move.IsDragStarted(),"An actual card drag begins");
                game.View.GetInformationButton().onClick.Invoke();
                Tick(.4f);
                var parent=card.GetRectTransform().parent;var position=card.GetRectTransform().anchoredPosition;
                pointer.position+=new Vector2(70,30);drag.Invoke(card.GetCardViewHandler(),new object[]{pointer});move.OnPointerUp(pointer);
                Require(!move.IsDragStarted() && parent!=game.Initial.GetDragParent() && card.GetRectTransform().parent==parent && card.GetRectTransform().anchoredPosition==position,"Modal cancellation restores placement and rejects late movement/release");
                var query=(IBoardPlacementQuery)Get(game.Placement,"_boardPlacementQuery");
                using(var pair=new PairDragPresenter(game.Initial,game.Placement,game.Board,query,game.Gate,game.UI,()=>true))
                {
                    Require(pair.TryBegin(new PairClue(1,2),game.Initial.GetDragParent()),"A real grouped drag begins");
                    game.View.GetInformationButton().onClick.Invoke();pair.End(0,false);
                    Require(!pair.IsDragging && query.GetPlacedCardIndexesOnBoard().All(index=>index<0),"Information cancels a grouped drag before it can place into the board");
                }
            }
        }
    }
    private sealed class EventInboxStorage : Scripts.Economy.IPendingRewardStorage
    {public string Data;public string Read()=>Data;public void Write(string data)=>Data=data;}
    private static void ReviewNetworkObserverFailures(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);using(var game=new Fixture(canvas,camera,new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9}))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();Field(game.Round,"_remainingGuessCount",7);
            var profiles=new Scripts.Economy.PlayerProfileRepository(new Scripts.Economy.MemoryPlayerProfileStorage(),()=>{var p=new Scripts.Economy.PlayerProfileData();p.Wallet.PowerUps[RewardType.Life]=1;return p;});
            var journal=new Scripts.Economy.PowerUpUseJournal(profiles);var save=new PowerUseControlledSave();
            var uses=new PowerUpUseService(game.Tracker,new TestLevel(new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9}),game._saves,game.Round,game._info,game._counts,game.Target,journal,save,
                ()=>GrowingSnapshot(game,new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9}),new PowerUpCatalog(new[]{new LifePowerUpEffect(()=>throw new System.IO.IOException("Injected live effect"))}),()=>null);
            int recovery=0;uses.RecoveryRequired+=()=>recovery++;uses.Changed+=()=>throw new Exception("Injected power UI observer");
            Require(uses.TryApply(RewardType.Life)==PowerUpUseResult.ResumeRequired && recovery==1 && profiles.Snapshot().Wallet.PowerUps[RewardType.Life]==0,"A bad UI observer cannot suppress committed powerup recovery or its return value");
        }
        var networkRoot=new GameObject("EventNetworkManager");var root=new GameObject("EventNetworkController");root.SetActive(false);
        try
        {
            networkRoot.AddComponent<Unity.Netcode.NetworkManager>();
            var controller=root.AddComponent<MultiplayerGameController>();int sent=0,states=0;
            controller.StateChanged+=(s,e)=>throw new Exception("Injected network UI observer");controller.StateChanged+=(s,e)=>states++;
            var send=typeof(MultiplayerGameController).GetMethod("SendGuess",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            send.Invoke(controller,new object[]{(Action)(()=>sent++)});
            Require(sent==1 && states==1 && (bool)Get(controller,"_pending"),"UI failure cannot block a submitted command");
            send.Invoke(controller,new object[]{(Action)(()=>throw new System.IO.IOException("Injected transport failure"))});
            Require(!(bool)Get(controller,"_pending"),"A failed send releases pending input");
            var storage=new PowerUseFailingStorage();storage.Fail=false;
            var profiles=new Scripts.Economy.PlayerProfileRepository(storage,()=>new Scripts.Economy.PlayerProfileData());profiles.Snapshot();storage.Fail=true;
            var rewards=new Scripts.Economy.GameWinRewardService(profiles,Scripts.Economy.PlayerProfileRuntime.CreateRewardRules(),new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings()));
            var inbox=new EventInboxStorage();var jobs=new Scripts.Economy.DuelRewardRecovery(rewards,inbox);
            Field(controller,"_rewardRecovery",jobs);Field(controller,"_initialized",true);string id=Guid.NewGuid().ToString("N");Field(controller,"_matchId",id);
            ulong winner=controller.LocalPlayerId;Field(controller,"<State>k__BackingField",new MultiplayerPublicState(1,1,MultiplayerPhase.Finished,MultiplayerRules.NoPlayer,winner,0,0));
            int ended=0;controller.MatchEnded+=(s,e)=>throw new Exception("Injected result UI observer");controller.MatchEnded+=(s,e)=>ended++;
            var process=typeof(MultiplayerGameController).GetMethod("ProcessOutcome",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            process.Invoke(controller,new object[]{id,1,winner,new[]{1,2,3},(byte)RewardType.Bomb});
            Require(ended==1 && controller.RewardState==RewardSaveState.RetryRequired && !string.IsNullOrEmpty(inbox.Data),"The finished match is delivered even when wallet persistence fails, with a durable retry intent");
            using(var game=new Fixture(canvas,camera,new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9}))
            {
                var view=canvas.GetComponentInChildren<LevelFinishPopupView>(true);var popup=new LevelEndPopupController(view);
                var presenter=new MultiplayerRoundPresenter(new LifeBarController(canvas.GetComponentInChildren<LifeBarView>(true)),game.Initial,game.UI,popup,controller,new ContractMultiplayerHud(),popup);
                var outcome=new MultiplayerOutcome(1,winner,new[]{1,2,3},false,RewardType.Bomb);presenter.ShowOutcome(outcome,true);
                Require(view.GetComponentsInChildren<TMPro.TMP_Text>().Any(text=>text.text=="Ödül kaydedilemedi."),"The real duel result popup exposes reward persistence failure");
                storage.Fail=false;controller.RetryReward();presenter.RefreshRewardState(outcome,true);
                Require(!view.GetComponentsInChildren<TMPro.TMP_Text>().Any(text=>text.text=="Ödül kaydedilemedi."),"A successful retry clears the duel result error");
            }
            process.Invoke(controller,new object[]{id,1,winner,new[]{1,2,3},(byte)RewardType.Bomb});
            Require(controller.RewardState==RewardSaveState.Completed && ended==1 && profiles.Snapshot().Wallet.PowerUps[RewardType.Bomb]==1,"The same network outcome retries once and does not repeat the match event");
            var second=jobs.Register(new Scripts.Economy.PendingDuelAward{MatchId=id,Round=2,Answer=new[]{1,2,3},Reward=RewardType.Life});storage.Fail=true;second.Retry();
            storage.Fail=false;var reopened=new Scripts.Economy.DuelRewardRecovery(rewards,inbox);reopened.Initialize();reopened.Tick();reopened.Tick();
            Require(profiles.Snapshot().Wallet.PowerUps[RewardType.Life]==1,"A pending duel award survives reopening its inbox and is applied once");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(networkRoot);}
    }
    private sealed class CountingCollection : Menu.IFruitCollectionView
    {
        private readonly Menu.IFruitCollectionView _view;public int Renders,Badges;
        public CountingCollection(Menu.IFruitCollectionView view){_view=view;}
        public IBaseButtonView CloseButton=>_view.CloseButton;public bool IsVisible=>_view.IsVisible;public Menu.IGardenPanelView Garden=>_view.Garden;public Menu.ISeedPurchasePopupView Purchase=>_view.Purchase;
        public event Action<Menu.GardenTab> TabChanged {add=>_view.TabChanged+=value;remove=>_view.TabChanged-=value;}
        public void SetVisible(bool value)=>_view.SetVisible(value);public void SelectTab(Menu.GardenTab tab)=>_view.SelectTab(tab);public void FocusBadge(string id)=>_view.FocusBadge(id);
        public void Render(IReadOnlyDictionary<Scripts.Economy.FruitId,Scripts.Economy.FruitBalance> data){Renders++;_view.Render(data);}
        public void RenderAchievements(IReadOnlyList<Scripts.Economy.AchievementStatus> data){Badges++;_view.RenderAchievements(data);}
        public void SetBadgeCount(long count)=>_view.SetBadgeCount(count);
    }
    private static void ReviewProfileAreas(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);var root=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/FruitCollection.prefab"),canvas.transform);
        var noticeRoot=new GameObject("EventGardenNotice",typeof(RectTransform),typeof(Menu.GardenNoticeView));noticeRoot.transform.SetParent(canvas.transform,false);
        try
        {
            var profiles=new Scripts.Economy.PlayerProfileRepository(new Scripts.Economy.MemoryPlayerProfileStorage(),()=>new Scripts.Economy.PlayerProfileData());
            var settings=new Scripts.Economy.EconomySettings();var rules=new Scripts.Economy.AchievementRules(settings);var achievements=new Scripts.Economy.AchievementService(profiles,rules);
            var inventory=new Scripts.Economy.FruitInventoryService(profiles,rules);var garden=new Scripts.Economy.GardenService(profiles,settings,new Scripts.Economy.SystemGardenClock(),rules);
            var view=new CountingCollection(root.GetComponent<Menu.FruitCollectionView>());var di=new DiContainer();di.Bind<IHapticController>().To<HapticController>().AsSingle();di.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
            using(var presenter=new Menu.FruitCollectionPresenter(inventory,profiles,view,di.Resolve<BaseButtonControllerFactory>(),garden,new Scripts.Economy.GardenVisitCoordinator(garden),noticeRoot.GetComponent<Menu.GardenNoticeView>(),achievements))
            {
                presenter.Open(Menu.GardenTab.Collection);int renders=view.Renders,badges=view.Badges;Scripts.Economy.ProfileArea areas=Scripts.Economy.ProfileArea.None;
                profiles.Updated+=change=>areas=change.Areas;
                profiles.Update(profile=>profile.TelemetryEngagement.ActiveDay++);
                Require(areas==Scripts.Economy.ProfileArea.Telemetry && view.Renders==renders && view.Badges==badges,"Telemetry-only writes do not redraw fruit or badge collections");
                inventory.Grant("event-fruit",Scripts.Economy.FruitGainSource.SinglePlayer,new Dictionary<Scripts.Economy.FruitId,long>{{Scripts.Economy.FruitId.Apple,1}});
                Require(view.Renders==renders+1 && view.Badges==badges+1,"Actual collection changes redraw only their relevant data");
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(noticeRoot);}
    }
}

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewFactories(Canvas canvas,Camera camera)
    {
        string prefs=Profile();var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        var errors=new List<string>();int isolated=0;
        Application.LogCallback log=(message,stack,type)=>{if(type!=LogType.Error && type!=LogType.Exception && type!=LogType.Assert)return;if(message.Contains("Injected settings"))isolated++;else errors.Add(message);};
        Application.logMessageReceived+=log;
        try
        {
            ReviewSettingsCreation(canvas,camera);
            ReviewDirectModalCommand(canvas,camera);
            Require(errors.Count==0 && isolated==1,"No unexpected factory errors: "+string.Join(" | ",errors));
            Require(prefs==Profile(),"Factory checks preserve real player preferences");
            System.IO.File.WriteAllText(Folder+"/factory-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks=new[]{
                "one settings creation path for menu/online/single/multiplayer","local UI transforms with a scaled parent","duplicate-open guard","release-only close and input restoration","save/reset/navigation/collection callbacks remain distinct","privacy listener detached on close","external destruction cleans overlay","failure before creation leaves no overlay","direct game modal cancels normal/pair/note/power interactions without UI event subscriptions","bad power-close observer isolated","legacy world-transform default preserved"}},Newtonsoft.Json.Formatting.Indented));
        }
        finally{Application.logMessageReceived-=log;DOTween.defaultUpdateType=previous;}
    }
    private static void ReviewSettingsCreation(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);using(var game=new Fixture(canvas,camera,new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9}))
        {
            game.BuildActors();game.Gate.Clear();
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<SettingsPopupView>("Assets/Prefabs/SettingsPopup.prefab");
            var parent=new GameObject("SettingsScaledParent",typeof(RectTransform));parent.transform.SetParent(canvas.transform,false);
            FruitLabStyle.Stretch((RectTransform)parent.transform);parent.transform.localScale=Vector3.one*.8f;
            var scenes=new List<string>();int cancelled=0,closed=0,saved=0,reset=0,collection=0,privacy=0,shutdown=0;
            var buttons=(BaseButtonControllerFactory)Get(game,"_buttons");
            var factory=new SettingsPopupService(game.Tracker,buttons,scene=>scenes.Add(scene),()=>shutdown++,()=>privacy++);
            SettingsPopupRequest Request(SettingsPopupContext context)=>new SettingsPopupRequest{Prefab=prefab,Parent=parent.transform,OverlayName="SettingsTestOverlay",Context=context,
                CancelInteractions=()=>{Require(parent.transform.Find("SettingsTestOverlay")==null,"Cancel precedes overlay creation");cancelled++;},Closed=()=>closed++,Save=()=>saved++,Reset=()=>reset++,Collection=()=>collection++};
            try
            {
                foreach(var context in new[]{SettingsPopupContext.Menu,SettingsPopupContext.Online,SettingsPopupContext.Game})
                {
                    int cancellations=cancelled,closes=closed;
                    var request=Request(context);var handle=factory.Show(request);
                    Require(handle!=null && handle.View.transform.localScale==Vector3.one && handle.View.transform.parent.localScale==Vector3.one,"The creator applies local UI transforms");
                    Require(factory.Show(request)==null && cancelled==cancellations+1,"Repeated opens do not create or cancel twice");
                    var view=handle.View;
                    Require(((Component)view.GetRetryButton()).gameObject.activeSelf==(context==SettingsPopupContext.Game),"Retry is available only in a single-player game, even when online opens from a single-player profile");
                    var privacyButton=view.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button=>button.name=="PrivacyButton");
                    privacyButton.onClick.Invoke();Require(privacy==cancellations+1,"Privacy uses the shared controller dependency");
                    if(context==SettingsPopupContext.Menu)
                        view.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(button=>button.name=="AchievementsButton").onClick.Invoke();
                    else
                    {
                        var button=(BaseButtonView)view.GetCloseButton();button.OnPointerDown(null);Require(!handle.IsClosed,"Close waits for pointer release");button.OnPointerUp(null);button.OnPointerClick(null);
                    }
                    handle.Close();
                    Require(handle.IsClosed && closed==closes+1 && parent.transform.Find("SettingsTestOverlay")==null,"Close cleans the owned overlay once");
                }
                Require(saved==0 && reset==0 && scenes.Count==0 && collection==1,"Closing and opening collection do not save, reset or navigate");
                foreach(bool menu in new[]{true,false})
                {
                    using(var handle=factory.Show(Request(SettingsPopupContext.Game)))Click((BaseButtonView)(menu?handle.View.GetMenuButton():handle.View.GetRetryButton()));
                }
                Require(scenes.SequenceEqual(new[]{"Menu","Game"}) && saved==1 && reset==1 && shutdown==1,"Settings use the explicit save/reset callbacks and correct destination");
                Field(game.Tracker,"_gameOption",GameOption.MultiPlayer);
                using(var handle=factory.Show(Request(SettingsPopupContext.Game)))
                {Require(!((Component)handle.View.GetRetryButton()).gameObject.activeSelf,"Multiplayer game settings never expose restart");Click((BaseButtonView)handle.View.GetMenuButton());}
                Require(saved==1 && shutdown==2 && scenes.Last()=="Menu","Multiplayer menu does not write a single-player checkpoint");
                var external=factory.Show(Request(SettingsPopupContext.Online));int before=closed;
                // Preview-scene objects do not receive normal Play-mode lifecycle messages.
                typeof(SettingsPopupView).GetMethod("OnDestroy",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(external.View,null);
                Require(external.IsClosed && closed==before+1 && parent.transform.Find("SettingsTestOverlay")==null
                    && Get(external.View,"PrivacyRequested")==null,"External view destruction also disposes the controller and overlay");
                var broken=Request(SettingsPopupContext.Online);broken.CancelInteractions=()=>throw new InvalidOperationException("Cancel must finish before opening");
                try{factory.Show(broken);throw new Exception("Expected cancellation failure");}catch(InvalidOperationException){}
                Require(parent.transform.Find("SettingsTestOverlay")==null,"A failed mandatory action leaves no half-open overlay");
                var original=new GameObject("TransformProbe",typeof(RectTransform));
                try
                {
                    original.transform.localScale=Vector3.one*.9f;
                    var objects=new Factory.BaseObjectFactory<GameObject,GameObject>();
                    var world=objects.Spawn(parent.transform,original);var local=objects.Spawn(parent.transform,original,false);
                    try{Require((world.transform.lossyScale-original.transform.lossyScale).sqrMagnitude<.0001f && local.transform.localScale==original.transform.localScale,"Factory transform policy preserves legacy world behavior and supports explicit local behavior");}
                    finally{UnityEngine.Object.DestroyImmediate(world);UnityEngine.Object.DestroyImmediate(local);}
                }
                finally{UnityEngine.Object.DestroyImmediate(original);}
            }
            finally{UnityEngine.Object.DestroyImmediate(parent);}
        }
    }
    private static void ReviewDirectModalCommand(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);using(var game=new Fixture(canvas,camera,new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9}))
        using(var assistance=AssistanceFor(game,new LevelData{NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9},out _,false))
        using(var annotations=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,assistance,game.Initial,game.Board))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();
            var context=canvas.gameObject.AddComponent<GamePlayContext>();var popup=Find<GamePopupCreator>(canvas.gameObject.scene);
            var mutations=new List<(object owner,string field,object previous)>();
            void Set(object owner,string field,object value){mutations.Add((owner,field,Get(owner,field)));Field(owner,field,value);}
            try
            {
                Set(context,"_initialCardAreaController",game.Initial);Set(context,"_powerMessages",game.PowerMessages);Set(context,"_positionAssistance",assistance);Set(context,"<Annotations>k__BackingField",annotations);
                var di=new DiContainer();di.Bind<IGameInteractionCancellation>().FromInstance(context);
                var factory=new SettingsPopupService(game.Tracker,(BaseButtonControllerFactory)Get(game,"_buttons"),_=>{},()=>{},()=>{});
                Set(popup,"_settings",factory);Set(popup,"_interactions",di.Resolve<LazyInject<IGameInteractionCancellation>>());Set(popup,"_saveGameAction",(Action)(()=>{}));Set(popup,"_deleteSaveAction",(Action)(()=>{}));Set(popup,"_notes",assistance);
                var query=(IBoardPlacementQuery)Get(game.Placement,"_boardPlacementQuery");
                var pair=new PairDragPresenter(game.Initial,game.Placement,game.Board,query,game.Gate,game.UI,()=>true);game.View.GetPairView().Configure(pair);
                game.Initial.TryGetCardItem(0,out var card);var pointer=new UnityEngine.EventSystems.PointerEventData(((GameObject)Get(game,"_events")).GetComponent<UnityEngine.EventSystems.EventSystem>()){pointerId=9,position=new Vector2(70,130)};
                card.GetCardMoveHandler().OnPointerDown(pointer);var drag=typeof(CardViewHandler).GetMethod("OnDrag",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);drag.Invoke(card.GetCardViewHandler(),new object[]{pointer});
                Require(pair.TryBegin(new PairClue(2,3),game.Initial.GetDragParent()),"Start a group drag beside a normal drag");
                Require(annotations.TrySelect(3),"Open a manual note selection");
                Field(game.PowerMessages,"_activePowerUpType",GameUIButtonType.RevealingPowerUp);
                game.PowerMessages.ClosePowerUpEvent+=(s,e)=>throw new Exception("Injected settings power-close observer");
                typeof(GamePopupCreator).GetMethod("CreateSettingsPopup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(popup,new object[]{null,EventArgs.Empty});
                Tick(.4f);
                var overlay=canvas.transform.Find("GameSettingsOverlay");
                Require(overlay!=null && !card.GetCardMoveHandler().IsDragStarted() && !pair.IsDragging && !annotations.IsSelecting
                    && (GameUIButtonType)Get(game.PowerMessages,"_activePowerUpType")==GameUIButtonType.Default,"Direct modal creation clears all interactions before opening, without publishing OpenSettings");
                Layer(canvas.transform);Draw(camera);
                var eventData=new UnityEngine.EventSystems.PointerEventData(((GameObject)Get(game,"_events")).GetComponent<UnityEngine.EventSystems.EventSystem>()) {
                    position=RectTransformUtility.WorldToScreenPoint(camera,game.UI.GetCheckButtonRectTransform().position)};
                var hits=new List<UnityEngine.EventSystems.RaycastResult>();canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().Raycast(eventData,hits);
                Require(hits.Count>0 && hits[0].gameObject.transform.IsChildOf(overlay),"The settings overlay blocks underlying gameplay input");
                var view=overlay.GetComponentInChildren<SettingsPopupView>();Click((BaseButtonView)view.GetCloseButton());
                Require(canvas.transform.Find("GameSettingsOverlay")==null,"Closing game settings removes the modal input blocker");
                int turn=game.Round.GetTriedCardsList().Count;pointer.position+=Vector2.one*50;drag.Invoke(card.GetCardViewHandler(),new object[]{pointer});card.GetCardMoveHandler().OnPointerUp(pointer);pair.End(0,false);
                Require(query.GetPlacedCardIndexesOnBoard().All(index=>index<0) && game.Round.GetTriedCardsList().Count==turn,"Late pointer/group release cannot place a card or submit a guess");
            }
            finally
            {
                game.View.GetPairView().Unbind();
                var overlay=canvas.transform.Find("GameSettingsOverlay");if(overlay!=null)UnityEngine.Object.DestroyImmediate(overlay.gameObject);
                foreach(var mutation in mutations.AsEnumerable().Reverse())Field(mutation.owner,mutation.field,mutation.previous);
                UnityEngine.Object.DestroyImmediate(context);
            }
        }
    }
}

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
    public static void ReviewManualNotes(Canvas canvas,Camera camera)
    {
        var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        var errors=new List<string>();Application.LogCallback collect=(message,stack,type)=>{
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert || message.Contains("error inside a tween callback"))errors.Add(message);
        };
        Application.logMessageReceived+=collect;
        try
        {
            var state=new PositionDeductionService(4,2);
            state.SetManualPresence(1,FruitPresence.Present);state.TogglePosition(1,1);
            Require(state.FixedSlot(1)==-1,"Editing alone does not replay inference");
            state.ApplyGuess(1,new[]{2,3},0,0);
            Require(state.FixedSlot(1)==0 && state.FixedSlot(4)==1,"Future inference uses the player's current notes");
            state.TogglePosition(1,1);
            Require(state.IsPossible(1,1) && state.FixedSlot(4)==1 && !state.IsPossible(4,0),"Undoing a note preserves its already-derived consequences");
            var restored=PositionDeductionService.Restore(Newtonsoft.Json.JsonConvert.DeserializeObject<PositionNotesSnapshot>(Newtonsoft.Json.JsonConvert.SerializeObject(state.Capture(1,true))));
            Require(restored.IsPossible(1,1) && restored.FixedSlot(4)==1,"Unified notes survive JSON round trips without replaying guesses");
            for(int fruit=1;fruit<=4;fruit++)state.SetManualPresence(fruit,FruitPresence.Absent);
            Require(state.ApplyGuess(2,new[]{1,2},0,1) && !state.IsConsistent,"Incorrect notes remain editable despite a contradictory inference");
            state.TogglePosition(1,0);Require(state.Presence(1)!=FruitPresence.Absent,"Even a contradictory notebook allows a fruit to be restored");

            var definition=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9,Mechanic=LevelMechanic.Normal};
            LevelSaveData saved=null;
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",7);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{2,3,1});
                var preferences=new PositionAssistancePreferences(true);
                using(var assistance=AssistanceFor(game,definition,out var noteView,preferences))
                using(var editor=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,assistance,game.Initial,game.Board))
                {
                    assistance.Refresh();game.Initial.TryGetCardItem(0,out var card);
                    card.GetCardMoveHandler().OnPointerDown(null);Tick(.4f);Require(!editor.IsSelecting,"Short holds remain ordinary card input");
                    Tick(.12f);Require(editor.IsSelecting,"Half-second holds open editing");card.GetCardMoveHandler().OnPointerUp(null);
                    Require(game.Placement.GetCardsOnBoard().Count==0,"Releasing a hold cannot place the selected fruit");
                    var earlyProvider=new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
                    Field(earlyProvider,"_saveData",game._saves);
                    for(int slot=0;slot<3;slot++)
                    {
                        ClickPositionIndicator(game,editor.GetPositionButton(slot));
                        Require(editor.IsSelecting && game.Initial.GetHolderView(0).Indicators[slot].IsExcluded,"Position edits remain selected and immediately update the initial holder");
                    }
                    Require(assistance.State.Presence(1)==FruitPresence.Absent && ((NormalCardItemView)card.GetView()).DeducedPresence==FruitPresence.Absent,"All crossed positions exclude the fruit without deleting it");
                    Require(earlyProvider.TryCreateSnapshot(out var earlySave) && earlySave.TriedCardsList.Count==0 && earlySave.PositionNotes.Edited,"Notes save even before the first guess");
                    ClickPositionIndicator(game,editor.GetPositionButton(1));
                    Require(!earlySave.PositionNotes.Possible[1],"Saved notes are a copy and cannot change when the live notebook changes");
                    Require(assistance.State.Presence(1)==FruitPresence.Unknown && ((NormalCardItemView)card.GetView()).GetImage().color.a>.9f,"Reopening a position restores the fruit");
                    foreach(int index in new[]{0,1,2})
                    {
                        editor.GetButton(index).onClick.Invoke();Require(editor.IsSelecting,"Colour buttons keep the editor open");
                        Require(assistance.State.Presence(1)==(index==0?FruitPresence.Absent:index==1?FruitPresence.Unknown:FruitPresence.Present),"Colour buttons edit the same notebook as positional controls");
                    }
                    editor.GetButton(1).onClick.Invoke();
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-manual-notes.png");
                    var overlay=editor.Toolbar.parent.gameObject;
                    UnityEngine.EventSystems.ExecuteEvents.Execute(overlay,new UnityEngine.EventSystems.PointerEventData(((GameObject)Get(game,"_events")).GetComponent<UnityEngine.EventSystems.EventSystem>()),UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                    Require(!editor.IsSelecting,"An outside press dismisses both editor rows");
                    Require(game.Placement.TryPlaceCardOnBoard(0,0),"A manually marked card remains playable");Tick(.4f);
                    Require(editor.TrySelect(0),"A normal card on the board is editable");editor.Cancel();game.UI.TriggerResetNumbers();Tick(.4f);

                    var settings=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<SettingsPopupView>("Assets/Prefabs/SettingsPopup.prefab"),canvas.transform,false);
                    try
                    {
                        var controller=new SettingsPopupController(_=>{},()=>{});
                        controller.Initialize(settings,()=>{},()=>{},()=>{},game.Tracker,(BaseButtonControllerFactory)Get(game,"_buttons"),assistance);
                        var toggle=settings.transform.Find("Content/NormalAssistance")?.GetComponent<UnityEngine.UI.Button>()
                            ??settings.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(button=>button.name=="NormalAssistance");
                        var reset=settings.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(button=>button.name=="ResetNotes");
                        Layer(canvas.transform);Draw(camera);
                        game.AssertHit((RectTransform)toggle.transform,typeof(UnityEngine.EventSystems.IPointerClickHandler));
                        game.AssertHit((RectTransform)reset.transform,typeof(UnityEngine.EventSystems.IPointerClickHandler));
                        toggle.onClick.Invoke();Require(!assistance.AssistanceEnabled,"Settings turns automatic assistance off");
                        assistance.TogglePosition(1,0);var frozen=assistance.State.Capture(0,false);
                        settings.gameObject.SetActive(false);
                        SubmitAssistedGuess(game,new[]{1,2,3},assistance);
                        Require(assistance.State.Capture(1,false).Possible.SequenceEqual(frozen.Possible) && assistance.State.Presence(1)==FruitPresence.Unknown,"Disabled assistance freezes notes while history continues");
                        Require(editor.TrySelect(1),"Manual editing remains available with assistance off");editor.GetButton(0).onClick.Invoke();editor.Cancel();
                        settings.gameObject.SetActive(true);reset.onClick.Invoke();
                        Require(assistance.State.Capture(1,false).Possible.All(value=>value) && Enumerable.Range(1,7).All(fruit=>assistance.State.Presence(fruit)==FruitPresence.Unknown),"Reset with assistance off clears every note");
                        assistance.TogglePosition(4,2);
                        var provider=new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
                        Field(provider,"_saveData",game._saves);Field(provider,"_levels",new TestLevel(definition));Field(provider,"_tracker",game.Tracker);
                        Require(provider.TryCreateSnapshot(out saved) && saved.PositionNotes.Edited && !saved.PositionNotes.AutomaticEnabled,"Checkpoints contain private manual notes and their inference cursor");
                        saved=Newtonsoft.Json.JsonConvert.DeserializeObject<LevelSaveData>(Newtonsoft.Json.JsonConvert.SerializeObject(saved));
                        toggle.onClick.Invoke();
                        Require(assistance.AssistanceEnabled && assistance.State.Presence(4)==FruitPresence.Absent && !assistance.State.HasManualChanges,"Re-enabling assistance rebuilds from public history and clears manual interventions");
                        assistance.SetPresence(1,FruitPresence.Absent);reset.onClick.Invoke();
                        Require(assistance.State.Presence(1)==FruitPresence.Present && !assistance.State.HasManualChanges,"Reset with assistance on keeps only legitimate recomputed evidence");
                        Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-note-settings.png");
                        settings.gameObject.SetActive(false);
                        game.Placement.TryRevealAndLockCard(0,1,false);Tick(.4f);
                        Require(!editor.TrySelect(1),"Real hint locks cannot be edited");
                        assistance.SetPresence(2,FruitPresence.Absent);Require(assistance.State.Presence(2)==FruitPresence.Present,"Manual commands cannot change a genuinely fixed fruit");
                    }
                    finally {UnityEngine.Object.DestroyImmediate(settings.gameObject);}
                    Require(game._info.GetCardItemInfoList().All(info=>info.isExisted),"Manual exclusion never removes real cards or spends powerups");
                }
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",7);((MemorySave)Get(game,"_memory")).Save(saved);game._saves.SetLevelSaveDataAsSaved(saved);game.Round.Initialize();game.BuildActors();game.Results.TryAddTriedCards();game.Gate.Clear();
                using(var assistance=AssistanceFor(game,definition,out var view,new PositionAssistancePreferences(false)))
                {
                    assistance.Refresh();Require(!assistance.State.IsPossible(4,2) && assistance.State.IsPossible(4,0) && assistance.State.Presence(1)==FruitPresence.Unknown,"Resume restores the frozen notebook instead of replaying old inference");
                    assistance.SetAssistanceEnabled(true);Require(assistance.State.Presence(4)==FruitPresence.Absent,"A resumed game can rebuild public inference on demand");
                    assistance.SetPresence(1,FruitPresence.Absent);game.Initial.ClearInitialCardHolders();
                    game._saves.CreateDefaultLevelSaveData();game.Round.Initialize();game._area.RemoveResultBlocks();game.BuildActors();assistance.Tick();
                    Require(!assistance.State.HasManualChanges && assistance.State.Capture(0,true).Possible.All(value=>value),"A fresh round cannot inherit the previous notebook");
                }
            }
            ValidateNoteLayouts(canvas,camera);
            ValidateMultiplayerNotes(canvas,camera);
            Require(errors.Count==0,"No Unity errors during notebook review: "+string.Join(" | ",errors));
            System.IO.File.WriteAllText(Folder+"/manual-notes-review.json","{\"passed\":true,\"manual_propagation\":true,\"persistent_selection\":true,\"settings_reset\":true,\"save_resume\":true,\"layouts\":80}");
        }
        finally{DOTween.defaultUpdateType=previous;Application.logMessageReceived-=collect;}
    }

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
                    var completion=new SinglePlayerRoundCompletion(game.Tracker,game.Round,saves,game._saves,game.Target,CollectionTestRewards(game));
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
            Debug.Log("FRUIT_LAB_SESSION_CONTRACTS_PASS: "+outcomes+" outcomes; zero/full rewards, overflow, duplicate completion, real buttons, cancellation, alternate HUD/result/indicator implementations.");
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
                var root=new GameObject("ContractBindingTracker");root.SetActive(false);SceneManager.MoveGameObjectToScene(root,scene);
                ILevelSession session=null;PositionAssistanceController assistance=null;
                try
                {
                    var tracker=root.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;Field(tracker,"_gameOption",mode);
                    var container=new DiContainer();
                    container.Bind<Context>().FromInstance(Find<SceneContext>(scene));
                    container.Bind<ILevelTracker>().FromInstance(tracker);container.Bind<IGameSaveService>().FromInstance(new MemorySave());
                    BindCollectionTestRewards(container);
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
                }
                finally {assistance?.Dispose();session?.Dispose();UnityEngine.Object.DestroyImmediate(root);}
            }
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);}
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
                game._guess.FailRound();for(int i=0;i<35;i++)Tick(.1f);
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
            game.Initial.ClearInitialCardHolders();game.Board.ClearBoardHolders();game._area.RemoveResultBlocks();
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

    private static SinglePlayerSession CreateContractSession(Fixture game,LevelData definition)
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
            (ICardItemLocator)Get(game.Placement,"_cardItemLocator"),game._guess,game._info,cardInfo,game.Placement,(ICardInteractionManager)Get(game,"_interaction"),game.Results,game.Round);
        var saves=(MemorySave)Get(game,"_memory");
        return new SinglePlayerSession(game.Tracker,new TestLevel(definition),saves,game._saves,game._counts,popups,game._guess,game.Results,game.Round,
            new SinglePlayerRoundCompletion(game.Tracker,game.Round,saves,game._saves,game.Target,CollectionTestRewards(game)),new SinglePlayerEndPresenter(popups,game.UI,game._area,game.Target,cardInfo,bar,popup,success,failure),
            null,builder,new LevelPresentation(popups,game._powers,bar,new ContractStartAnimation(),popup,failure,fade));
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
        public bool Success;public int Coins,Crystals;public Sequence Sequence;
        public Sequence SuccessLevelAnimation(int earnedCoinCount,int earnedCrystalCount,int previousCoinCount,int previousCrystalProgressCount,RewardType rewardTypeBeforeCollection)
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
        public void CreateAbleToMovePopup(){}public void CreateWaitingOpponentPopup(Action action){}
        public void CreateMultiplayerLevelEnd(bool success,IUserReady ready,Action action){}
        public void CreateNewGameOfferPopup(){}public void CreateDisconnectionPopup(){}
        public void CloseNotAbleToMovePopup(){}public void CloseAbleToMovePopup(){}public void CloseNewGameOfferPopup(){}
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
        public MultiplayerPublicState PublicState;
        public ulong LocalPlayerId=>1;public double ServerTime=>0;public MultiplayerPublicState State=>PublicState;
        public bool IsLocalTurn=>State.Phase==MultiplayerPhase.Playing && State.CurrentPlayer==LocalPlayerId;
        public bool LocalRematchRequested=>false;public string StatusNotice=>"";public string DisconnectMessage=>"Kurucu ayrıldı";
        public string GetPlayerName(ulong player)=>"Oyuncu "+player;
        public void Initialize(){}public void BoardReady(int round){}public void SubmitGuess(int[] cards){}public void RequestRematch(){}public void Stop(){}
        public event EventHandler<int> RoundPreparing {add{} remove{}}
        public event EventHandler<MultiplayerFeedback> GuessAccepted {add{} remove{}}
        public event EventHandler<MultiplayerPublicState> StateChanged {add{} remove{}}
        public event EventHandler<MultiplayerOutcome> MatchEnded {add{} remove{}}
        public event EventHandler<string> RequestRejected {add{} remove{}}
    }
}

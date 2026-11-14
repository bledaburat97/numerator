using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using Game;
using Newtonsoft.Json;
using Scripts;
using UnityEngine;

public static partial class FruitLabTutorialEditorTools
{
    private static void ReviewGameScreenContracts(Canvas canvas,Camera camera)
    {
        ReviewAnnotationHold(canvas,camera);
        int attempts=0,version=1,saved=0;
        var retry=new CheckpointRetry(()=>{attempts++;if(attempts<3)throw new IOException("Injected checkpoint");saved=version;});
        retry.Request();Require(retry.Pending && attempts==1,"Failed normal checkpoint remains pending");
        version=2;retry.Tick(.4f);Require(attempts==1,"Checkpoint retries are throttled");retry.Tick(.6f);retry.Tick(1);
        Require(!retry.Pending && attempts==3 && saved==2,"Checkpoint retry stores the latest turn exactly once after recovery");
        var cancel=new CheckpointRetry(()=>throw new IOException());cancel.Request();cancel.Clear();cancel.Tick(2);Require(!cancel.Pending,"Finished/new round cancels old checkpoint work");
        ClearPreviewActors(canvas);
        var ordinary=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=8};
        using(var game=new Fixture(canvas,camera,ordinary))
        using(var rules=RulesFor(game,new LevelMechanicsController(game.Results,game.Helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker)))
        {
            game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3});
            var provider=GrowingSnapshot(game,ordinary);LevelSaveData checkpoint=null;int writes=0;
            rules.Attach(new LevelMechanicsPresenter(game.View,game.Board),()=>{
                Require(provider.TryCreateSnapshot(out var data),"A normal turn produces a complete checkpoint");
                if(++writes==1)throw new IOException("Injected turn write failure");checkpoint=data;
            });
            rules.Begin(LevelMechanic.Normal);SubmitSplit(game,new[]{4,5,6});
            Require(rules.HasPendingCheckpoint && game.Round.GetRemainingGuessCount()==7,"A failed checkpoint never rolls back the submitted guess or spent life");
            rules.TickCheckpoint(1,true);
            Require(!rules.HasPendingCheckpoint && checkpoint.RemainingGuessCount==7 && checkpoint.TriedCardsList.Count==1,"Automatic retry persists the real turn, history and life count");
        }
        ClearPreviewActors(canvas);
        var growing=new LevelData {Mechanic=LevelMechanic.Growing,NumOfBoardHolders=2,NumOfCards=8,MaxNumOfTries=8};
        using(var game=new Fixture(canvas,camera,growing))
        {
            game.BuildActors();game.Gate.Clear();var environment=LevelEnvironmentView.Ensure(canvas);environment.Render(growing,2,0);
            Canvas.ForceUpdateCanvases();var board=canvas.GetComponentInChildren<BoardAreaView>().GetRectTransform();
            var holders=board.GetComponentsInChildren<BoardHolderView>().OrderBy(view=>board.InverseTransformPoint(view.transform.position).x).ToArray();
            var futures=board.GetComponentsInChildren<UnityEngine.UI.Image>().Where(image=>image.name.StartsWith("FutureSlot")).OrderBy(image=>board.InverseTransformPoint(image.transform.position).x).ToArray();
            Require(holders.Length==2 && futures.Length==2,"Growing opens with two live and two future holders");
            var actual=holders[0].GetRectTransform();
            foreach(var future in futures)Require((future.rectTransform.rect.size-actual.rect.size).sqrMagnitude<.01f,"Future and live holders have identical dimensions");
            var left=new Vector3[4];var right=new Vector3[4];actual.GetWorldCorners(left);futures[1].rectTransform.GetWorldCorners(right);
            Require(Math.Abs(board.InverseTransformPoint(left[0]).x+board.InverseTransformPoint(right[2]).x)<1,"All four growing slots are centered together");
            Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-growing-centered.png");
            float fourthX=futures[1].rectTransform.anchoredPosition.x;
            using(var rules=GrowingRules(game,growing,GrowthFor(game,growing)))
            {
                rules.Attach(new LevelMechanicsPresenter(game.View,game.Board),null);rules.Begin(LevelMechanic.Growing);
                SubmitSplit(game,game.Target.GetTargetCardsList().AsEnumerable().Reverse().ToArray());
                var next=game.Target.GetTargetCardsList().AsEnumerable().Reverse().ToArray();
                for(int i=0;i<next.Length;i++)Require(game.Placement.TryPlaceCardOnBoard(next[i]-1,i),"Place a second growing guess");
                game.Check();
                Require(board.GetComponentsInChildren<BoardHolderView>().Length==3,"Growth creates the third holder before animation finishes");
                foreach(float elapsed in new[]{0f,.1f,.3f})
                {
                    Tick(elapsed);environment.RefreshLayout();
                    var future=board.GetComponentsInChildren<UnityEngine.UI.Image>().Single(image=>image.name=="FutureSlot0");
                    Require(Math.Abs(future.rectTransform.anchoredPosition.x-fourthX)<.1f,"Fourth slot keeps its final position throughout growth, without another guess");
                }
                Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-growing-third-holder.png");
            }
        }
        var elimination=new LevelData {Lesson=TutorialLessonId.EliminationNote,NumOfBoardHolders=2,NumOfCards=6,MaxNumOfTries=8};
        for(int first=1;first<=6;first++)for(int second=1;second<=6;second++)
        {
            if(first==second)continue;
            var guess=new[]{first,second};var answer=EliminationTutorialAnswer.Choose(elimination,guess,0);var feedback=new GuessEvaluation(1,guess,answer);
            Require(feedback.Correct==0 && feedback.Misplaced==0,"Every free opening pair gets a real zero/zero answer");
            Require(EliminationTutorialAnswer.Choose(elimination,guess,1)==null,"Elimination answer stays fixed after its first guess");
            var state=new TutorialLessonState {Lesson=TutorialLessonId.EliminationNote};var lesson=new TutorialLessonController(state.Lesson,state,null);
            Require(lesson.Current.Wait,"Free elimination opens without a guided placement");lesson.BindFirstGuess(guess);lesson.Accept(LessonSignal.Guess);
            Require(lesson.Current.Card==first-1,"First note points to the player's actual first fruit");
            var restored=JsonConvert.DeserializeObject<TutorialLessonState>(JsonConvert.SerializeObject(state));
            var resumed=new TutorialLessonController(state.Lesson,restored,null,guess);
            Require(resumed.Current.Card==first-1 && resumed.State.Step==state.Step,"Saved note teaching retains the chosen fruit");
        }
        var positionDefinition=new LevelData {Lesson=TutorialLessonId.PositionNote,NumOfBoardHolders=3,NumOfCards=6};
        int positionCases=0;
        for(int a=1;a<=6;a++)for(int b=1;b<=6;b++)for(int c=1;c<=6;c++)
        {
            if(a==b || a==c || b==c)continue;
            var guess=new[]{a,b,c};var answer=PositionTutorialAnswer.Choose(positionDefinition,guess,0);
            var feedback=new GuessEvaluation(1,guess,answer);
            Require(feedback.Correct==0 && feedback.Misplaced==3 && !feedback.Won,"Every freely chosen opening triple has a consistent zero/three answer");
            Require(PositionTutorialAnswer.Choose(positionDefinition,guess,1)==null,"Position answer remains fixed after the first guess");
            var state=new TutorialLessonState {Lesson=TutorialLessonId.PositionNote};var lesson=new TutorialLessonController(state.Lesson,state,null);
            Require(lesson.Current.Wait,"Position teaching starts with no placement instructions");lesson.BindFirstGuess(guess);lesson.Accept(LessonSignal.Guess);lesson.Accept(LessonSignal.Continue);
            Require(lesson.Current.Card==a-1,"Position teaching highlights the player's first chosen fruit");
            var resumed=new TutorialLessonController(state.Lesson,JsonConvert.DeserializeObject<TutorialLessonState>(JsonConvert.SerializeObject(state)),null,guess);
            Require(resumed.State.Step==state.Step && resumed.Current.Card==a-1,"Position lesson resumes with the same selected fruits");positionCases++;
        }
        Require(positionCases==120 && PositionTutorialAnswer.Choose(new LevelData(),new[]{1,2,3},0)==null,"Only the scheduled position lesson adapts its opening");
        var oldPosition=new TutorialLessonState {Lesson=TutorialLessonId.PositionNote,Revision=1,Step=4};
        var migratedPosition=new TutorialLessonController(oldPosition.Lesson,oldPosition,null,new[]{5,2,4});
        Require(migratedPosition.State.Step==1 && migratedPosition.Current.Expected==LessonSignal.Continue,"An older position lesson resumes from its recorded first guess without changing the answer");
        var progressed=new TutorialLessonController(TutorialLessonId.PositionNote,new TutorialLessonState {Lesson=TutorialLessonId.PositionNote,Revision=1,Step=10},null,new[]{5,2,4});
        Require(progressed.State.Step==6 && progressed.Current.Card==3,"Already completed position notes are not repeated during migration");
        var oldLesson=new TutorialLessonController(TutorialLessonId.Ultra,new TutorialLessonState {Lesson=TutorialLessonId.Ultra,Step=2},null);
        Require(!oldLesson.State.Completed && oldLesson.Current.Expected==LessonSignal.PowerApplied,"Unfinished older tutorial steps migrate to the revised flow");
        foreach(string failure in new[]{"save","delete","load"})
        {
            int sideEffects=0,loads=0;bool fail=true;
            var controller=new SettingsPopupController(_=>{if(failure=="load" && fail){fail=false;throw new IOException();}loads++;},()=>{});
            Action operation=()=>{if(failure!="load" && fail){fail=false;throw new IOException();}sideEffects++;};
            var method=typeof(SettingsPopupController).GetMethod(failure=="delete"?"OnRetryButtonClick":"OnMenuButtonClick",BindingFlags.NonPublic|BindingFlags.Instance);
            try{method.Invoke(controller,new object[]{operation});throw new Exception("Expected settings failure");}
            catch(System.Reflection.TargetInvocationException error){Require(error.InnerException is IOException,"Injected navigation failure propagates");}
            method.Invoke(controller,new object[]{operation});int once=sideEffects;method.Invoke(controller,new object[]{operation});
            Require(loads==1 && sideEffects==once,"Settings retries after "+failure+" failure and still blocks duplicate success");
        }
        var definition=new LevelData {Lesson=TutorialLessonId.Crystal,NumOfBoardHolders=3,NumOfCards=5,MaxNumOfTries=8};
        int compatible=0;
        for(int a=1;a<=5;a++)for(int b=1;b<=5;b++)for(int c=1;c<=5;c++)
        {
            if(a==b || a==c || b==c)continue;
            var first=new List<int>{a,b,c};var answer=CrystalTutorialAnswer.Choose(definition,first,new List<List<int>>(),new[]{1,2,3});
            var feedback=new GuessEvaluation(1,first,answer);Require(feedback.Correct==0 && feedback.Misplaced==1,"Every crystal opening has an actual zero/one answer");
            for(int x=1;x<=5;x++)for(int y=1;y<=5;y++)for(int z=1;z<=5;z++)
            {
                if(x==y || x==z || y==z)continue;
                var guess=new List<int>{x,y,z};var prior=new GuessEvaluation(1,first,guess);
                var selected=CrystalTutorialAnswer.Choose(definition,guess,new[]{first},answer);
                bool valid=prior.Correct==0 && prior.Misplaced==1;
                Require(valid==(selected!=null),"Only guesses consistent with the opening can become the answer");
                if(valid){compatible++;Require(new GuessEvaluation(2,guess,selected).Won,"Consistent crystal guess really wins");}
            }
        }
        Require(compatible>0 && CrystalTutorialAnswer.Choose(new LevelData(),new List<int>{1,2,3},new List<List<int>>(),new[]{1,2,3})==null,"Ordinary rounds never use adaptive answers");
        ClearPreviewActors(canvas);
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",2);game.BuildActors();ConfigureLessonFixture(game,definition);game.Bootstrap.StartRound();Tick(.6f);game.Next();Tick(.6f);
            Require(game.Bootstrap.ActiveLesson.Current==null && !game.Gate.IsRestricted,"Crystal lesson leaves all placements to the player");
            foreach(int fruit in new[]{0,1,2})game.Place(fruit);game.Check();Tick(.8f);
            var first=game._area.History.GetHistoryEntry(0);Require(first.correctPosCount==0 && first.wrongPosCount==1,"Actual crystal result is zero correct, one misplaced");
            foreach(int fruit in new[]{4,0,3})game.Place(fruit);game.Check();Tick(.8f);
            Require(game.Target.GetTargetCardsList().SequenceEqual(new[]{5,1,4}) && new GuessEvaluation(2,new[]{5,1,4},game.Target.GetTargetCardsList()).Won,"Actual compatible second guess becomes the saved winning answer");
            game.Bootstrap.RoundEnded(true);game.Bootstrap.ShowRewards(2,1);
            Require(game.Message!=null && game.Message.transform.Find("Title").GetComponent<TMPro.TMP_Text>().text=="Kristal Etkisi","Third lesson presents its crystal-only explanation");
            Require(game.Message.transform.Find("CurrencyFlow/Coin")==null && game.Message.transform.Find("CurrencyFlow/Seed")==null && !game.View.GetCoinImageRectTransform().gameObject.activeInHierarchy,"The crystal tip has no coin/seed illustrations or visible header coin");
            game.UI.RefreshCoinCounter();Require(!game.View.GetCoinImageRectTransform().gameObject.activeInHierarchy,"Wallet refreshes cannot expose the hidden coin during the tip");
            Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-third-lesson-crystal-only.png");
            game.Next();Require(game.View.GetCoinImageRectTransform().gameObject.activeInHierarchy,"Closing the crystal explanation restores the coin HUD");
            new LevelSuccessCleanupAnimationController(game.Initial,game.UI,null,game._area,game.Placement).FadeOutObsoleteGameUi(.1f);Tick(.2f);
            Require(game.View.GetUserText().alpha==1,"Level-end cleanup preserves the common header");
            game.Gate.Clear();game.UI.SetAllButtonsUnclickable();int settings=0;game.UI.OpenSettings+=(s,e)=>settings++;
            game.AssertHit(game.View.GetSettingsButton().GetButtonRectTransform(),typeof(UnityEngine.EventSystems.IPointerDownHandler));Click((BaseButtonView)game.View.GetSettingsButton());
            Require(settings==1,"End-state settings remain clickable while gameplay buttons are locked");
        }
        ClearPreviewActors(canvas);
        var basics=new LevelData {Lesson=TutorialLessonId.Basics,NumOfBoardHolders=2,NumOfCards=5,MaxNumOfTries=6};
        using(var game=new Fixture(canvas,camera,basics))
        {
            game.BuildActors();ConfigureLessonFixture(game,basics);game.Bootstrap.StartRound();Tick(.6f);
            Require(!game.Gate.AllowsButton(GameUIButtonType.Settings),"Basics locks settings with all unrelated input");
            var spot=canvas.GetComponentInChildren<TutorialSpotlightView>();var rect=game.View.GetSettingsButton().GetButtonRectTransform();
            Require(spot.IsRaycastLocationValid(UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,rect.position),camera),"Basics spotlight has no settings hole");
            int opened=0;game.UI.OpenSettings+=(s,e)=>opened++;Click((BaseButtonView)game.View.GetSettingsButton());Require(opened==0,"Basics cannot open settings through an underlying control");
        }
    }

    private static void ReviewAnnotationHold(Canvas canvas,Camera camera)
    {
        foreach(bool multiplayer in new[]{false,true})
        {
            ClearPreviewActors(canvas);
            var definition=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9};
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",7);if(multiplayer)Field(game.Tracker,"_gameOption",GameOption.MultiPlayer);
                game.BuildActors();game.Gate.Clear();
                using(var notes=AssistanceFor(game,definition,out _,false))
                using(var annotations=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,notes,game.Initial,game.Board))
                {
                    notes.Refresh();game.Initial.TryGetCardItem(0,out var card);
                    var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=19,position=RectTransformUtility.WorldToScreenPoint(camera,card.GetRectTransform().position)};
                    var move=card.GetCardMoveHandler();move.OnPointerDown(pointer);Tick(.65f);
                    Require(!annotations.IsSelecting,"A hold shorter than 0.7 seconds does not enter note editing");Tick(.1f);
                    Require(annotations.IsSelecting && annotations.GetPositionButton(0).gameObject.activeInHierarchy,"A 0.7-second hold opens the position and color controls");
                    var parent=card.GetRectTransform().parent;var position=card.GetRectTransform().anchoredPosition;
                    var drag=typeof(CardViewHandler).GetMethod("OnDrag",BindingFlags.Instance|BindingFlags.NonPublic);
                    pointer.position+=new Vector2(80,70);drag.Invoke(card.GetCardViewHandler(),new object[]{pointer});move.HandleDrag(pointer.position);move.OnPointerUp(pointer);
                    Require(annotations.IsSelecting && !move.IsDragStarted() && card.GetRectTransform().parent==parent && card.GetRectTransform().anchoredPosition==position,"The hold's remaining drag and release cannot move or deselect the card");
                    annotations.GetButton(2).onClick.Invoke();ClickPositionIndicator(game,annotations.GetPositionButton(0));
                    Require(annotations.IsSelecting,"Color and position edits keep the note mode open");
                    var overlay=annotations.Toolbar.parent.gameObject;
                    game.AssertHit(card.GetRectTransform(),typeof(UnityEngine.EventSystems.EventTrigger));
                    var tap=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,card.GetRectTransform().position)};
                    UnityEngine.EventSystems.ExecuteEvents.Execute(overlay,tap,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                    Require(!annotations.IsSelecting && !overlay.activeSelf && !annotations.GetPositionButton(0).gameObject.activeInHierarchy,"Tapping the card through the editor's backdrop closes every editing control");
                    move.OnPointerDown(pointer);drag.Invoke(card.GetCardViewHandler(),new object[]{pointer});
                    Require(move.IsDragStarted(),"Dragging is available again on a fresh gesture after editing closes");move.CancelPress();Tick(.4f);
                    Require(annotations.TrySelect(0),"Notes can be reopened without moving the card");
                    tap.position=Vector2.zero;UnityEngine.EventSystems.ExecuteEvents.Execute(overlay,tap,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
                    Require(!annotations.IsSelecting && !overlay.activeSelf,"A background tap also closes the editor");
                }
            }
        }
    }
    public static void ReviewPlaytest(Canvas canvas, Camera camera)
    {
        using var tutorialIsolation=TutorialProgress.UseTestValues(new Dictionary<string,int>());
        var oldUpdate = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        int stages = 0;
        var expectedMechanics = new[] { LevelMechanic.Normal,LevelMechanic.Normal,LevelMechanic.Normal,LevelMechanic.Normal,
            LevelMechanic.Normal,LevelMechanic.Normal,LevelMechanic.Normal,LevelMechanic.Growing,LevelMechanic.Helper,
            LevelMechanic.Normal,LevelMechanic.Normal,LevelMechanic.Dynamite,LevelMechanic.Pair,LevelMechanic.Normal,
            LevelMechanic.Normal,LevelMechanic.Ice,LevelMechanic.Split,LevelMechanic.Normal,LevelMechanic.Normal };
        var expectedPowers = new RewardType?[] {null,null,null,null,RewardType.Revealing,RewardType.Life,RewardType.Bomb,
            null,null,RewardType.Helper,null,null,null,RewardType.Pair,RewardType.Ultra,null,null,RewardType.Split,null};
        try
        {
            ValidateMechanicBindings();
            ValidateSessionAliases(true);
            ReviewGameScreenContracts(canvas,camera);
            Require(LevelPlaytestSchedule.Settings.Enabled && LevelPlaytestSchedule.Settings.StartLevel == 1, "Tutorial test starts at display level 1");
            Require(LevelPlaytestSchedule.Settings.Cycle.Count == 19 && LevelPlaytestSchedule.Settings.SeparateProfile, "Nineteen lessons use a separate phone profile");
            for (int cycle = 0; cycle < 12; cycle++)
            for (int index = 0; index < expectedMechanics.Length; index++)
            {
                int id = cycle * expectedMechanics.Length + index;
                Require(LevelPlaytestSchedule.TryCreate(id, out var data) && data.Mechanic == expectedMechanics[index]
                    && data.TrialPowerUp == expectedPowers[index] && data.PlaytestStep == index + 1
                    && (int)data.Lesson==index+1, "Deterministic repeated sequence at " + (id + 1));
                Require(LevelPlaytestSchedule.CanResume(id, data), "Matching playtest definitions resume");
                var changed = data.Copy(); changed.PlaytestRevision++;
                Require(!LevelPlaytestSchedule.CanResume(id, changed), "Old test revisions cannot override the new content profile");
                Require(!LevelPlaytestSchedule.CanResume(id, new LevelData { Mechanic = LevelMechanic.LegacyElimination }), "Old scheduled content migrates to the test cycle");
                Require(!LevelMechanicRules.ShouldTrigger(LevelMechanic.LegacyElimination, new GuessEvaluation(3, new[] {1,2}, new[] {3,4}), 4), "Removed rule never triggers");
                foreach (var power in PowerUpTypeUtility.OrderedRewardTypes) Require(PowerUpTypeUtility.IsUnlocked(power, id)==(id+1>=LevelPlaytestSchedule.IntroductionLevel(power)), "Powers unlock in teaching order");
            }
            Require(LevelDataGetter.GetStartingLevelsData().All(data => data.Mechanic == LevelMechanic.Normal)
                && LevelDataGetter.GetLoopLevelsData().All(data => data.Mechanic == LevelMechanic.Normal), "No permanent special-level placement remains");

            for (int index = 0; index < expectedMechanics.Length; index++)
            {
                ClearPreviewActors(canvas);
                int id = index;
                LevelPlaytestSchedule.TryCreate(id, out var definition);
                using (var game = new Fixture(canvas, camera, definition))
                {
                    Field(game.Tracker, "_levelId", id); Field(game.Tracker, "_starCount", 73); Field(game.Tracker, "_giftStarCount", 2);
                    foreach (var type in PowerUpTypeUtility.OrderedRewardTypes)Field(game.Tracker,Fixture.PowerStockField(type),0);
                    game.BuildActors(); game.Gate.Clear();
                    game.Target.RevealMultiplayerAnswer(new[] {4,1,3,5,2}.Take(definition.NumOfBoardHolders).ToArray());
                    var helper = game.Helper;
                    var pair = game.Pair;


                    var snapshot = new GameSaveSnapshotProvider(game.Target, game._info, new PlayingFlow(), game._counts, game.Round);
                    Field(snapshot, "_saveData", game._saves); Field(snapshot, "_levels", new TestLevel(definition)); Field(snapshot, "_tracker", game.Tracker);
                    using (var rules = RulesFor(game,new LevelMechanicsController(game.Results, helper, game.Round, game._saves,
                        game.Target, game._area, game._powers, game.Tracker, pair,(BoardSplitService)Get(game.Results,"_split"),game.Board)))
                    {
                        Field(rules, "_levels", new TestLevel(definition)); rules.Attach(new LevelMechanicsPresenter(game.View, game.Board), null);
                        LevelPlaytestSchedule.PrepareTrialStock(game.Tracker, game._saves, definition);
                        rules.Begin(definition.Mechanic); game._powers.Refresh(); Tick(.3f);
                        var caption = canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(text => text.name == "MechanicStatus");
                        string expectedTitle=definition.TrialPowerUp.HasValue && definition.Mechanic==LevelMechanic.Normal
                            ?PowerUpTypeUtility.Name(definition.TrialPowerUp.Value)+" denemesi":LevelIdentity.For(definition).Name;
                        Require(caption.text==expectedTitle,"Header identifies the actual mechanic or power trial without a debug sentence");
                        if (definition.Mechanic == LevelMechanic.Pair) Require(pair.Current != null, "Pair section gives its automatic clue");

                        void WrongGuess()
                        {
                            for (int fruit = 0; fruit < definition.NumOfBoardHolders; fruit++) game.Place(fruit);
                            game.Check(); Tick(.6f);
                        }
                        if (definition.Mechanic == LevelMechanic.Helper)
                        {
                            for (int guess = 0; guess < 3; guess++) WrongGuess();
                            Require(game._saves.GetLevelSaveData().RevealedGuessNumbers.SequenceEqual(new[] {3}), "Automatic Helper section reveals its third wrong guess");
                        }
                        if(definition.DisablePowerUps)
                        {
                            var powers=canvas.GetComponentInChildren<PowerUpListHolderView>(true);var layout=canvas.GetComponent<FruitLabGameLayout>();
                            var position=layout.BoardArea.position;var areaSize=layout.PowerUpArea.rect.size;
                            powers.SetLocked(false);powers.SetLocked(true);
                            Require(layout.BoardArea.position==position && layout.PowerUpArea.rect.size==areaSize && layout.PowerUpArea.gameObject.activeInHierarchy,"Hiding powers preserves the reserved bar and board layout");
                            Require(!canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetComponentsInChildren<PowerUpButtonView>(false).Any(),"No-powerup sections hide every power button");
                            Require(!canvas.GetComponentsInChildren<FruitLabGlyph>(false).Any(glyph=>glyph.name=="UseLock"),"No-powerup sections show no lock badges");
                            foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)
                            {
                                Field(game.Tracker,Fixture.PowerStockField(type),2);
                                Require(!game.Uses.CanUse(type) && game.Uses.TryApply(type)==PowerUpUseResult.Unavailable,"Powerups are forbidden by the service, not just the UI");
                            }
                        }
                        if (definition.TrialPowerUp.HasValue && definition.TrialPowerUp!=RewardType.Ultra)
                        {
                            var type = definition.TrialPowerUp.Value;
                            Require(game.Tracker.GetPowerUpCount(type) == 1, "Trial replenishes an empty inventory");
                            LevelPlaytestSchedule.PrepareTrialStock(game.Tracker, game._saves, definition);
                            Require(game.Tracker.GetPowerUpCount(type) == 1, "Preparation cannot accumulate extra stock");
                            if (type == RewardType.Helper) WrongGuess();
                            if (type == RewardType.Life) for (int n = 0; n < 2; n++) WrongGuess();
                            if (type == RewardType.Bomb)
                            {
                                var bomb = game.Bomb;
                                Field(bomb, "_pair", pair);
                            }
                            var button = (BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(type);
                            int remaining = game.Round.GetRemainingGuessCount();
                            Click(button);
                            if (type == RewardType.Revealing) Require(game.Reveal.TryRequestRevealCard(0), "Reveal trial accepts a chosen slot");
                            if (type == RewardType.Life)
                            {
                                Require(game.Round.GetRemainingGuessCount() == remaining + 2, "Extra lives commit before the visual animation");
                                Require(snapshot.TryCreateSnapshot(out var lifeSave) && lifeSave.RemainingGuessCount == remaining + 2, "Pausing a life animation preserves the awarded lives");
                            }
                            Tick(3.5f);
                            Require(game.Tracker.GetPowerUpCount(type) == 0, "Every trial actually consumes its prepared item");
                            if (type == RewardType.Bomb) Require(game.Target.GetTargetCardsList().Count == definition.NumOfBoardHolders - 1, "Bomb trial reduces the target");
                            if (type == RewardType.Revealing) Require(game._info.GetCardItemInfoList().Any(card => card.isLocked), "Reveal trial locks the correct fruit");
                            if (type == RewardType.Helper) Require(game._saves.GetLevelSaveData().RevealedGuessNumbers.Count == 1, "Manual Helper is usable without an automatic Helper rule");
                            if (type == RewardType.Pair) Require(pair.Current != null, "Manual Pair is usable without an automatic Pair rule");
                            if (type == RewardType.Split) Require(game._saves.GetLevelSaveData().BoardSplitApplied,"Manual Split activates the shared saved rule");
                            Require(snapshot.TryCreateSnapshot(out var saved) && saved.TrialStockPrepared && saved.Definition.TrialPowerUp == type, "Snapshots retain the completed trial preparation");
                            var loaded = JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(saved));
                            game._saves.SetLevelSaveDataAsSaved(loaded);
                            LevelPlaytestSchedule.PrepareTrialStock(game.Tracker, game._saves, loaded.Definition);
                            Require(game.Tracker.GetPowerUpCount(type) == 0, "Menu/resume cannot replenish the same used item twice");
                            game._saves.CreateDefaultLevelSaveData();
                            LevelPlaytestSchedule.PrepareTrialStock(game.Tracker, game._saves, definition);
                            Require(game.Tracker.GetPowerUpCount(type) == 1, "Retry/new attempts can test the item again");
                        }
                        Require(game.Tracker.GetLevelId() == id && game.Tracker.GetCoinCount() == 73 && game.Tracker.GetCrystalProgressCount() == 2, "Test stock preserves progress and currency balances");
                        Layer(canvas.transform); Draw(camera); Capture(camera, Folder + "/preview-playtest-" + (index + 1) + ".png");
                    }
                }
                stages++;
            }
            ReviewLessonProgress(canvas,camera);
            ReviewLessonPlacementResume(canvas,camera);
            ReviewPowerLessons(canvas,camera);
            ReviewLessonPhoneLayouts(canvas);
            using (LevelPlaytestSchedule.UseForTests(new LevelPlaytestSettings { Enabled = false }))
            {
                Require(!LevelPlaytestSchedule.TryCreate(99, out _), "One setting disables the temporary sequence");
                Require(!PowerUpTypeUtility.IsUnlocked(RewardType.Pair, 99) && !PowerUpTypeUtility.IsUnlocked(RewardType.Helper, 99), "New powers have no assigned production unlocks yet");
            }
            Require(errors.Count == 0, "No runtime/tween errors: " + string.Join("; ", errors));
            File.WriteAllText(Folder + "/playtest-review.json", JsonConvert.SerializeObject(new { passed = true, stages,
                repeated_cycles = 12, first_level = 1, real_scene_start_and_resume_cases=38, actual_power_lessons=7,
                checks = new[] { "nineteen repeatable lessons", "all current level types", "seven mandatory powerup uses", "removed elimination", "deterministic repeats", "trial resupply", "no duplicate on resume", "retry resupply", "life save during animation", "round definition migration", "currency/progress preserved", "test profile can be disabled", "real scene DI", "unchanged real PlayerPrefs", "outside touches preserve mandatory lessons", "small-phone lesson text", "adaptive crystal lesson with consistent feedback", "checkpoint retry", "navigation retry", "settings available while waiting for power readiness and blocked during guided actions", "manual notes for all three guessed fruits" }
            }, Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = oldUpdate; Application.logMessageReceived -= collect; }
        Debug.Log("PLAYTEST_PASS: nineteen-step lesson sequence, real notes and powerup use, resume, modal cleanup and twelve cycles");
    }

    private static void ReviewLessonProgress(Canvas canvas,Camera camera)
    {
        Require(LevelIdentity.For(new LevelData {NumOfBoardHolders=5}).Group==LevelGroup.Normal,"Five slots alone are not hard");
        Require(LevelIdentity.For(new LevelData {NumOfBoardHolders=4,DisablePowerUps=true}).Group==LevelGroup.Hard,"No-powerup rule has hard identity");
        Require(!MenuProgression.AreCrystalRewardsUnlocked(1) && MenuProgression.AreCrystalRewardsUnlocked(2),"Crystal UI starts with the third lesson");
        // Exercise every teaching path twice with independent checkpoints. The lesson
        // accepts only matching gameplay signals and keeps no hidden-answer reference.
        for(int cycle=0;cycle<2;cycle++)foreach(var definition in LevelPlaytestSchedule.Settings.Cycle)
        {
            if(definition.Lesson==TutorialLessonId.Basics)continue;
            var state=new TutorialLessonState {Lesson=definition.Lesson};int checkpoints=0;
            var lesson=new TutorialLessonController(definition.Lesson,state,()=>checkpoints++);
            while(lesson.Current!=null)
            {
                var step=lesson.Current;int phase=state.Step;
                var restored=JsonConvert.DeserializeObject<TutorialLessonState>(JsonConvert.SerializeObject(state));
                Require(new TutorialLessonController(definition.Lesson,restored,null).State.Step==phase,"Lesson stage survives serialization");
                Require(lesson.Accept(step.Expected,step.Card,step.Slot),"Every scripted lesson has a reachable next stage");
                Require(state.Step==phase+1,"One signal advances one stage");
            }
            Require(state.Completed && checkpoints>0,"Each encounter has independent completion");
        }
        foreach(int id in new[]{1,3,20,22})
        {
            ClearPreviewActors(canvas);LevelPlaytestSchedule.TryCreate(id,out var definition);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",id);game.BuildActors();game.Gate.Clear();
                using(var notes=AssistanceFor(game,definition,out _,false))
                using(var annotations=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,notes,game.Initial,game.Board))
                {
                    ConfigureLessonFixture(game,definition,annotations);
                    game.Bootstrap.StartRound();Tick(.6f);
                    int budget=30;bool dismissedNote=false;
                    while(game.Bootstrap.ActiveLesson?.Current!=null && budget-->0)
                    {
                        var step=game.Bootstrap.ActiveLesson.Current;
                        Require(!game.Gate.AllowsButton(GameUIButtonType.Settings),"Settings cannot exit an unfinished lesson, including wait phases");
                        if(step.Wait)
                        {
                            Require(step.Expected==LessonSignal.Guess && game.Message==null && !game.Gate.IsRestricted,"Note lessons begin with free play and no popup");
                            bool position=definition.Lesson==TutorialLessonId.PositionNote;
                            var fruits=position?(id==3?new[]{4,1,3}:new[]{2,0,4}):(id==1?new[]{0,4}:new[]{5,2});
                            foreach(int fruit in fruits)game.Place(fruit);game.Check();Tick(.8f);Tick(.3f);
                            var row=game._area.History.GetHistoryEntry(0);
                            Require(row.correctPosCount==0 && row.wrongPosCount==(position?3:0),"The player's opening receives the lesson's real zero/three or zero/zero feedback");
                            if(position)Require(game.Target.GetTargetCardsList().OrderBy(fruit=>fruit).SequenceEqual(fruits.Select(fruit=>fruit+1).OrderBy(fruit=>fruit)),"The position answer contains all three freely chosen fruits");
                            else Require(game.Target.GetTargetCardsList().All(fruit=>!fruits.Contains(fruit-1)),"The committed answer excludes both chosen fruits");
                            continue;
                        }
                        Require(game.Message!=null,"The actual lesson popup is visible");
                        if(definition.Lesson==TutorialLessonId.PositionNote && step.Expected==LessonSignal.PositionExcluded && !dismissedNote)
                        {
                            int previous=game.Bootstrap.ActiveLesson.State.Step;
                            annotations.Cancel();game.Bootstrap.Tick();dismissedNote=true;
                            Require(game.Bootstrap.ActiveLesson.Current.Expected==LessonSignal.NoteOpened && game.Bootstrap.ActiveLesson.State.Step==previous-1,"Closing the position editor returns to its long-press instruction without losing the lesson");
                            Tick(.8f);Tick(.3f);continue;
                        }
                        if(definition.Lesson==TutorialLessonId.PositionNote)
                        {
                            var message=(RectTransform)game.Message.transform;var board=game.Board.GetRectTransformOfGarden(0);
                            var corners=new Vector3[4];board.GetWorldCorners(corners);float boardTop=canvas.transform.InverseTransformPoint(corners[2]).y;
                            message.GetWorldCorners(corners);float bottom=canvas.transform.InverseTransformPoint(corners[0]).y;
                            Require(bottom>=boardTop && bottom-boardTop<40,"Position-note instructions sit immediately above the board");
                        }
                        var settings=canvas.GetComponentInChildren<GameUIView>().GetSettingsButton().GetButtonRectTransform();
                        Require(canvas.GetComponentInChildren<TutorialSpotlightView>().IsRaycastLocationValid(UnityEngine.RectTransformUtility.WorldToScreenPoint(camera,settings.position),camera),"Tutorials have no settings spotlight hole");
                        if(step.Expected!=LessonSignal.Continue)
                        {
                            int before=game.Bootstrap.ActiveLesson.State.Step;
                            canvas.GetComponentInChildren<TutorialSpotlightView>().OnPointerDown(new UnityEngine.EventSystems.PointerEventData(null));
                            game.Next();Require(game.Bootstrap.ActiveLesson.State.Step==before && !game.Bootstrap.ActiveLesson.State.Completed,"Unexpected touches cannot finish a required note step");
                        }
                        switch(step.Expected)
                        {
                            case LessonSignal.Continue:game.Next();break;
                            case LessonSignal.Placed:game.Place(step.Card);break;
                            case LessonSignal.Guess:game.Check();break;
                            case LessonSignal.NoteOpened:
                                Require(game.Initial.TryGetCardItem(step.Card,out var card),"Guided note card exists");
                                card.GetCardMoveHandler().OnPointerDown(null);Tick(CardMoveHandler.LongPressDuration+.05f);card.GetCardMoveHandler().OnPointerUp(null);
                                Require(annotations.IsSelecting,"A real long press opens the note menu through its scoped tutorial gate");break;
                            case LessonSignal.FruitExcluded:annotations.GetButton(0).onClick.Invoke();break;
                            case LessonSignal.PositionExcluded:
                                var hand=canvas.GetComponentInChildren<HandTutorialView>();
                                Require(hand.transform.parent==canvas.rootCanvas.transform && hand.transform.GetSiblingIndex()>annotations.Toolbar.parent.GetSiblingIndex(),"Tutorial hand renders in front of the position controls");
                                if(step.Slot==0){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-position-note-hand.png");}
                                ClickPositionIndicator(game,annotations.GetPositionButton(step.Slot));break;
                            default:throw new Exception("Unexpected note lesson stage: "+step.Expected);
                        }
                        Tick(.8f);Tick(.3f);
                    }
                    Require(budget>0 && game._saves.GetLevelSaveData().Tutorial.Completed,"The full real note lesson completes in both cycles");
                    Require(game.Gate.AllowsButton(GameUIButtonType.Settings),"Settings unlock when the lesson completes");
                    if(definition.Lesson==TutorialLessonId.PositionNote)
                    {
                        var opening=game.Round.GetTriedCardsList()[0];
                        for(int slot=0;slot<3;slot++)Require(!notes.State.IsPossible(opening[slot],slot),"Each freely chosen fruit has its own attempted position excluded");
                        var roundtrip=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(((IGameSaveService)Get(game,"_memory")).GetSavedLevel()));
                        Require(roundtrip.TargetCards.SequenceEqual(game.Target.GetTargetCardsList()),"The first position answer survives its saved checkpoint");
                    }
                    if(definition.Lesson==TutorialLessonId.EliminationNote)Require(game.Round.GetTriedCardsList()[0].All(fruit=>notes.State.Presence(fruit)==FruitPresence.Absent),"Both freely chosen fruits from the zero/zero example are excluded");
                    Require(definition.Lesson==TutorialLessonId.PositionNote?notes.IndicatorsVisible:!notes.IndicatorsVisible,"Only direct position editing opens indicator strips");
                    // Resume a note menu step without keeping a destroyed selection.
                    var saved=game._saves.GetLevelSaveData().Tutorial;saved.Completed=false;saved.Step=definition.Lesson==TutorialLessonId.PositionNote?7:2;
                    game.Bootstrap.Suspend(true);annotations.Cancel();game.Bootstrap.Resume();
                    Require(game.Message==null,"Background focus does not resume over an open modal");
                    game.Bootstrap.Resume(true);Tick(1.1f);Tick(.3f);
                    Require(game.Bootstrap.ActiveLesson.Current.Expected==LessonSignal.NoteOpened,"Modal return reopens the safe card-selection step");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-lesson-"+(id+1)+".png");
                    game.Bootstrap.Cancel();Require(!game.Gate.IsRestricted,"Scene cleanup releases the lesson gate without marking an incomplete lesson complete");
                }
            }
        }
        // Ultra uses exactly the shared powerup journal, notes engine and live effect.
        ClearPreviewActors(canvas);LevelPlaytestSchedule.TryCreate(14,out var ultraDefinition);
        using(var game=new Fixture(canvas,camera,ultraDefinition))
        using(var notes=AssistanceFor(game,ultraDefinition,out _,false))
        {
            Field(game.Tracker,"_levelId",14);game.BuildActors();game.Gate.Clear();
            game.PowerContainer.Bind<PositionAssistanceController>().FromInstance(notes);
            game.PowerContainer.Bind<ILevelSaveDataManager>().FromInstance(game._saves);
            game.PowerContainer.Bind<Scripts.Economy.UltraActivationService>().FromInstance(new Scripts.Economy.UltraActivationService(game.PowerProfiles));
            var ultra=game.PowerContainer.Instantiate<UltraPowerUpController>();Field(game._powers,"_ultra",ultra);
            LevelPlaytestSchedule.PrepareTrialStock(game.Tracker,game._saves,ultraDefinition);
            Require(game.Uses.TryApply(RewardType.Ultra)==PowerUpUseResult.Applied && notes.UltraActive,"Ultra trial enables the real normal-help engine");
            int stock=game.Tracker.GetPowerUpCount(RewardType.Ultra);
            Require(game.Uses.TryApply(RewardType.Ultra)==PowerUpUseResult.AlreadyActive && game.Tracker.GetPowerUpCount(RewardType.Ultra)==stock,"Ultra cannot consume twice");
        }
    }
    private static void ConfigureLessonFixture(Fixture game,LevelData definition,CardAnnotationController annotations=null)
    {
        Field(game.Bootstrap,"_levels",new TestLevel(definition));Field(game.Bootstrap,"_save",game._saves);
        Field(game.Bootstrap,"_saves",(IGameSaveService)Get(game,"_memory"));
        Field(game.Results,"_levels",new TestLevel(definition));Field(game.Results,"_knowledge",game._info);
        Field(game.Bootstrap,"_saves",Get(game,"_memory"));
        Field(game.Bootstrap,"_uses",game.PowerContainer.Resolve<Zenject.LazyInject<PowerUpUseService>>());
        var snapshot=new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
        Field(snapshot,"_saveData",game._saves);Field(snapshot,"_levels",new TestLevel(definition));Field(snapshot,"_tracker",game.Tracker);
        game.PowerContainer.Bind<IGameSaveSnapshotProvider>().FromInstance(snapshot);
        Field(game.Bootstrap,"_snapshots",game.PowerContainer.Resolve<Zenject.LazyInject<IGameSaveSnapshotProvider>>());
        game.Bootstrap.SetAnnotations(annotations);
        new LevelMechanicsPresenter(game.View,game.Board).ShowStatus(definition.Mechanic,definition,definition.NumOfBoardHolders,0,definition.Mechanic==LevelMechanic.Split);
    }
    private static void ReviewLessonPlacementResume(Canvas canvas,Camera camera)
    {
        foreach(int id in new[]{0,19})
        {
            ClearPreviewActors(canvas);LevelPlaytestSchedule.TryCreate(id,out var definition);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",id);game.BuildActors();ConfigureLessonFixture(game,definition);
                game.Bootstrap.StartRound();Tick(.6f);
                bool basic=definition.Lesson==TutorialLessonId.Basics;
                if(!basic){game.Next();Tick(.6f);}
                game.Place(basic?0:1);Tick(.6f);
                int step=game._saves.GetLevelSaveData().Tutorial.Step;
                Require(step==(basic?1:2),"The placement teaching stage is checkpointed");
                game.Bootstrap.Suspend(true);game.Bootstrap.Resume(true);Tick(.6f);
                Require(game._saves.GetLevelSaveData().Tutorial.Step==step && (basic?game.Step:game.Bootstrap.ActiveLesson.State.Step)==step,
                    "A focus interruption preserves the partially filled board and its exact teaching step");
                game.Place(basic?1:2);Tick(.6f);
                Require((basic?game.Step:game.Bootstrap.ActiveLesson.State.Step)==(basic?2:3),"The next placement after a modal remains actionable");
                // A real scene reload does not retain unsubmitted card placements.
                game.Bootstrap.PrepareRound();
                for(int fruit=0;fruit<definition.NumOfCards;fruit++)game.Placement.TryRemoveCardFromBoard(fruit);
                Tick(.5f);game.Bootstrap.StartRound();Tick(.6f);
                Require((basic?game.Step:game.Bootstrap.ActiveLesson.State.Step)==(basic?0:1),
                    "Rebuilding the scene rewinds only the unsubmitted placement segment");
            }
        }
    }
    private static void ReviewPowerLessons(Canvas canvas,Camera camera)
    {
        foreach(int id in new[]{4,5,6,9,13,14,17})
        {
            ClearPreviewActors(canvas);LevelPlaytestSchedule.TryCreate(id,out var definition);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",id);game.BuildActors();game.Gate.Clear();
                using(var notes=AssistanceFor(game,definition,out _,false))
                {
                    game.PowerContainer.Bind<PositionAssistanceController>().FromInstance(notes);
                    game.PowerContainer.Bind<ILevelSaveDataManager>().FromInstance(game._saves);
                    game.PowerContainer.Bind<Scripts.Economy.UltraActivationService>().FromInstance(new Scripts.Economy.UltraActivationService(game.PowerProfiles));
                    var ultra=game.PowerContainer.Instantiate<UltraPowerUpController>();Field(game._powers,"_ultra",ultra);
                    LevelPlaytestSchedule.PrepareTrialStock(game.Tracker,game._saves,definition);game._powers.Refresh();
                    ConfigureLessonFixture(game,definition);game.Bootstrap.StartRound();Tick(.6f);
                    if(game.Bootstrap.ActiveLesson.Current.Expected==LessonSignal.Continue){game.Next();Tick(.6f);}
                    var type=definition.TrialPowerUp.Value;
                    if(type==RewardType.Life || type==RewardType.Helper)
                    {
                        Require(game.Bootstrap.ActiveLesson.Current.Wait && !game.Gate.IsRestricted && game.Gate.AllowsButton(GameUIButtonType.Settings),"Waiting for power availability leaves settings and gameplay accessible");
                        int settingsOpened=0;EventHandler onSettings=(sender,args)=>settingsOpened++;
                        game.UI.OpenSettings+=onSettings;
                        try
                        {
                            var settings=(BaseButtonView)game.View.GetSettingsButton();
                            game.AssertHit(settings.GetButtonRectTransform(),typeof(UnityEngine.EventSystems.IPointerDownHandler));
                            Click(settings);Require(settingsOpened==1,"The real settings button responds during the level-six free-play wait");
                        }
                        finally{game.UI.OpenSettings-=onSettings;}
                        int waitingStep=game.Bootstrap.ActiveLesson.State.Step;
                        game.Bootstrap.Suspend(true);game.Bootstrap.Resume(true);Tick(.6f);
                        Require(game.Bootstrap.ActiveLesson.State.Step==waitingStep && game.Gate.AllowsButton(GameUIButtonType.Settings),"Closing settings resumes the same waiting lesson without relocking navigation");
                        int count=type==RewardType.Life?2:1;
                        for(int n=0;n<count;n++)
                        {
                            var answer=game.Target.GetTargetCardsList().ToArray();
                            for(int slot=0;slot<answer.Length;slot++)game.Place(answer[(slot+1)%answer.Length]-1);
                            game.Check();Tick(.7f);game.Bootstrap.Tick();Tick(.6f);
                        }
                    }
                    Require(game.Bootstrap.ActiveLesson.Current.Expected==LessonSignal.PowerApplied && !game.Gate.AllowsButton(GameUIButtonType.Settings),"Actual readiness opens the required power lesson with settings blocked");
                    if(type==RewardType.Revealing)
                    {
                        Require(game.Message.transform.Find("Title").GetComponent<TMPro.TMP_Text>().text=="Göster Güçlendiricisi" && !game.Message.transform.Find("NextButton").gameObject.activeSelf,"Reveal introduction directly requires the powerup click in the same popup");
                        var powers=canvas.GetComponentInChildren<PowerUpListHolderView>(true);var reveal=powers.GetPowerUpButton(type).GetButtonRectTransform();
                        var position=reveal.anchoredPosition;var size=reveal.rect.size;
                        powers.SetProgression(43);Require(Vector2.Distance(reveal.anchoredPosition,position)<.1f && Vector2.Distance(reveal.rect.size,size)<.1f,"One revealed powerup uses exactly its all-unlocked game slot and size");
                        powers.SetProgression(id);Require(reveal.rect.width<powers.GetRectTransform().rect.width*.2f,"The early reveal button never fills the strip");
                    }
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-lesson-action-"+type+".png");
                    var saved=game._saves.GetLevelSaveData().Tutorial;int phase=saved.Step,stock=game.Tracker.GetPowerUpCount(type);
                    game.Next();Require(!saved.Completed && saved.Step==phase && game.Tracker.GetPowerUpCount(type)==stock,"A required power lesson cannot be skipped by the hidden continuation button");
                    saved.Completed=false;saved.Step=phase;game.Bootstrap.Suspend(true);game.Bootstrap.Resume(true);Tick(.6f);
                    var button=(BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(type);
                    game.AssertHit(button.GetButtonRectTransform(),typeof(UnityEngine.EventSystems.IPointerDownHandler));Click(button);
                    if(type==RewardType.Revealing)
                    {
                        var holder=game.Board.GetRectTransformOfGarden(0);
                        game.AssertHit(holder,typeof(UnityEngine.EventSystems.IPointerClickHandler));
                        Require(game.Reveal.TryRequestRevealCard(0),"Reveal tutorial passes input to its board target");
                    }
                    Tick(.7f);
                    Require(game._saves.GetLevelSaveData().LastPowerUp==type && game.Tracker.GetPowerUpCount(type)==stock-1
                        && (saved.Completed || game.Bootstrap.ActiveLesson.Current.Expected==LessonSignal.Continue),"A committed effect advances or completes its actual lesson once");
                    // Simulate a close between the transaction and teaching update.
                    saved.Step=phase;saved.Completed=false;
                    game.Bootstrap.Suspend(true);game.Bootstrap.Resume(true);Tick(.6f);
                    Require((saved.Completed || game.Bootstrap.ActiveLesson.Current.Expected==LessonSignal.Continue) && game.Tracker.GetPowerUpCount(type)==stock-1,
                        "Resume recognizes the journaled power use without spending again");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-lesson-power-"+type+".png");
                    if(!saved.Completed)game.Next();Require(saved.Completed && !game.Gate.IsRestricted,"Effect teaching releases input after completion");
                }
            }
        }
    }
    private static void ReviewLessonPhoneLayouts(Canvas canvas)
    {
        var rect=(RectTransform)canvas.transform;var previous=rect.sizeDelta;
        try
        {
            foreach(var size in new[]{new Vector2(320,568),new Vector2(375,812)})
            {
                rect.sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                var popup=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<TutorialMessagePopupView>(MessagePath),canvas.transform,false);
                try
                {
                    popup.Init();
                    foreach(var definition in LevelPlaytestSchedule.Settings.Cycle)
                    {
                        var lesson=new TutorialLessonController(definition.Lesson,new TutorialLessonState {Lesson=definition.Lesson},null);
                        while(lesson.Current!=null)
                        {
                            var step=lesson.Current;
                            if(!step.Wait)
                            {
                                string open=(definition.NumOfBoardHolders-1).ToString();
                                popup.ShowAt(step.Title.Replace("{open}",open),step.Text.Replace("{open}",open),step.Expected==LessonSignal.Continue,step.Expected==LessonSignal.Continue?TutorialMessagePlacement.Center:TutorialMessagePlacement.Top);
                                Check();
                            }
                            lesson.Accept(step.Expected,step.Card,step.Slot);
                        }
                    }
                    popup.ShowAt("İlk kristalin","Coin → tohum\nKristal → güçlendirici",true,TutorialMessagePlacement.Center);
                    popup.ShowCrystalFlow();Check();
                    Require(popup.transform.Find("CurrencyFlow/Crystal")!=null && popup.transform.Find("CurrencyFlow/Power")!=null
                        && popup.transform.Find("CurrencyFlow/Coin")==null && popup.transform.Find("CurrencyFlow/Seed")==null,"Crystal teaching shows only crystal and powerup icons");
                    void Check()
                    {
                        var panel=(RectTransform)popup.transform;
                        var board=canvas.GetComponentInChildren<BoardAreaView>().GetRectTransform();
                        var corners=new Vector3[4];panel.GetWorldCorners(corners);float bottom=rect.InverseTransformPoint(corners[0]).y,top=rect.InverseTransformPoint(corners[2]).y;
                        board.GetWorldCorners(corners);float boardTop=rect.InverseTransformPoint(corners[2]).y;
                        Require(Mathf.Abs(panel.rect.width-board.rect.width)<1f && top<=rect.rect.yMax+1 && bottom>=boardTop+5,"Every lesson matches board width and fits above the board: "+popup.transform.Find("Title").GetComponent<TMPro.TMP_Text>().text);
                        Require(popup.transform.Find("Title").GetComponent<TMPro.TMP_Text>().font.name=="TutorialTitle" && popup.transform.Find("Body").GetComponent<TMPro.TMP_Text>().font.name=="TutorialBody","Tutorial uses the bundled Turkish-capable fonts");
                        foreach(var text in popup.GetComponentsInChildren<TMPro.TMP_Text>(false))
                        {text.ForceMeshUpdate();Require(!text.isTextOverflowing,"Lesson text remains readable without clipping: "+text.text);}
                    }
                }
                finally {UnityEngine.Object.DestroyImmediate(popup.gameObject);}
            }
        }
        finally {rect.sizeDelta=previous;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();}
    }
}

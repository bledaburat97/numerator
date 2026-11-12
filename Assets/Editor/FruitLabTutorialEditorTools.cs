using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using Game;
using Scripts;
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

// Explicit editor tasks, never an automatic profile reset on import or scene load.
public static partial class FruitLabTutorialEditorTools
{
    public static string BakePositionIndicators()
    {
        const string path = "Assets/Prefabs/PossibleHolderIndicator.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<PossibleHolderIndicatorView>();
            view.Configure(1, null); view.SetSize(new Vector2(10, InitialHolderView.IndicatorHeight));
            foreach (string name in new[] { "InnerImage", "CrossImage" })
            {
                var obsolete = root.transform.Find(name);
                if (obsolete != null) Object.DestroyImmediate(obsolete.gameObject);
            }
            Dirty(root); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return "Updated the existing position indicator prefab: compact numbered / cross / confirmation cells";
    }

    private static void ValidatePositionDeductions()
    {
        var noneCorrect = new PositionDeductionService(7, 3);
        Require(noneCorrect.ApplyGuess(1, new[] {1,2,3}, 0, 2), "Aggregate feedback is accepted");
        for (int i = 0; i < 3; i++) Require(!noneCorrect.IsPossible(i + 1, i), "Zero correct excludes the tried fruit/slot pairs");
        Require(noneCorrect.Presence(1) == FruitPresence.Unknown, "Two misplaced do not identify which two of three fruit are present");
        var noMisplaced = new PositionDeductionService(7, 3);
        noMisplaced.ApplyGuess(1, new[] {1,2,3}, 2, 0);
        for (int fruit = 1; fruit <= 3; fruit++)
        {
            Require(noMisplaced.FixedSlot(fruit) == -1 && noMisplaced.Presence(fruit) == FruitPresence.Unknown,
                "One remaining position is insufficient to confirm an unknown fruit");
            for (int slot = 0; slot < 3; slot++) Require(noMisplaced.IsPossible(fruit, slot) == (slot == fruit - 1), "Zero misplaced excludes other slots");
        }
        var absent = new PositionDeductionService(7, 3); absent.ApplyGuess(1, new[] {1,2,3}, 0, 0);
        for (int fruit = 1; fruit <= 3; fruit++) Require(absent.Presence(fruit) == FruitPresence.Absent, "Zero/zero excludes every guessed fruit");
        var allPresent = new PositionDeductionService(7, 3); allPresent.ApplyGuess(1, new[] {1,2,3}, 1, 2);
        for (int fruit = 1; fruit <= 7; fruit++) Require(allPresent.Presence(fruit) == (fruit <= 3 ? FruitPresence.Present : FruitPresence.Absent), "A complete set confirms membership and excludes other fruit");
        var cascade = new PositionDeductionService(7, 3);
        cascade.ApplyGuess(1, new[] {1,2,3}, 0, 3); cascade.ApplyGuess(2, new[] {4,1,2}, 0, 2);
        Require(cascade.FixedSlot(1) == 2 && cascade.FixedSlot(2) == 0 && cascade.FixedSlot(3) == 1,
            "Known membership and remaining slots propagate to a fixed point");
        Require(cascade.Reason(1, 2).Guesses.SequenceEqual(new[] {1,2}), "Derived confirmations retain their actual evidence rows");
        var column = new PositionDeductionService(5, 2);
        column.ApplyGuess(1, new[] {1,2}, 0, 0); column.ApplyGuess(2, new[] {3,4}, 1, 0); column.ApplyGuess(3, new[] {3,5}, 0, 1);
        Require(column.FixedSlot(5) == 0 && column.FixedSlot(4) == 1, "A unique candidate in a slot confirms unknown membership and propagates");
        var invalid = new PositionDeductionService(5, 3);
        Require(!invalid.ApplyGuess(1, new[] {1,1,3}, 0, 2) && !invalid.ApplyGuess(1, new[] {1,2,3}, 4, 0)
            && invalid.Presence(1) == FruitPresence.Unknown && invalid.IsPossible(1, 0), "Invalid input cannot create exclusions");
        int checkedFeedback = 0;
        var guesses = Permutations(5, 3).ToArray();
        foreach (var answer in guesses)
        foreach (var guess in guesses)
        {
            var evaluation = new GuessEvaluation(1, guess, answer);
            var state = new PositionDeductionService(5, 3); state.ApplyGuess(1, guess, evaluation.Correct, evaluation.Misplaced);
            AssertSoundDeduction(state, answer); checkedFeedback++;
        }
        var random = new System.Random(1946);
        for (int slots = 1; slots <= 5; slots++)
        for (int count = 0; count < 100; count++)
        {
            int fruits = slots == 5 ? 9 : 7;
            var answer = Enumerable.Range(1, fruits).OrderBy(_ => random.Next()).Take(slots).ToArray();
            var state = new PositionDeductionService(fruits, slots);
            for (int turn = 1; turn <= 15; turn++)
            {
                var guess = Enumerable.Range(1, fruits).OrderBy(_ => random.Next()).Take(slots).ToArray();
                var evaluation = new GuessEvaluation(turn, guess, answer);
                state.ApplyGuess(turn, guess, evaluation.Correct, evaluation.Misplaced);
                AssertSoundDeduction(state, answer); checkedFeedback++;
            }
        }
        Require(checkedFeedback == 11100, "The full 3/5 matrix and deterministic multi-guess cases were evaluated");
    }
    private static IEnumerable<int[]> Permutations(int fruits, int slots)
    {
        var values = new int[slots];
        IEnumerable<int[]> Next(int index)
        {
            if (index == slots) { yield return (int[])values.Clone(); yield break; }
            for (int fruit = 1; fruit <= fruits; fruit++)
            {
                bool exists = false; for (int previous = 0; previous < index; previous++) if (values[previous] == fruit) exists = true;
                if (exists) continue;
                values[index] = fruit;
                foreach (var result in Next(index + 1)) yield return result;
            }
        }
        return Next(0);
    }
    private static void AssertSoundDeduction(PositionDeductionService state, IReadOnlyList<int> answer)
    {
        Require(state.IsConsistent, "Legitimate public feedback cannot produce contradictory deductions");
        for (int fruit = 1; fruit <= state.FruitCount; fruit++)
        {
            if (state.Presence(fruit) == FruitPresence.Absent) Require(!answer.Contains(fruit), "An absent deduction never removes a true fruit");
            if (state.Presence(fruit) == FruitPresence.Present) Require(answer.Contains(fruit), "A membership deduction is always true");
            if (state.FixedSlot(fruit) >= 0) Require(answer[state.FixedSlot(fruit)] == fruit, "A confirmed position is always true");
            for (int slot = 0; slot < state.SlotCount; slot++) if (answer[slot] == fruit) Require(state.IsPossible(fruit, slot), "A true position is never excluded");
        }
    }
    private static PositionAssistanceController AssistanceFor(Fixture game, LevelData definition, out PositionAssistanceView view,bool ultra=true)
    {
        view = new PositionAssistanceView(game.Initial, game.View, game.Canvas.GetComponentInChildren<ResultAreaView>(true), game.UI, null);
        game.ExtraDisposables.Add(view);
        var assistance=new PositionAssistanceController(game._area.History, game.Initial, game._counts, new TestLevel(definition), game._info,
            (BoardSplitService)Get(game.Results, "_split"), view,game._saves);
        if(ultra)assistance.ActivateUltra();
        return assistance;
    }
    private static void SubmitAssistedGuess(Fixture game, int[] cards, PositionAssistanceController assistance)
    {
        game.UI.TriggerResetNumbers(); Tick(.5f);
        for (int slot = 0; slot < cards.Length; slot++) Require(game.Placement.TryPlaceCardOnBoard(cards[slot]-1, slot), "A chosen fruit is placeable");
        Tick(.5f); game.Check(); Tick(.6f); assistance.Tick();
    }
    private static void ClickPositionIndicator(Fixture game, PossibleHolderIndicatorView indicator)
    {
        Layer(game.Canvas.transform); Draw(game.Camera);
        var rect=indicator.GetRectTransform();
        var data=new PointerEventData(((GameObject)Get(game,"_events")).GetComponent<EventSystem>()) {
            position=RectTransformUtility.WorldToScreenPoint(game.Camera,rect.TransformPoint(rect.rect.center)), button=PointerEventData.InputButton.Left
        };
        var hits=new List<RaycastResult>();game.Canvas.GetComponent<GraphicRaycaster>().Raycast(data,hits);
        Require(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==indicator.gameObject,
            "The small indicator receives real UI raycasts without triggering card placement; first="+hits.FirstOrDefault().gameObject?.name);
        int guesses=game.Round.GetTriedCardsList().Count;
        ExecuteEvents.Execute(indicator.gameObject,data,ExecuteEvents.pointerClickHandler);
        Require(game.Round.GetTriedCardsList().Count==guesses,"Reading evidence cannot submit a guess");
    }

    public static void ReviewPositionAssistance(Canvas canvas, Camera camera)
    {
        ValidatePositionDeductions();
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>(); Application.LogCallback collect = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); };
        Application.logMessageReceived += collect;
        try
        {
            var definition = new LevelData { NumOfBoardHolders = 3, NumOfCards = 7, MaxNumOfTries = 9, Mechanic = LevelMechanic.Normal };
            LevelSaveData saved = null;
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas, camera, definition))
            {
                Field(game.Tracker, "_levelId", 7); game.BuildActors(); game.Gate.Clear(); game.Target.RevealMultiplayerAnswer(new[] {2,3,1});
                using (var assistance = AssistanceFor(game, definition, out var view))
                {
                    assistance.Refresh();
                    game.Initial.TryGetCardItem(0,out var heldCard);var move=heldCard.GetCardMoveHandler();
                    move.OnPointerDown(null);Tick(.8f);
                    Require(!canvas.GetComponentsInChildren<RectTransform>(true).Any(rect=>rect.name=="CardAnnotationOverlay" && rect.gameObject.activeSelf),"Long holds no longer open a probability toolbar");
                    move.OnPointerUp(null);Tick(.5f);game.UI.TriggerResetNumbers();Tick(.5f);
                    Require(game.Round.GetTriedCardsList().Count==0,"A long hold is not a submitted guess");
                    Require(game.Initial.GetHolderView(0).Indicators.Count == 3, "Every fruit holder displays one numbered cell per board slot");
                    SubmitAssistedGuess(game, new[] {1,2,3}, assistance);
                    Require(game.Initial.GetHolderView(0).Indicators[0].IsExcluded, "A submitted zero-correct guess creates the right crosses");
                    Require(((NormalCardItemView)game.Initial.GetRectTransformOfCardItem(0).GetComponent<NormalCardItemView>()).DeducedPresence == FruitPresence.Present, "Complete membership gives a presence badge");
                    Require(game.Initial.GetHolderView(3).Indicators.All(indicator => indicator.IsExcluded), "Absent fruit have all their cells crossed");
                    Require(game.Initial.TryGetCardItem(3, out var absent) && ((NormalCardItemView)absent.GetView()).DeducedPresence == FruitPresence.Absent
                        && ((NormalCardItemView)absent.GetView()).GetImage().color.a <= .33f, "Absent fruit stay available, faded and crossed without being deleted");
                    ClickPositionIndicator(game,game.Initial.GetHolderView(0).Indicators[0]);
                    var history = canvas.GetComponentInChildren<ResultAreaView>(true);
                    Require(view.IsReasonVisible && view.ReasonText.Contains("1. tahminde doğru yerde meyve yoktu") && history.EvidenceRows.Contains(0),
                        "Clicking a cross explains its public evidence and highlights the right history row");
                    Layer(canvas.transform); Draw(camera); Capture(camera, Folder + "/preview-position-reason.png"); view.CloseReason();
                    game.View.SetPairClue(new PairClue(1,2));
                    for(int frame=0;frame<60;frame++)game.View.GetPairView().RefreshRope(1f/60);
                    var cell=game.Initial.GetHolderView(0).Indicators[0];var point=RectTransformUtility.WorldToScreenPoint(camera,cell.transform.position);
                    Require(game.View.GetPairView().IsOverControls(point,camera),"The pair rope yields input to the indicator strip");
                    ClickPositionIndicator(game,cell);Require(view.IsReasonVisible,"Evidence remains tappable with an active pair rope");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-position-pair.png");
                    view.CloseReason();game.View.SetPairClue(null);
                    SubmitAssistedGuess(game, new[] {4,1,2}, assistance);
                    Require(assistance.State.FixedSlot(1) == 2 && game.Initial.GetHolderView(0).Indicators[2].IsConfirmed, "A confirmed position displays a green check");
                    Require(game._info.GetCardItemInfoList().All(info => !info.isLocked && info.isExisted), "Inferences never change real reveal locks or eliminate cards from the game state");
                    Require(game.Placement.GetCardsOnBoard().Count == 0, "Inferences do not place cards on behalf of the player");
                    var provider = new GameSaveSnapshotProvider(game.Target, game._info, new PlayingFlow(), game._counts, game.Round);
                    Field(provider, "_saveData", game._saves); Field(provider, "_tracker", game.Tracker); Field(provider, "_levels", new TestLevel(definition));
                    Require(provider.TryCreateSnapshot(out saved), "The ordinary checkpoint contains the accepted guesses");
                    saved = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelSaveData>(Newtonsoft.Json.JsonConvert.SerializeObject(saved));
                    Layer(canvas.transform); Draw(camera); Capture(camera, Folder + "/preview-position-confirmed.png");
                }
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas, camera, definition))
            {
                Field(game.Tracker, "_levelId", 7); ((IGameSaveService)Get(game, "_memory")).Save(saved); game._saves.SetLevelSaveDataAsSaved(saved);
                game.Round.Initialize(); game.BuildActors(); game.Results.TryAddTriedCards(); game.Gate.Clear();
                using (var assistance = AssistanceFor(game, definition, out var view))
                {
                    assistance.Refresh();
                    Require(assistance.State.FixedSlot(1) == 2 && assistance.State.Reason(1, 2).Guesses.SequenceEqual(new[] {1,2}), "Save/resume reconstructs deductions and reasons from existing history");
                    game._area.History.Clear(); assistance.Tick();
                    Require(assistance.State.Presence(1) == FruitPresence.Unknown && game.Initial.GetHolderView(0).Indicators.All(cell => !cell.IsExcluded && !cell.IsConfirmed), "A new history generation clears all deductions");
                }
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas, camera, definition))
            {
                Field(game.Tracker,"_levelId",7); Field(game.Tracker,"_gameOption",GameOption.MultiPlayer);game.BuildActors();game.Gate.Clear();game.UI.InitializeForMultiplayer();
                using(var assistance=AssistanceFor(game,definition,out var view))
                {
                    assistance.Refresh();
                    game._area.AddResultBlock(new ResultBlockModel{finalNumbers=new List<int>{1,2,3},correctPosCount=0,wrongPosCount=2,isLocalGuess=false,playerName="Rakip"});
                    assistance.Tick();
                    Require(!assistance.State.IsPossible(1,0),"Opponent feedback also updates local deductions");
                    var layout=canvas.GetComponent<FruitLabGameLayout>();
                    Require(!layout.PowerUpArea.gameObject.activeSelf,"The multiplayer powerup row remains hidden normally");
                    ClickPositionIndicator(game,game.Initial.GetHolderView(0).Indicators[0]);
                    Require(view.IsReasonVisible && layout.PowerUpArea.gameObject.activeSelf,"Multiplayer explanations get their own visible space");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-position-multiplayer.png");
                    view.CloseReason();Require(!layout.PowerUpArea.gameObject.activeSelf,"Closing an explanation restores multiplayer layout");
                }
                game.View.SetPositionIndicators(false);game.View.SetOpponentInfoStatus(false);
            }
            ClearPreviewActors(canvas);
            var four=new LevelData{NumOfBoardHolders=4,NumOfCards=8,MaxNumOfTries=10};
            using(var game=new Fixture(canvas,camera,four))
            {
                Field(game.Tracker,"_levelId",20);Field(game.Tracker,"_splitPowerUpCount",1);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3,4});
                using(var assistance=AssistanceFor(game,four,out var view))
                {
                    assistance.Refresh();
                    _ = game.Bomb;
                    game._powers.Refresh();Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Bomb));Tick(1.2f);assistance.Tick();
                    Require(assistance.State.SlotCount==3 && game.Initial.GetHolderView(0).Indicators.Count(cell=>cell.gameObject.activeSelf)==3,"Bomb resizes the visible cells and resets the deduction schema");
                    SubmitAssistedGuess(game,new[]{2,3,1},assistance);Require(assistance.State.Presence(1)==FruitPresence.Present,"Bomb leaves valid public inference feedback");
                    game._powers.Refresh();Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Split));assistance.Tick();
                    // Bomb has left three slots; Split cannot activate on three. Test an actual mid-round split on a fresh four-slot board below.
                }
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,four))
            {
                Field(game.Tracker,"_levelId",20);Field(game.Tracker,"_splitPowerUpCount",1);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3,4});
                using(var assistance=AssistanceFor(game,four,out var view))
                {
                    assistance.Refresh();SubmitAssistedGuess(game,new[]{2,3,4,1},assistance);
                    Require(assistance.State!=null,"Ordinary boards initially offer assistance");
                    game._powers.Refresh();Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Split));assistance.Tick();
                    Require(assistance.State!=null && game.View.HasPositionIndicators,"Split retains the unified notebook while stopping whole-board deduction");
                    Require(game.Initial.TryGetCardItem(0,out var card) && ((NormalCardItemView)card.GetView()).DeducedPresence==FruitPresence.Present,"Split retains existing valid membership notes");
                }
            }
            foreach (var mechanic in new[] {LevelMechanic.Growing, LevelMechanic.Split})
            {
                var special = new LevelData { NumOfBoardHolders = mechanic == LevelMechanic.Split ? 4 : 2, NumOfCards = 9, MaxNumOfTries = 9, Mechanic = mechanic };
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas, camera, special))
                {
                    Field(game.Tracker, "_levelId", 8); game.BuildActors();
                    using (var assistance = AssistanceFor(game, special, out var view))
                    {
                        assistance.Refresh(); Require(assistance.State != null && game.View.HasPositionIndicators && assistance.State.Presence(1)==FruitPresence.Unknown, "Growing and split boards expose an editable notebook without automatic deductions");
                        assistance.TogglePosition(1,0);Require(!assistance.State.IsPossible(1,0),"Manual notes work on growing and split boards");
                    }
                }
            }
            int layouts = 0;
            foreach (var size in new[] {new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
            for (int slots = 2; slots <= 5; slots++)
            for (int fruits = 5; fruits <= 9; fruits++)
            {
                ClearPreviewActors(canvas); ((RectTransform)canvas.transform).sizeDelta = size; canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                var level = new LevelData {NumOfBoardHolders=slots,NumOfCards=fruits,MaxNumOfTries=9};
                using (var game = new Fixture(canvas, camera, level))
                {
                    Field(game.Tracker,"_levelId",7);game.BuildActors();game.Gate.Clear();
                    using (var assistance = AssistanceFor(game, level, out var view))
                    {
                        assistance.Refresh(); Canvas.ForceUpdateCanvases();
                        var area = canvas.GetComponentInChildren<InitialCardAreaView>(true).GetRectTransform();
                        var historyArea=canvas.GetComponentInChildren<ResultAreaView>(true);
                        var scroll=historyArea.GetComponentInParent<ScrollRect>();
                        Require(historyArea.GetRowHeight()*6+historyArea.GetResultAreaInfo().spacing*7<=scroll.viewport.rect.height+.1f,"Six history rows remain available with indicators");
                        for (int fruit = 0; fruit < fruits; fruit++)
                        {
                            var holder=game.Initial.GetHolderView(fruit); var body=holder.GetRectTransform();
                            Require(holder.Indicators.Count==slots,"Slot count matches the current board");
                            var corners=new Vector3[4];holder.Indicators[0].GetRectTransform().GetWorldCorners(corners);
                            Vector2 low=area.InverseTransformPoint(corners[0]);
                            holder.Indicators[slots-1].GetRectTransform().GetWorldCorners(corners);Vector2 high=area.InverseTransformPoint(corners[2]);
                            Require(low.x>=area.rect.xMin-.5f && high.x<=area.rect.xMax+.5f && low.y>=area.rect.yMin-.5f && high.y<=area.rect.yMax+.5f,
                                "Indicator strip fits its fruit area at "+size+", board="+slots+", fruits="+fruits+", low="+low+", high="+high);
                            Require(Mathf.Abs((high.x-low.x)-body.rect.width)<1,"All cells together span the fruit holder width");
                            Require(holder.Indicators.All(cell=>cell.GetRectTransform().rect.width>=7 && cell.GetComponentInChildren<TMP_Text>(true).fontSize>=8),"Numbers and symbols remain readable");
                            var card = game.Initial.GetRectTransformOfCardItem(fruit).GetComponent<NormalCardItemView>();
                            Color expected = Color.Lerp(card.GetFruitColor(fruit+1),FruitLabStyle.Text,.35f); expected.a=1;
                            Require(holder.Indicators.All(cell=>cell.GetComponentInChildren<TMP_Text>(true).color==expected),"Each fruit's digits retain its own readable card colour");
                            Color fg=expected.linear, bg=FruitLabStyle.Recess.linear;
                            float contrast=(.2126f*fg.r+.7152f*fg.g+.0722f*fg.b+.05f)/(.2126f*bg.r+.7152f*bg.g+.0722f*bg.b+.05f);
                            Require(contrast>=4.5f,"Small fruit-coloured numbers have sufficient contrast: fruit="+(fruit+1)+", ratio="+contrast);
                        }
                        if(slots==5 && fruits==9)
                        {Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-position-5-9-"+(int)size.x+"-"+(int)size.y+".png");}
                        layouts++;
                    }
                }
            }
            Require(errors.Count==0,"No Unity errors during positional assistance review: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/position-assistance-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                passed=true,publicFeedbackChecks=11100,layouts,saveResume=true,ordinaryStateUnchanged=true,reasonAndHistoryFocus=true,growingAndSplitDisabled=true,multiplayer=true,bombResize=true,midRoundSplit=true
            },Newtonsoft.Json.Formatting.Indented));
        }
        finally
        {
            ((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
            DOTween.defaultUpdateType=previous;Application.logMessageReceived-=collect;
        }
    }

    public static void ReviewCrystalHome(Canvas canvas, Camera camera)
    {
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        try
        {
            foreach (int slots in new[] {3,5})
            foreach (bool targetOnBoard in new[] {true,false})
            {
                ClearPreviewActors(canvas);
                var level = new LevelData {NumOfBoardHolders=slots,NumOfCards=slots+4,MaxNumOfTries=9,Mechanic=LevelMechanic.Normal};
                using (var game = new Fixture(canvas,camera,level))
                {
                    Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();
                    var answer = Enumerable.Range(1,slots).ToArray();game.Target.RevealMultiplayerAnswer(answer);
                    var query = (IBoardPlacementQuery)Get(game.Placement,"_boardPlacementQuery");
                    var hints = new HintProvider(game._guess,game.Target,game._info,game.Initial,query,game.Placement,game.Locks);
                    game.Results.TurnFlow.Hints=hints;
                    Field(hints,"_round",game.Round);
                    Field(hints,"_eliminate",new EliminateFruitEffect(game.Target,game._info,game._saves));
                    CrystalView crystal = null;int flights=0;
                    game._guess.HintRewardTokenEvent += (sender,args)=>{crystal=(CrystalView)args.RewardTokenView;flights++;};
                    int boundary = game.Round.GetLifeBarRewardInfoList().Where(reward=>reward.IsCrystal).Max(reward=>reward.BoundaryIndex);
                    var guess = targetOnBoard ? answer.Take(slots-1).Concat(new[]{slots+1}).ToArray() : answer.Skip(1).Concat(new[]{1}).ToArray();
                    while(game.Round.GetRemainingGuessCount()>boundary)
                    {
                        for(int i=0;i<slots;i++)Require(game.Placement.TryPlaceCardOnBoard(guess[i]-1,i),"The crystal regression submits a real playable board");
                        Tick(.5f);game.Check();
                        if(crystal==null){Tick(1.5f);continue;}
                        int removed=game._info.GetCardItemInfoList().FindIndex(info=>!info.isExisted);
                        Require(removed>=0 && (!targetOnBoard || removed==slots),"The first lost crystal eliminates the absent fruit from the submitted guess when possible");
                        Require(game.Initial.TryGetCardItem(removed,out var card),"Logical elimination precedes visual removal");
                        var rect=card.GetView().GetRectTransform();var home=game.Initial.GetHolderView(removed).GetRectTransform();
                        Require(card.GetCardMoveHandler().IsMovementLocked() && query.GetOccupiedCardIndexesOnBoard().All(index=>index<0),"Pending elimination cannot be dragged or retained in logical board slots");
                        Tick(.35f);
                        Require(rect.parent==home && Vector3.Distance(rect.position,home.position)<.05f,"The eliminated fruit returns home with the other cards before crystal impact");
                        float flight=crystal.GetFlightDuration(false);
                        for(float elapsed=.35f;elapsed<flight+.03f;elapsed+=.01f)Tick(.01f);
                        float miss=Vector3.Distance(crystal.GetRectTransform().TransformPoint(crystal.GetRectTransform().rect.center),rect.TransformPoint(rect.rect.center));
                        Require(rect.parent==home && miss<.05f,"The crystal hits the fruit centre on its initial holder; miss="+miss);
                        if(slots==5 && targetOnBoard){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-crystal-home-impact.png");}
                        Tick(.6f);
                        Require(rect==null && home!=null && !game.Initial.TryGetCardItem(removed,out _),"Impact removes only the fruit and preserves its initial holder");
                        Require(flights==1 && game.Round.GetTriedCardsList().Count==9-boundary && game.Round.GetRemainingGuessCount()==boundary,"Crystal return never submits an extra guess or consumes extra life");
                        GC.KeepAlive(hints);
                        break;
                    }
                    Require(flights==1,"Actual wrong guesses must cross a crystal reward boundary");
                }
            }
            Debug.Log("FRUIT_LAB_CRYSTAL_HOME_PASS: real wrong-guess boundaries on 3/5 slots, board/home targets, return-before-impact, model and holder preservation.");
        }
        finally {DOTween.defaultUpdateType=previous;}
    }

    public static string ReviewSessionBindings()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        try
        {
            foreach (var mode in new[] { GameOption.SinglePlayer, GameOption.MultiPlayer })
            {
                var root = new GameObject("SessionValidationTracker"); root.SetActive(false); SceneManager.MoveGameObjectToScene(root, scene);
                try
                {
                    var tracker = root.AddComponent<LevelTracker>(); tracker.SuppressPersistenceForTests = true; Field(tracker, "_gameOption", mode);
                    var container = new DiContainer(true); container.Settings = new ZenjectSettings(ValidationErrorResponses.Throw);
                    container.Bind<Context>().FromInstance(Find<SceneContext>(scene));
                    container.Bind<ILevelTracker>().FromInstance(tracker);
                    BindCollectionTestRewards(container);
                    container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
                    container.Bind<IHapticController>().To<HapticController>().AsSingle();
                    container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
                    var installer = Find<GameMonoInstaller>(scene);
                    typeof(MonoInstallerBase).GetProperty("Container", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(installer, container);
                    installer.InstallBindings();
                    container.Resolve(typeof(ILevelSession));
                    container.Resolve(typeof(ISinglePlayerProgression));
                    container.Resolve(typeof(IMultiplayerHudPresentation));
                    container.Resolve(typeof(IMultiplayerResultPresentation));
                    container.Resolve(typeof(IInitialCardAreaPresentation));
                    container.Resolve(typeof(SinglePlayerSession)); container.Resolve(typeof(MultiplayerSession));
                    container.Resolve(typeof(PositionAssistanceController));
                    container.ResolveRoots();
                }
                finally { Object.DestroyImmediate(root); }
            }
            return "Singleplayer and multiplayer session dependency graphs validated with the real Game scene installer; no PlayerPrefs writes";
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    public static void ReviewPerformance(Canvas canvas, Camera camera)
    {
        ClearPreviewActors(canvas);
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        try
        {
            using (var game = new Fixture(canvas, camera))
            {
                game.BuildActors(); game.Gate.Clear();
                var area = canvas.GetComponentInChildren<ResultAreaView>(true);
                var answer = new[] { 1, 2, 3, 4, 5 };
                var cards = new List<int> { 1, 3, 2, 6, 7 };
                var clock = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++)
                {
                    var evaluation = new GuessEvaluation(i + 1, cards, answer, i % 2 == 0 ? BoardPartition.ForCount(5) : null);
                    game._area.AddResultBlock(new ResultBlockModel { finalNumbers = cards, correctPosCount = evaluation.Correct,
                        wrongPosCount = evaluation.Misplaced, groups = evaluation.Groups });
                    game._area.LabelLatestMultiplayerGuess(i % 2 == 0, "Oyuncu " + i);
                    if (i == 0) game._area.RevealGuess(evaluation);
                }
                clock.Stop();
                Require(game._area.HistoryCount == 1000 && area.HistoryCount == 1000, "All guesses remain available in the data model");
                Require(area.VisibleRowCount <= 12 && area.AllocatedRowCount <= 12, "A long match uses a bounded number of UI rows");
                int allocated = area.AllocatedRowCount;
                var scroll = area.GetComponentInParent<ScrollRect>();
                scroll.verticalNormalizedPosition = 1; scroll.onValueChanged.Invoke(Vector2.up);
                var first = area.GetVisibleRow(0);
                Require(first != null && first.IsLocalGuess == true && first.IsSplit, "Scrolling back restores old player identity and split counters");
                Require(first.transform.Find("PlayerLabel").GetComponent<TMP_Text>().text == "Sen", "A recycled row has the correct player label");
                Require(first.GetComponentsInChildren<Transform>().Any(t => t.name == "HelperAbsent"), "Offscreen detailed hints survive row recycling");
                game._area.RevealAnswer(answer);
                scroll.verticalNormalizedPosition = 0; scroll.onValueChanged.Invoke(Vector2.zero);
                Require(area.GetVisibleRow(999) != null && area.GetVisibleRow(999).IsLocalGuess == false, "The latest opponent row remains accessible after review");
                scroll.verticalNormalizedPosition = 1; scroll.onValueChanged.Invoke(Vector2.up);
                first = area.GetVisibleRow(0);
                Require(game._area.History.GetHistoryEntry(0).reviewStates[0] == CardResultState.Correct
                    && game._area.History.GetHistoryEntry(0).reviewStates[1] == CardResultState.Absent
                    && game._area.History.GetHistoryEntry(0).reviewStates[2] == CardResultState.Absent
                    && game._area.History.GetHistoryEntry(1).reviewStates[1] == CardResultState.Misplaced,
                    "Offscreen review preserves group-local exclusion for split rows and misplaced status for normal rows");
                Require(area.AllocatedRowCount == allocated, "Scrolling and review reuse the existing row pool");
                game._area.History.Clear();
                Require(area.VisibleRowCount == 0 && game._area.HistoryCount == 0, "Rematch clears history and releases visible rows");
                for (int i = 0; i < 8; i++) game._area.AddResultBlock(new ResultBlockModel { finalNumbers = new List<int> { 2, 1 } });
                Require(area.GetComponentsInChildren<ResultBlockView>().All(row => !row.IsSplit && row.IsLocalGuess == null
                    && !row.GetComponentsInChildren<Transform>().Any(t => t.name == "HelperAbsent")), "New rounds clear old groups, players, hints and review frames");
                using (var assistance=AssistanceFor(game,new TestLevel().GetLevelData(),out var noteView,ultra:false))
                using (var notes = new CardAnnotationController(game.Initial, game.Placement, game.UI, canvas.GetComponent<FruitLabGameLayout>(), game.Gate, null, () => true,assistance,game.Initial,game.Board))
                {
                    Require(notes.TrySelect(0), "Performance fixture can open marking");
                    notes.Tick();
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    for (int i = 0; i < 600; i++) notes.Tick();
                    long annotationBytes = GC.GetAllocatedBytesForCurrentThread() - before;
                    Require(annotationBytes == 0, "Idle marking ticks do not allocate managed memory");
                }
                double now = 0; int timerUpdates = 0, seconds = 0; bool timerVisible = false;
                var timer = new MultiplayerHudPresenter(() => now, (value, visible) => { timerUpdates++; seconds = value; timerVisible = visible; });
                timer.SetDeadline(60);
                long timerBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 600; i++) timer.Tick();
                long timerBytes = GC.GetAllocatedBytesForCurrentThread() - timerBefore;
                Require(timerUpdates == 1 && timerBytes == 0, "An unchanged timer does not write text or allocate");
                now = 15.2; timer.Tick(); Require(seconds == 45, "The display follows the server deadline after a time jump");
                now = 61; timer.Tick(); Require(seconds == 0, "An expired deadline never displays a negative time");
                timer.SetDeadline(0); Require(!timerVisible, "Match end hides the timer");
                var simulation = new PairRopeSimulation();
                var a = new Vector2(-60, 0); var b = new Vector2(60, 0);
                simulation.Reset(a, b);
                for (int i = 0; i < 2400; i++) simulation.Advance(a, b, 1f / 60);
                Require(simulation.IsSleeping, "A settled rope sleeps while preserving its shape");
                int revision = simulation.Revision;
                for (int i = 0; i < 120; i++) simulation.Advance(a, b, 1f / 60);
                Require(simulation.Revision == revision, "A sleeping rope does not rebuild its mesh");
                simulation.Advance(a + new Vector2(20, 10), b, 1f / 60);
                Require(!simulation.IsSleeping && simulation.Revision > revision, "Moving a card wakes the rope immediately");
                File.WriteAllText(Folder + "/performance-review.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    passed = true, history = 1000, allocatedRows = allocated, historyBuildMilliseconds = clock.Elapsed.TotalMilliseconds,
                    idleTimerBytes = timerBytes, ropeSleep = true, offscreenHintsAndReview = true, recycledPlayerIdentity = true,
                    environment = "Unity Editor; not a device FPS benchmark"
                }, Newtonsoft.Json.Formatting.Indented));
            }
        }
        finally { DOTween.defaultUpdateType = previous; }
    }

    private const string Folder = "Library/FruitLabEditor";
    private const string MaskPath = "Assets/UnmaskService.prefab";
    private const string MessagePath = "Assets/Prefabs/TutorialMessagePopup.prefab";
    private const string ButtonPath = "Assets/Prefabs/Buttons/BaseButton.prefab";

    public static string Bake()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before saving tutorials");
        string backup = Folder + "/TutorialBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        foreach (string path in new[] { MaskPath, MessagePath, "Assets/Scenes/Game.unity", "Assets/Prefabs/SettingsPopup.prefab" })
            File.Copy(path, backup + "/" + System.IO.Path.GetFileName(path), true);
        Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Additive);
        try
        {
            FruitLabStyle.Font = Find<GameUIView>(scene).GetUserText().font;
            var root = new GameObject("UnmaskService", typeof(RectTransform), typeof(UnmaskServiceView));
            try
            {
                var spotlight = new GameObject("Spotlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(TutorialSpotlightView)).GetComponent<TutorialSpotlightView>();
                spotlight.transform.SetParent(root.transform, false); FruitLabStyle.Stretch(spotlight.rectTransform);
                spotlight.color = new Color(0, 0, 0, .55f); spotlight.raycastTarget = true;
                Set(root.GetComponent<UnmaskServiceView>(), "spotlight", spotlight);
                Set(root.GetComponent<UnmaskServiceView>(), "unmaskCardItemPrefab", AssetDatabase.LoadAssetAtPath<UnmaskCardItemView>("Assets/Prefabs/UnmaskCardItem.prefab"));
                PrefabUtility.SaveAsPrefabAsset(root, MaskPath);
            }
            finally { Object.DestroyImmediate(root); }
            foreach (var area in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<UnmaskServiceAreaView>(true)))
                Set(area, "unmaskServicePrefab", AssetDatabase.LoadAssetAtPath<UnmaskServiceView>(MaskPath));

            root = new GameObject("TutorialMessagePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TutorialMessagePopupView));
            try
            {
                var view = root.GetComponent<TutorialMessagePopupView>();
                Image surface = FruitLabStyle.AddSurface(root, FruitLabStyle.Panel); surface.raycastTarget = true;
                FruitLabStyle.Rim((RectTransform)root.transform, .8f);
                var title = FruitLabStyle.CreateText("Title", root.transform, 21, FruitLabStyle.Background);
                title.fontStyle = FontStyles.Bold; title.alignment = TextAlignmentOptions.Midline;
                var body = FruitLabStyle.CreateText("Body", root.transform, 19, FruitLabStyle.Background);
                body.fontStyle = FontStyles.Normal;
                title.enableAutoSizing = false;
                body.alignment = TextAlignmentOptions.TopLeft; body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Overflow;
                var next = Button(root.transform, "NextButton", true); next.SetText("Tamam");
                Set(view, "rectTransform", root.transform); Set(view, "text", body); Set(view, "title", title);
                Set(view, "nextButton", next);
                view.ShowAt(PowerUpTypeUtility.Name(RewardType.Bomb), PowerUpTypeUtility.Description(RewardType.Bomb), true, TutorialMessagePlacement.Center);
                Dirty(root); PrefabUtility.SaveAsPrefabAsset(root, MessagePath);
            }
            finally { Object.DestroyImmediate(root); }
            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene), "Game scene must retain the new mask binding");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        return "Tutorial prefabs and Game scene saved; " + FruitLabSettingsEditorTools.Bake() + "; backup=" + backup;
    }

    public static string BakeCardAnnotations()
    {
        const string path = "Assets/Prefabs/CardItem.prefab";
        Directory.CreateDirectory(Folder + "/CardAnnotationBackups");
        File.Copy(path, Folder + "/CardAnnotationBackups/CardItem-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".prefab", true);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view = root.GetComponent<NormalCardItemView>();
            view.EnsureAnnotationVisuals(); view.SetAnnotation(CardAnnotation.Excluded); view.SetAnnotation(CardAnnotation.None);
            var cross = root.transform.Find("AnimationProvider/AnnotationCross");
            var oldPlate = cross.GetComponent<Image>();
            if (oldPlate != null) Object.DestroyImmediate(oldPlate);
            var oldRenderer = cross.GetComponent<CanvasRenderer>();
            if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return "Saved the card visual animation layer and inactive annotation cross in CardItem.prefab";
    }

    public static void ReviewCardAnnotations(Canvas canvas, Camera camera) => ReviewManualNotes(canvas,camera);

    public static void Review(Canvas canvas, Camera camera)
    {
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || message.Contains("error inside a tween callback")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        UpdateType update = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var flags = new Dictionary<string, int>();
        var before = Profile();
        try
        {
            ClearPreviewActors(canvas);
            using (TutorialProgress.UseTestValues(flags))
            using (var game = new Fixture(canvas, camera))
            {
                game.Bootstrap.PrepareRound();
                Require(game.Message == null && !game.Bootstrap.IsFirstActive && game.Gate.IsRestricted, "Preparing a round cannot start the tutorial before actors exist");
                flags[TutorialProgress.First] = 1; flags[TutorialProgress.LegacySkipped] = 1;
                Require(game.Tracker.IsFirstLevelTutorial(), "An old skip/completion flag cannot suppress level-one training");
                TutorialProgress.RepairIncompleteFirstLevel(0);
                Require(!TutorialProgress.Seen(TutorialProgress.First) && !TutorialProgress.Seen(TutorialProgress.LegacySkipped), "Stale level-one flags are repaired without progress reset");
                game.BuildActors(); game.Bootstrap.StartRound();
                Tick(.001f);
                Require(game.Step == 0 && game.Bootstrap.IsFirstActive, "First lesson must start after board/cards are initialized");
                game.AssertNoSkip(); game.PauseAndResume();
                Require(game.Step == 0, "Leaving before a guess restarts level-one training with the same answer");
                Capture(camera, Folder + "/preview-tutorial-placement.png");
                game.AssertHit(game.Initial.GetRectTransformOfCardItem(0), typeof(IPointerUpHandler));
                Require(!game.Placement.TryPlaceCardOnBoard(1, 0) && !game.Placement.TryPlaceCardOnBoard(0, 1), "Only the instructed fruit and slot can be used");
                game.Check(); Require(game.Round.GetTriedCardsList().Count == 0, "Premature guess is blocked");
                game.Place(0); game.Place(1); Require(game.Step == 2, "First two placements advance to submit");
                game.AssertHit(game.UI.GetCheckButtonRectTransform(), typeof(IPointerDownHandler));
                game.Check(); game.PauseAndResume(); Tick(.5f); Require(game.Step == 3, "Wrong-guess return must finish before explanation");
                game.Feedback(0, new[] { 1, 2 }, 0, 1);
                Capture(camera, Folder + "/preview-tutorial-feedback.png");
                game.Next(); game.Place(2); game.Place(0); game.Check(); Tick(.5f);
                game.PauseAndResume(); Require(game.Step == 7, "Second explanation follows the second guess"); game.Feedback(1, new[] { 3, 1 }, 1, 0);
                game.Next(); game.Place(3); game.Place(0); game.Check(); Tick(.5f);
                Require(TutorialProgress.Seen(TutorialProgress.First) && !TutorialProgress.Seen(TutorialProgress.LegacySkipped), "Win persists the first-lesson completion flag");
                Require(game.Round.GetTriedCardsList().Count == 3 && game.Round.GetRemainingGuessCount() == 4, "Three scripted guesses use exactly two wrong-guess lives");
                Require(!game.Gate.IsRestricted && game.Message == null && canvas.GetComponentInChildren<UnmaskServiceView>(true) == null, "Win must remove tutorial masks, hands and input restrictions");
                Require(!((Component)game.View.GetCardInfoButton()).gameObject.activeSelf, "Card-info tutorial stays disabled");

                Field(game.Tracker, "_levelId", 1);
                Require(!game.Tracker.IsFirstLevelTutorial(), "Later levels cannot start the first lesson");
                game.AssertSavePolicy();
                // Reopen a playable round without a scripted first lesson.
                var rewardNodes = (List<LifeBarRewardInfo>)game.Round.GetLifeBarRewardInfoList();
                rewardNodes[2] = new LifeBarRewardInfo(2, LifeBarRewardType.Crystal, true);
                game.Bootstrap.StartRound();
                Require(game.Message != null && game.Gate.IsRestricted, "The first round containing crystal hints explains them before play");
                game.Next(); Require(TutorialProgress.Seen(TutorialProgress.CrystalHints), "Crystal hint explanation is acknowledged separately");
                flags.Remove(TutorialProgress.Reveal); Field(game.Tracker, "_revealingPowerUpCount", 0);
                Require(!game.Gate.TryExplain(GameUIButtonType.RevealingPowerUp), "No-stock powerups do not open a use tutorial");
                game.Bootstrap.ShowRewards(2, 1); Require(game.Message != null, "First earned currencies are explained");
                Capture(camera, Folder + "/preview-tutorial-currency.png");
                game.Next(); Require(TutorialProgress.Seen(TutorialProgress.Currency), "Currency intro remembers acknowledgment");

                game.AssertResponsive();
                Tick(1f);
                Require(errors.Count == 0, "Unity errors: " + string.Join("; ", errors));
            }
            Require(before == Profile(), "Read-only tutorial review must preserve all actual player progress and inventory");
            File.WriteAllText(Folder + "/tutorial-review.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                passed = true, scenario = "[1,2] => 0/1; [3,1] => 1/0; [4,1] => win", checks = new[] {
                    "board readiness", "real pointer placement and guess buttons", "input/raycast restrictions", "actual result rows and life count",
                    "completion only on win", "no skip or replay controls", "old skipped flag recovery", "pause and resume with 0/1/2 guesses", "settings blocked during the first lesson", "pending animation cancellation on pause", "fixed level-one answer and legacy save policy",
                    "zero-stock tutorial guard", "first crystal-hint round",
                    "coin/crystal intro", "320/375/430 width layouts", "unchanged real PlayerPrefs", "no Unity/tween errors" },
                flags, player_preferences_written = false
            }, Newtonsoft.Json.Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = update; Application.logMessageReceived -= collect; }
        Debug.Log("FRUIT_LAB_TUTORIAL_PASS: scripted real placements/results, mandatory completion/pause resume/fixed answer, first-use tips, mobile layouts and player-data isolation.");
    }

    public static Scripts.Economy.CrystalRewardPlan MakeCrystalPlan(int previous,RewardType reward,int earned,int level)
    {
        var profile=new Scripts.Economy.PlayerProfileData();profile.Wallet.CrystalProgress=previous;profile.Wallet.NextReward=reward;profile.Progress.LevelId=level;
        Scripts.Economy.PlayerProfileRuntime.CreateRewardRules().ApplySingle(profile,0,earned,out var plan);return plan;
    }
    private static LevelMechanicsController RulesFor(Fixture game,LevelMechanicsController rules)
    {game.Results.TurnFlow.Mechanics=rules;return rules;}
    private sealed class Fixture : IDisposable
    {
        public readonly Canvas Canvas; public readonly Camera Camera;
        public readonly TutorialInputGate Gate = new TutorialInputGate();
        public readonly List<IDisposable> ExtraDisposables=new List<IDisposable>();
        public readonly LevelTracker Tracker; public readonly TargetNumberCreator Target;
        public readonly RoundStateManager Round; public readonly BoardAreaController Board;
        public readonly InitialCardAreaController Initial; public readonly CardPlacementCoordinator Placement;
        public ResultManager Results; public PowerUpMessageController PowerMessages; public RevealingPowerUpController Reveal;
        public readonly GameUIController UI; public readonly GameUIView View; public readonly TutorialBootstrapper Bootstrap;
        private readonly CardInteractionManager _interaction;
        public readonly HelperEffect Helper;
        public readonly PairEffect Pair;
        public readonly BombPowerUpController Bomb;
        public readonly PowerUpUseService Uses;
        public readonly CardLockService Locks;
        public readonly Scripts.Economy.IPlayerProfileRepository PowerProfiles;
        public readonly DiContainer PowerContainer;
        public readonly GamePowerUpAreaController _powers;
        public readonly GuessManager _guess;
        public readonly ResultAreaController _area;
        public readonly CardItemInfoManager _info;
        private readonly GameObject _objects, _events;
        private readonly BaseButtonControllerFactory _buttons;
        public readonly LevelSaveDataManager _saves;
        public readonly BoardHolderCountManager _counts;
        private readonly MemorySave _memory = new MemorySave();
        public TutorialMessagePopupView Message => Canvas.GetComponentInChildren<TutorialMessagePopupView>(true);
        public int Step => ((FirstLevelTutorialController)Get(Bootstrap, "_first"))?.Step ?? -1;
        public Fixture(Canvas canvas, Camera camera, LevelData definition = null)
        {
            Canvas = canvas; Camera = camera;
            _objects = new GameObject("TutorialTestServices"); _objects.SetActive(false); _objects.transform.SetParent(canvas.transform, false);
            _events = new GameObject("TutorialTestEvents", typeof(EventSystem)); SceneManager.MoveGameObjectToScene(_events, canvas.gameObject.scene);
            Tracker = _objects.AddComponent<LevelTracker>(); Tracker.SuppressPersistenceForTests = true; Field(Tracker, "_levelId", 0); Field(Tracker, "_gameOption", GameOption.SinglePlayer);
            foreach (string name in new[] { "_bombPowerUpCount", "_revealingPowerUpCount", "_lifePowerUpCount" }) Field(Tracker, name, 1);
            var level = new TestLevel(definition); var saves = _saves = new LevelSaveDataManager(level); saves.CreateDefaultLevelSaveData();
            Round = new RoundStateManager(level, saves); Round.Initialize();
            var counts = _counts = new BoardHolderCountManager(saves, level); var indexes = new BoardCardIndexManager(); var sizes = new SizeManager();
            var boardView = canvas.GetComponentInChildren<BoardAreaView>(true);
            var split = new BoardSplitService(level,Tracker);
            var boardLayout = new BoardLayoutManager(boardView,sizes); Field(boardLayout,"_split",split);Field(boardLayout,"_levels",level);
            Board = new BoardAreaController(boardView, boardLayout, counts, indexes, indexes); Field(Board,"_split",split);
            Target = _objects.AddComponent<TargetNumberCreator>();
            Field(Target, "_levelDataCreator", level); Field(Target, "_gameSaveService", _memory); Field(Target, "_levelTracker", Tracker); Field(Target,"_saveData",saves);
            Field(split,"_save",saves); Field(split,"_target",Target); Field(split,"_round",Round);
            var initialView = canvas.GetComponentInChildren<InitialCardAreaView>(true); var layout = new InitialCardAreaLayoutManager(initialView, sizes);
            var haptic = new HapticController();
            Initial = new InitialCardAreaController(initialView, Board, counts, level, saves, Target, layout, new InitialCardAreaFactory(initialView, haptic, layout));
            _info = new CardItemInfoManager(saves, counts);
            Locks=new CardLockService(_info,indexes,indexes);
            ExtraDisposables.Add(new CardKnowledgePresenter(_info,Initial));
            var container = new DiContainer(); container.Bind<IHapticController>().FromInstance(haptic); container.Bind<TutorialInputGate>().FromInstance(Gate);
            container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            _buttons = container.Resolve<BaseButtonControllerFactory>();
            _powers = new GamePowerUpAreaController(_buttons, canvas.GetComponentInChildren<PowerUpListHolderView>(true), Tracker);
            Field(_powers,"_levels",level);
            View = canvas.GetComponentInChildren<GameUIView>(true);
            UI = new GameUIController(_buttons, Tracker, null, haptic, View, _powers); Field(UI, "_tutorialGate", Gate);
            var fade = new FadePanelController(canvas.GetComponentInChildren<FadePanelView>(true));
            var powerMessages = PowerMessages = new PowerUpMessageController(_powers, fade, Tracker,UI); Field(powerMessages, "_tutorialGate", Gate);

            var reveal = Reveal = new RevealingPowerUpController(Board, powerMessages, _powers, haptic, Target);
            var locator = new CardItemLocator(Board, UI); locator.Initialize();
            Placement = new CardPlacementCoordinator(Board, locator, UI, indexes, indexes, reveal, Initial, _info, Gate);
            _area = new ResultAreaController(canvas.GetComponentInChildren<ResultAreaView>(true));
            var results = Results = new ResultManager(UI, _area, Target, indexes, Round, _powers,_area.History);
            Field(results,"_split",split);
            _guess = new GuessManager(new LifeBarController(canvas.GetComponentInChildren<LifeBarView>(true)), UI, Round);
            results.ConfigureTurnFlow(new SinglePlayerTurnFlow(Round,_guess,null));
            _interaction = new CardInteractionManager(UI, new BoardHolderClickRouter(Board, reveal), Initial, haptic, Placement);
            Helper=new HelperEffect(Round,Target,saves);Field(Helper,"_split",split);Field(Helper,"_history",_area.History);
            Pair=new PairEffect(Round,Target,saves,_info,Tracker);
            Bomb=new BombPowerUpController(Board,Placement,Target,_info,counts);Field(Bomb,"_pair",Pair);
            var snapshot=new GameSaveSnapshotProvider(Target,_info,new PlayingFlow(),counts,Round);
            Field(snapshot,"_saveData",saves);Field(snapshot,"_levels",level);Field(snapshot,"_tracker",Tracker);
            var profiles=PowerProfiles=new Scripts.Economy.PlayerProfileRepository(new Scripts.Economy.MemoryPlayerProfileStorage(),()=>new Scripts.Economy.PlayerProfileData());
            profiles.Changed+=()=>{
                var stock=profiles.Snapshot().Wallet.PowerUps;
                foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)Field(Tracker,PowerStockField(type),stock[type]);
            };
            PowerContainer=new DiContainer();
            PowerContainer.Bind<IGuessManager>().FromInstance(_guess);
            PowerContainer.Bind<IBombPowerUpController>().FromInstance(Bomb);
            PowerContainer.Bind<ICardPlacementCoordinator>().FromInstance(Placement);
            PowerContainer.Bind<HelperEffect>().FromInstance(Helper);PowerContainer.Bind<PairEffect>().FromInstance(Pair);
            PowerContainer.Bind<BoardSplitService>().FromInstance(split);
            PowerContainer.Bind<UltraPowerUpController>().FromMethod(_=>(UltraPowerUpController)Get(_powers,"_ultra"));
            GameMonoInstaller.BindPowerUpEffects(PowerContainer);
            Uses=new PowerUpUseService(Tracker,level,saves,Round,_info,counts,Target,new Scripts.Economy.PowerUpUseJournal(profiles),_memory,
                ()=>{
                    var countsNow=PowerUpTypeUtility.OrderedRewardTypes.ToDictionary(type=>type,Tracker.GetPowerUpCount);
                    profiles.Update(profile=>{foreach(var entry in countsNow)profile.Wallet.PowerUps[entry.Key]=entry.Value;});
                    return snapshot;
                },
                PowerContainer.Resolve<IPowerUpCatalog>(),
                ()=>new IceBoardService(level,Tracker,saves,Round));
            PowerContainer.Bind<PowerUpUseService>().FromInstance(Uses);
            Field(_powers,"_uses",PowerContainer.Resolve<LazyInject<PowerUpUseService>>());
            Field(PowerMessages,"_uses",PowerContainer.Resolve<LazyInject<PowerUpUseService>>());
            Field(Reveal,"_uses",PowerContainer.Resolve<LazyInject<PowerUpUseService>>());
            var mask = _objects.AddComponent<UnmaskServiceAreaView>(); Set(mask, "unmaskServicePrefab", AssetDatabase.LoadAssetAtPath<UnmaskServiceView>(MaskPath));
            Bootstrap = new TutorialBootstrapper(Tracker, Initial, Board, Placement, results, Round, _info, UI,
                mask, fade, Gate, _buttons);
            var creator = canvas.GetComponentInChildren<GamePopupCreator>(true); var data = new SerializedObject(creator);
            Bootstrap.Configure(creator.transform, (RectTransform)canvas.transform.Find("SafeArea"), (RectTransform)canvas.transform,
                (HandTutorialView)data.FindProperty("handTutorialPrefab").objectReferenceValue,
                (TutorialMessagePopupView)data.FindProperty("tutorialMessagePopupPrefab").objectReferenceValue);
            results.LevelSuccessEvent += (sender, args) => Bootstrap.RoundEnded(true);
            View.GetPairView()?.Configure(new PairDragPresenter(Initial, Placement, Board, indexes, Gate, UI, () => true));
        }
        public static string PowerStockField(RewardType type)=>type==RewardType.Revealing?"_revealingPowerUpCount":type==RewardType.Life?"_lifePowerUpCount":type==RewardType.Bomb?"_bombPowerUpCount":type==RewardType.Helper?"_helperPowerUpCount":type==RewardType.Pair?"_pairPowerUpCount":type==RewardType.Split?"_splitPowerUpCount":"_ultraPowerUpCount";
        public void BuildActors()
        {
            Board.CreateBoard(); Target.SetTargetNumber(_counts.GetBoardHolderCount()); _info.Initialize(); Initial.Initialize(false);
            _powers.Initialize(); UI.Initialize(false); Placement.Initialize(); _interaction.Initialize(); _area.Initialize(false); _guess.Initialize();
            Layer(Canvas.transform);
        }
        public void Place(int index)
        {
            Require(Initial.TryGetCardItem(index, out var card), "Expected fruit exists");
            card.GetCardMoveHandler().OnPointerDown(null); card.GetCardMoveHandler().OnPointerUp(null); Tick(.36f);
        }
        public void Check() => Click((BaseButtonView)View.GetCheckButton());
        public void Next() => Click((BaseButtonView)new SerializedObject(Message).FindProperty("nextButton").objectReferenceValue);
        public void Feedback(int rowIndex, int[] fruits, int correct, int misplaced)
        {
            Require(Round.GetTriedCardsList()[rowIndex].SequenceEqual(fruits), "Real guess must retain the instructed board order");
            var view = Canvas.GetComponentInChildren<ResultAreaView>(true).GetVisibleRow(rowIndex);
            Require(view != null, "Tutorial feedback row is visible");
            Require(((TMP_Text)Get(view, "_correctCount")).text == correct.ToString()
                && ((TMP_Text)Get(view, "_misplacedCount")).text == misplaced.ToString(), "Actual result feedback matches tutorial text");
        }
        public void AssertHit(RectTransform target, Type handler)
        {
            Layer(Canvas.transform); Draw(Camera);
            var pointer = new PointerEventData(_events.GetComponent<EventSystem>()) { position = RectTransformUtility.WorldToScreenPoint(Camera, target.TransformPoint(target.rect.center)) };
            var hits = new List<RaycastResult>();
            foreach(var raycaster in Canvas.GetComponentsInChildren<GraphicRaycaster>(true))
                if(raycaster.isActiveAndEnabled)raycaster.Raycast(pointer,hits);
            hits=hits.OrderByDescending(hit=>SortingLayer.GetLayerValueFromID(hit.sortingLayer))
                .ThenByDescending(hit=>hit.sortingOrder).ThenByDescending(hit=>hit.depth).ToList();
            Require(hits.Count > 0 && hits[0].gameObject.GetComponentsInParent<MonoBehaviour>().Any(component => handler.IsInstanceOfType(component)),
                "Spotlight hole must pass input to " + target.name + "; top=" + hits.FirstOrDefault().gameObject?.name);
        }
        public void AssertNoSkip()
        {
            var secondary = Message != null ? new SerializedObject(Message).FindProperty("secondaryButton").objectReferenceValue as BaseButtonView : null;
            Require(Message != null && (secondary == null || !secondary.gameObject.activeSelf),
                "The first lesson has no skip/close action");
            Require(Message.transform.Find("SkipButton") == null, "Old skip object is removed from the prefab");
        }
        public void PauseAndResume()
        {
            AssertNoSkip();
            Require(!Gate.AllowsButton(GameUIButtonType.Settings), "Settings is blocked with unrelated input during the first lesson");
            int opened=0;EventHandler open=(sender,args)=>opened++;
            UI.OpenSettings+=open;
            try { Click((BaseButtonView)View.GetSettingsButton());Require(opened==0,"Settings cannot bypass the first lesson"); }
            finally { UI.OpenSettings-=open; }
            var provider = new GameSaveSnapshotProvider(Target, _info, new PlayingFlow(), _counts, Round);
            if (provider.TryCreateSnapshot(out var snapshot)) _memory.Save(snapshot); else _memory.DeleteSave();
            int guesses = Round.GetTriedCardsList().Count;
            Require(!TutorialProgress.Seen(TutorialProgress.First), "Pausing cannot mark training completed");
            Require(guesses == 0 ? !_memory.HasSavedGame() : FirstLevelTutorialController.CanResume(_memory.Data, 6), "Actual pause snapshot is safe to resume");
            Bootstrap.Cancel(); UI.TriggerResetNumbers(); Tick(.5f);
            Require(Message == null && !Gate.IsRestricted && !TutorialProgress.Seen(TutorialProgress.First), "Pausing cancels pending callbacks without completing training");
            if (_memory.HasSavedGame()) _saves.SetLevelSaveDataAsSaved(_memory.Data); else _saves.CreateDefaultLevelSaveData();
            Round.Initialize(); _info.Initialize(); Target.SetTargetNumber(2);
            Bootstrap.PrepareRound(); Bootstrap.StartRound(); Tick(.001f);
            Require(Bootstrap.IsFirstActive && Step == (guesses == 0 ? 0 : guesses == 1 ? 3 : 7)
                && Target.GetTargetCardsList().SequenceEqual(new[] { 4, 1 }), "Resuming level one restores the guided lesson with portakal/elma");
        }
        public void AssertSavePolicy()
        {
            var save = new LevelSaveData { TargetCards = new List<int> { 4, 1 }, TriedCardsList = new List<List<int>>(),
                CardItemInfoList = Enumerable.Range(0, 5).Select(_ => new CardItemInfo { isExisted = true }).ToList(), RemainingGuessCount = 6 };
            Require(FirstLevelTutorialController.CanResume(save, 6), "A clean fixed-answer first level can resume");
            save.TargetCards = new List<int> { 2, 3 }; Require(!FirstLevelTutorialController.CanResume(save, 6), "Legacy random first-level answers restart the lesson");
            _memory.Data = save; Field(Tracker, "_levelId", 0); Target.SetTargetNumber(2); Target.SetTargetNumber(2);
            Require(Target.GetTargetCardsList().SequenceEqual(new[] { 4, 1 }), "Level-one target overrides legacy saves and remains fixed on retry");
            save.TargetCards = new List<int> { 4, 1 }; save.TriedCardsList.Add(new List<int> { 1, 3 }); save.RemainingGuessCount = 5;
            Require(!FirstLevelTutorialController.CanResume(save, 6), "Unscripted old guesses restart training");
            save.TriedCardsList[0] = new List<int> { 1, 2 }; Require(FirstLevelTutorialController.CanResume(save, 6), "First scripted guess can resume");
            save.RemovedBoardHolderCount = 1; Require(!FirstLevelTutorialController.CanResume(save, 6), "Old bomb saves cannot shorten the mandatory two-slot lesson");
            Field(Tracker, "_levelId", 1); _memory.DeleteSave();
        }
        public void AssertResponsive()
        {
            var rect = (RectTransform)Canvas.transform;
            foreach (float width in new[] { 320f, 375f, 430f })
            foreach (float height in new[] { 568f, 812f })
            {
                rect.sizeDelta = new Vector2(width, height); Canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                foreach (bool actions in new[] { false, true })
                {
                    var message = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TutorialMessagePopupView>(MessagePath), Canvas.GetComponentInChildren<GamePopupCreator>(true).transform, false);
                    try
                    {
                        message.Init(); message.Bind(_buttons, () => { }); message.Show("Meyvelerin yerini bul", "Elmayı soldaki alana yerleştir. Sürükleyebilir veya meyveye dokunabilirsin.", actions);
                        Layer(message.transform); var panel = (RectTransform)message.transform;
                        Require(panel.rect.width <= width - 23f && panel.rect.height + 100f <= height, "Lesson fits small and tall phones"); Draw(Camera);
                        foreach (TMP_Text label in message.GetComponentsInChildren<TMP_Text>(true))
                            if (label.gameObject.activeInHierarchy) { label.ForceMeshUpdate(); Require(!label.isTextOverflowing, "Tutorial text fits: " + label.name); }
                        if (width == 320 && height == 568 && !actions) Capture(Camera, Folder + "/preview-tutorial-compact.png");
                    }
                    finally { Object.DestroyImmediate(message.gameObject); }
                }
            }
            rect.sizeDelta = new Vector2(375f, 812f); Canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
        }
        public void Dispose()
        {
            View.GetPairView()?.Unbind(); _guess.Dispose();_area.Dispose();
            foreach(var extra in ExtraDisposables)extra.Dispose();
            Bootstrap.Dispose(); _interaction.Unsubscribe(); Tick(2f);
            Object.DestroyImmediate(_events); Object.DestroyImmediate(_objects);
        }
    }

    public static string ResetLocalProgress()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before the requested profile reset");
        var profiles=Scripts.Economy.PlayerProfileRuntime.Current;
        var profileBefore=profiles.Snapshot();
        string[] ints = new[] { "level_id", "star_count", "gift_star_count", "revealing_power_up_count", "life_power_up_count", "bomb_power_up_count", "helper_power_up_count", "pair_power_up_count", "split_power_up_count", "reward_type" }.Concat(TutorialProgress.Keys).ToArray();
        string[] strings = { "saved_game_key", "star_count_of_levels" };
        string backup = Folder + "/PlayerPrefsBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json";
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));
        File.WriteAllText(backup, Newtonsoft.Json.JsonConvert.SerializeObject(new {
            company = PlayerSettings.companyName, product = PlayerSettings.productName,
            profile = profileBefore,
            ints = ints.ToDictionary(key => key, key => new { exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) }),
            strings = strings.ToDictionary(key => key, key => new { exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetString(key) })
        }, Newtonsoft.Json.Formatting.Indented));
        int coins = profileBefore.Wallet.Coins, crystals = profileBefore.Wallet.CrystalProgress;
        profiles.Update(profile=>{
            profile.Progress.LevelId=0;
            foreach(var type in new[]{RewardType.Revealing,RewardType.Life,RewardType.Bomb})
                profile.Wallet.PowerUps[type]=Mathf.Max(1,profile.Wallet.PowerUps[type]);
        });
        Scripts.Economy.PlayerProfileRuntime.MirrorLegacy(profiles.Snapshot());
        PlayerPrefs.SetInt("level_id", 0); foreach (string key in strings) PlayerPrefs.DeleteKey(key);
        foreach (string key in TutorialProgress.Keys) PlayerPrefs.DeleteKey(key);
        foreach (string key in new[] { "revealing_power_up_count", "life_power_up_count", "bomb_power_up_count" })
            PlayerPrefs.SetInt(key, Mathf.Max(1, PlayerPrefs.GetInt(key)));
        PlayerPrefs.Save();
        foreach (LevelTracker tracker in Resources.FindObjectsOfTypeAll<LevelTracker>())
        {
            if (EditorUtility.IsPersistent(tracker)) continue;
            Field(tracker, "_levelId", 0); Field(tracker, "_starCount", coins); Field(tracker, "_giftStarCount", crystals);
            Field(tracker, "_revealingPowerUpCount", PlayerPrefs.GetInt("revealing_power_up_count"));
            Field(tracker, "_lifePowerUpCount", PlayerPrefs.GetInt("life_power_up_count")); Field(tracker, "_bombPowerUpCount", PlayerPrefs.GetInt("bomb_power_up_count"));
        }
        Require(PlayerPrefs.GetInt("star_count") == coins && PlayerPrefs.GetInt("gift_star_count") == crystals, "Balances must be preserved");
        string report = Newtonsoft.Json.JsonConvert.SerializeObject(new { level = 1, coins, crystals,
            bomb = PlayerPrefs.GetInt("bomb_power_up_count"), reveal = PlayerPrefs.GetInt("revealing_power_up_count"), life = PlayerPrefs.GetInt("life_power_up_count"),
            tutorial_flags_cleared = TutorialProgress.Keys.All(key => !PlayerPrefs.HasKey(key)), backup }, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(Folder + "/tutorial-reset.json", report); return report;
    }

    private sealed class TestLevel : ILevelDataCreator
    {
        private readonly LevelData _definition;
        public TestLevel(LevelData definition = null) => _definition = definition ?? new LevelData { NumOfBoardHolders = 2, NumOfCards = 5, MaxNumOfTries = 6 };
        public LevelData GetLevelData() => _definition;
        public void SetSinglePlayerLevelData() { } public void SetMultiplayerLevelData() { }
    }
    private sealed class MemorySave : IGameSaveService
    {
        public LevelSaveData Data;
        public LevelSaveData GetSavedLevel() => Data; public bool HasSavedGame() => Data != null;
        public void DeleteSave() => Data = null;
        public void Save(LevelSaveData data) => Data = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelSaveData>(Newtonsoft.Json.JsonConvert.SerializeObject(data));
    }
    private sealed class PlayingFlow : ILevelSession
    {
        public void Tick(){}
        public void Begin(LevelEntryMode mode) { }
        public bool IsGameOver => false;
        public LevelFlowState State => LevelFlowState.Playing;
        public void Dispose() { }
    }
    private static void ClearPreviewActors(Canvas canvas)
    {
        foreach (var row in canvas.GetComponentsInChildren<ResultBlockView>(true)) Object.DestroyImmediate(row.gameObject);
        foreach (var holder in canvas.GetComponentsInChildren<InitialHolderView>(true)) Object.DestroyImmediate(holder.gameObject);
        foreach (var holder in canvas.GetComponentsInChildren<BoardHolderView>(true)) Object.DestroyImmediate(holder.gameObject);
        foreach (var boundary in canvas.GetComponentInChildren<LifeBarView>(true).GetComponentsInChildren<BoundaryView>(true)) Object.DestroyImmediate(boundary.gameObject);
        var fade = canvas.GetComponentInChildren<FadePanelView>(true); fade.transform.gameObject.SetActive(true);
        fade.GetFadeImage().gameObject.SetActive(false); fade.GetTutorialFadeImage().gameObject.SetActive(false); fade.GetBoardFadeImage().gameObject.SetActive(false);
        canvas.GetComponentInChildren<LevelFinishPopupView>(true).SetStatus(false);
    }
    private static BaseButtonView Button(Transform parent, string name, bool primary)
    {
        var view = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath), parent)).GetComponent<BaseButtonView>();
        view.name = name; ButtonAppearance.For(view).SetActionStyle(primary); return view;
    }
    private static void Click(BaseButtonView button) { button.OnPointerDown(null); button.OnPointerUp(null); button.OnPointerClick(null); }
    private static void Tick(float seconds) => DOTween.ManualUpdate(seconds, seconds);
    private static void Layer(Transform root) { foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30; }
    private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Field(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).First();
    private static void Set(Object target, string name, Object value) { var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
    private static void Dirty(GameObject root) { foreach (Component component in root.GetComponentsInChildren<Component>(true)) { if (component == null) continue; EditorUtility.SetDirty(component); if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component); } }
    private static string Profile() => string.Join("|", new[] { "level_id", "star_count", "gift_star_count", "revealing_power_up_count", "life_power_up_count", "bomb_power_up_count", "helper_power_up_count", "pair_power_up_count", "split_power_up_count", "reward_type" }.Concat(TutorialProgress.Keys).Select(key => key + ":" + PlayerPrefs.HasKey(key) + ":" + PlayerPrefs.GetInt(key))) + PlayerPrefs.GetString("saved_game_key") + PlayerPrefs.GetString("star_count_of_levels");
    private static void Draw(Camera camera)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var frame in camera.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UnmaskCardItemView>(true)))
            typeof(UnmaskCardItemView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(frame, null);
        foreach (var mask in camera.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UnmaskServiceView>(true)))
            typeof(UnmaskServiceView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mask, null);
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = camera.targetTexture });
    }
    private static void Capture(Camera camera, string path)
    {
        Draw(camera); RenderTexture previous = RenderTexture.active; RenderTexture.active = camera.targetTexture;
        var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
        try { image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
        finally { Object.DestroyImmediate(image); RenderTexture.active = previous; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Tutorial review: " + message); }
}

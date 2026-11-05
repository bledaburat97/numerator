using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Game;
using Newtonsoft.Json;
using Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeSplitViews()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Game.unity"); bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        try
        {
            var view = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BoardAreaView>(true)).Single();
            view.PrepareSplitVisuals();
            foreach (var component in view.GetComponentsInChildren<Component>(true)) EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "Saved two editable split-group panels in Game.unity";
        }
        finally { if (close) EditorSceneManager.CloseScene(scene,true); }
    }

    public static void ReviewSplit(Canvas canvas,Camera camera)
    {
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>(); string profile = Profile(); int layouts = 0, evaluations = 0;
        Application.LogCallback collect = (message,stack,type) => {
            if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        try
        {
            ValidateMechanicBindings();
            foreach (int count in new[] { 4,5 })
            {
                var partition = BoardPartition.ForCount(count); var answer = Enumerable.Range(1,count).ToArray();
                foreach (var guess in PermuteSplit(answer))
                {
                    var score = new GuessEvaluation(1,guess,answer,partition);
                    foreach (var group in score.Groups)
                    {
                        var actual = guess.Skip(group.Start).Take(group.Count).ToArray();
                        var expected = answer.Skip(group.Start).Take(group.Count).ToArray();
                        int correct = actual.Where((fruit,index) => fruit == expected[index]).Count();
                        Require(group.Correct == correct && group.Misplaced == actual.Count(expected.Contains)-correct,"Group-local scoring for every permutation");
                    }
                    Require(score.Won == guess.SequenceEqual(answer),"Both groups must be correct to win"); evaluations++;
                }
                var crossed = new GuessEvaluation(1,new[] {3,4,1,2},new[] {1,2,3,4},BoardPartition.ForCount(4));
                Require(crossed.Misplaced == 4 && crossed.Groups.All(group => group.Correct == 0 && group.Misplaced == 0),"A fruit from the other group is not a local misplaced match");
                Require(crossed.States.All(state => state == CardResultState.Absent),"Detailed hints use the same group membership as the counters");
                var duplicates = new GuessEvaluation(1,new[] {1,1,2,2},new[] {1,2,1,2},BoardPartition.ForCount(4));
                Require(duplicates.Groups.All(group => group.Correct == 1 && group.Misplaced == 0),"Group evaluator remains duplicate-safe");

                var definition = new LevelData { NumOfBoardHolders = count,NumOfCards = count+4,MaxNumOfTries = 10,Mechanic = LevelMechanic.Split };
                LevelSaveData snapshot;
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,definition))
                {
                    Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear(); game.Target.RevealMultiplayerAnswer(answer);
                    var helper = new HelperEffect(game.Round,game.Target,game._saves);
                    Field(helper,"_split",Get(game.Results,"_split"));
                    Field(game._powers,"_helper",helper); Field(game.PowerMessages,"_helper",helper);
                    using (var rules = new LevelMechanicsController(game.Results,helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker))
                    {
                        rules.Attach(new LevelMechanicsPresenter(game.View, game.Board),null); rules.Begin(LevelMechanic.Split);
                        var partial = answer.ToArray(); (partial[count-2],partial[count-1]) = (partial[count-1],partial[count-2]);
                        int wins = 0; game.Results.LevelSuccessEvent += (sender,args) => wins++;
                        SubmitSplit(game,partial); Require(wins == 0 && game.Round.GetRemainingGuessCount() == 9,"One solved group neither wins nor spends an extra life");
                        for (int row = 1; row < 6; row++) SubmitSplit(game,answer.Skip(row%(count-1)+1).Concat(answer.Take(row%(count-1)+1)).ToArray());
                        Require(game.Round.GetTriedCardsList().Count == 6 && game.Round.GetRemainingGuessCount() == 4,"Six submitted guesses cost six lives");
                        var area = canvas.GetComponentInChildren<ResultAreaView>(true);
                        var scroll = (ScrollRect)new SerializedObject(area).FindProperty("scrollRect").objectReferenceValue;
                        foreach (float width in new[] { 320f,375f,430f })
                        foreach (float height in new[] { 568f,667f,812f })
                        {
                            ((RectTransform)canvas.transform).sizeDelta = new Vector2(width,height);
                            canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); game.Board.RefreshLayout(); Canvas.ForceUpdateCanvases();
                            var spacing = scroll.content.GetComponent<VerticalLayoutGroup>();
                            Require(area.GetRowHeight()*6+spacing.spacing*5+spacing.padding.top+spacing.padding.bottom <= scroll.viewport.rect.height+.1f,"Six complete split rows fit");
                            foreach (var row in area.GetComponentsInChildren<ResultBlockView>()) CheckSplitRow(row);
                            CheckSplitBoard(canvas); layouts++;
                        }
                        ((RectTransform)canvas.transform).sizeDelta = new Vector2(375,812);
                        canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); game.Board.RefreshLayout(); Canvas.ForceUpdateCanvases();
                        game._area.EnableReview(); game.AssertHit(scroll.viewport,typeof(IBeginDragHandler));
                        Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-split-"+count+".png");
                        Field(game.Tracker,"_helperPowerUpCount",1); game._powers.Refresh();
                        Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Helper));
                        Require(game._saves.GetLevelSaveData().RevealedGuessNumbers.SequenceEqual(new[] {6}),"Helper remains usable on a split board");
                        var provider = new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
                        Field(provider,"_saveData",game._saves); Field(provider,"_levels",new TestLevel(definition)); Field(provider,"_tracker",game.Tracker);
                        Require(provider.TryCreateSnapshot(out snapshot),"Split round can be saved");
                        SubmitSplit(game,answer);
                        Require(wins == 1 && game.Round.GetRemainingGuessCount() == 4,"Full answer wins without spending another life");
                        game._area.RevealAnswer(answer);
                        Require(area.GetComponentsInChildren<ResultBlockView>().All(row => row.IsSplit),"Completion keeps both feedback groups visible");
                        Require(!area.GetComponentsInChildren<FruitLabGlyph>().Any(glyph => glyph.name == "HelperAbsent"),
                            "Full-answer review removes group-local crosses for fruits present elsewhere in the answer");
                        var split = new BoardSplitService(new TestLevel(definition),game.Tracker);
                        Field(game.Tracker,"_gameOption",GameOption.MultiPlayer);
                        Require(split.PartitionFor(count) == null,"Multiplayer remains unpartitioned");
                        Field(game.Tracker,"_gameOption",GameOption.SinglePlayer);
                    }
                }
                var loaded = JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(snapshot));
                Require(loaded.Definition.Mechanic == LevelMechanic.Split && loaded.TriedCardsList.Count == 6,"Save retains the rule and raw guesses");
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,definition))
                {
                    Field(game.Tracker,"_levelId",20); game._saves.SetLevelSaveDataAsSaved(loaded); game.Round.Initialize(); game.BuildActors();
                    game.Target.RevealMultiplayerAnswer(loaded.TargetCards.ToArray()); game.Results.TryAddTriedCards();
                    var rows = canvas.GetComponentInChildren<ResultAreaView>(true).GetComponentsInChildren<ResultBlockView>();
                    Require(rows.Length == 6 && rows.All(row => row.IsSplit) && game.Round.GetRemainingGuessCount() == 4,"Resume rebuilds every split result without spending lives");
                    var groups = (IReadOnlyList<GuessGroupFeedback>)Get(rows[0],"_groups");
                    Require(groups[0].Correct == 2 && groups[1].Correct == count-4,"Resumed local counts match the original submitted guess");
                    var helper = new HelperEffect(game.Round,game.Target,game._saves); Field(helper,"_split",Get(game.Results,"_split"));
                    using (var rules = new LevelMechanicsController(game.Results,helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker))
                    {
                        rules.Attach(new LevelMechanicsPresenter(game.View, game.Board),null); rules.Begin(LevelMechanic.Split);
                        var details = helper.EvaluateGuess(6);
                        Require(rows[5].GetComponentsInChildren<FruitLabGlyph>().Count(glyph => glyph.name == "HelperAbsent")
                            == details.States.Count(state => state == CardResultState.Absent),"Resume restores group-aware Helper details");
                    }
                }
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,definition))
                {
                    Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear(); game.Target.RevealMultiplayerAnswer(answer);
                    new BombPowerUpController(game.PowerMessages,game.Board,game.Placement,game.Target,game._info,game._counts,game.Round);
                    game._powers.Refresh(); Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Bomb)); Tick(1.2f);
                    Require(game.Target.GetTargetCardsList().Count == count-1,"Bomb removes the final split slot");
                    int wins = 0; game.Results.LevelSuccessEvent += (sender,args) => wins++;
                    SubmitSplit(game,answer.Take(count-1).ToArray());
                    var row = canvas.GetComponentInChildren<ResultAreaView>(true).GetComponentInChildren<ResultBlockView>();
                    Require(wins == 1 && row.IsSplit,"Bomb leaves a playable 2+1 or 2+2 partition"); CheckSplitRow(row);
                }
            }
            Require(Profile() == profile && errors.Count == 0,"No player-data changes or runtime/tween errors: "+string.Join("; ",errors));
            File.WriteAllText(Folder+"/split-review.json",JsonConvert.SerializeObject(new { passed = true,evaluations,layouts,
                checks = new[] { "shared range evaluator", "foreign-group exclusion", "duplicate safety", "one life per whole guess", "both groups required to win", "six full history rows", "matching board/row groups", "readable counters", "history scrolling", "save/resume counts", "Helper compatibility", "Bomb 2+1 and 2+2", "multiplayer isolation", "unchanged PlayerPrefs" }
            },Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = previous; Application.logMessageReceived -= collect; }
    }

    private static void SubmitSplit(Fixture game,int[] cards)
    {
        for (int index = 0; index < cards.Length; index++) Require(game.Placement.TryPlaceCardOnBoard(cards[index]-1,index),"Split guess placement");
        game.Check(); Tick(.4f);
    }
    private static void CheckSplitRow(ResultBlockView row)
    {
        Require(row.IsSplit,"Split row mode");
        var rect = row.GetRectTransform();
        var cards = row.GetComponentsInChildren<NonDraggableCardItemView>();
        foreach (var card in cards)
        {
            var cardRect = (RectTransform)card.transform;
            Require(cardRect.rect.width >= 23f,"Fruit cards remain legible on small screens: "+cardRect.rect.size+"; row="+rect.rect.size+"; canvas="+((RectTransform)row.GetComponentInParent<Canvas>().transform).rect.size);
            var point = rect.InverseTransformPoint(cardRect.position);
            Require(rect.rect.Contains(point),"Every split fruit stays in its row");
        }
        foreach (var label in row.GetComponentsInChildren<TMP_Text>())
        {
            Require(label.fontSize >= 12f,"Split counters remain readable; font="+label.fontSize+"; row="+rect.rect.height);
            label.ForceMeshUpdate(); Require(!label.isTextOverflowing,"Split counter does not clip: "+label.name+" font="+label.fontSize+" rect="+label.rectTransform.rect+" preferred="+label.preferredHeight);
        }
        var a = (RectTransform)row.transform.Find("SplitGroupA"); var b = (RectTransform)row.transform.Find("SplitGroupB");
        Require(a != null && b != null && a.position.x < b.position.x,"Both result groups are distinct and ordered");
    }
    private static void CheckSplitBoard(Canvas canvas)
    {
        var board = canvas.GetComponentInChildren<BoardAreaView>(true);
        Require(board.transform.Find("SplitGroupA").gameObject.activeSelf && board.transform.Find("SplitGroupB").gameObject.activeSelf,"Both board groups are visible");
        Require(board.GetComponentsInChildren<Image>().Where(image => image.name.StartsWith("SplitGroup")).All(image => !image.raycastTarget),"Group decoration never steals board input");
    }
    private static IEnumerable<int[]> PermuteSplit(int[] values)
    {
        if (values.Length == 0) { yield return Array.Empty<int>(); yield break; }
        for (int i = 0; i < values.Length; i++)
            foreach (var tail in PermuteSplit(values.Where((value,index) => index != i).ToArray()))
                yield return new[] {values[i]}.Concat(tail).ToArray();
    }
}

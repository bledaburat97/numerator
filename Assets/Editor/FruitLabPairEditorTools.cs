using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Game;
using Newtonsoft.Json;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    private const string PairPath = "Assets/Prefabs/UI/PairClue.prefab";

    public static string BakePairViews()
    {
        BakeMechanicViews();
        return BakePairRopeViews();
    }

    public static void ReviewPair(Canvas canvas, Camera camera)
    {
        var oldUpdate = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        int cases = 0, layouts = 0;
        try
        {
            ValidateMechanicBindings();
            Require(LevelDataGetter.GetStartingLevelsData().All(l => l.Mechanic == LevelMechanic.Normal)
                && LevelDataGetter.GetLoopLevelsData().All(l => l.Mechanic == LevelMechanic.Normal), "No permanent special-level distribution during playtest");
            Require(!PowerUpTypeUtility.IsUnlocked(RewardType.Pair, 3) && PowerUpTypeUtility.IsUnlocked(RewardType.Pair, 4), "All playtest powers unlock at display level 5");
            Require(PowerUpTypeUtility.MultiplayerRewardTypes.Length == 3, "Network reward pool remains compatible");
            foreach (int slots in new[] {3,4,5})
            {
                ClearPreviewActors(canvas);
                var definition = new LevelData { NumOfBoardHolders = slots, NumOfCards = 9, MaxNumOfTries = 10, Mechanic = LevelMechanic.Pair };
                using (var game = new Fixture(canvas, camera, definition))
                {
                    Field(game.Tracker, "_levelId", 45); Field(game.Tracker, "_pairPowerUpCount", 2);
                    game.BuildActors(); game.Gate.Clear();
                    var answer = new[] {4,1,3,5,2}.Take(slots).ToArray(); game.Target.RevealMultiplayerAnswer(answer);
                    var pair = new PairEffect(game.Round, game.Target, game._saves, game._info, game.Tracker);
                    Field(game._powers, "_pair", pair); Field(game.PowerMessages, "_pair", pair);
                    var helper = new HelperEffect(game.Round, game.Target, game._saves);
                    var eliminate = new EliminateFruitEffect(game.Target, game._info, game.Placement, game._saves);
                    var snapshot = new GameSaveSnapshotProvider(game.Target, game._info, new PlayingFlow(), game._counts, game.Round);
                    Field(snapshot, "_saveData", game._saves); Field(snapshot, "_tracker", game.Tracker); Field(snapshot, "_levels", new TestLevel(definition));
                    var memory = new MemorySave();
                    using (var rules = new LevelMechanicsController(game.Results, helper, game.Round, game._saves,
                        game.Target, game._area, game._powers, game.Tracker, pair))
                    {
                        rules.Attach(game.View, () => { if (snapshot.TryCreateSnapshot(out var data)) memory.Save(data); });
                        rules.Begin(LevelMechanic.Pair); Tick(.3f);
                        Require(pair.Current != null && pair.Current.Matches(answer) && game.View.HasPairClue, "Automatic pair is adjacent, unordered and visible");
                        Require(game.Tracker.GetPowerUpCount(RewardType.Pair) == 2 && !pair.CanApply, "Level clue is free and cannot be bought twice");
                        Require(game._info.GetCardItemInfoList().All(card => !card.isLocked) && game.Placement.GetCardsOnBoard().Count == 0, "Pair does not place or lock any card");
                        Require(snapshot.TryCreateSnapshot(out var automatic) && automatic.PairWasApplied, "Pair-only progress saves before the first guess");
                        string json = JsonConvert.SerializeObject(automatic);
                        var loaded = JsonConvert.DeserializeObject<LevelSaveData>(json);
                        var first = pair.Current;
                        game._saves.SetLevelSaveDataAsSaved(loaded); pair.Prepare(true);
                        Require(pair.Current.First == first.First && pair.Current.Second == first.Second, "Resume never rerolls an earned pair");
                        for (int n = 0; n < 3; n++)
                        {
                            for (int card = 0; card < slots; card++) game.Place(card);
                            game.Check(); Tick(.6f);
                        }
                        Require(pair.Current.First == first.First && pair.Current.Second == first.Second
                            && game._info.GetCardItemInfoList().All(card => card.isExisted)
                            && game._saves.GetLevelSaveData().RevealedGuessNumbers.Count == 0, "Pair level never executes periodic Helper/elimination rules");
                        // Retry state is new; the normal level must allow one manual application.
                        game._saves.CreateDefaultLevelSaveData(); game.Round.Initialize(); game._info.Initialize();
                        rules.Begin(LevelMechanic.Normal); game._powers.SetButtonsClickable(true);
                        Require(pair.Current == null && !game.View.HasPairClue && pair.CanApply, "Retry clears the prior relationship");
                        Require(!pair.TryApply(() => false) && pair.Current == null && !game._saves.GetLevelSaveData().PairWasApplied, "Rejected inventory transaction leaves no clue or used flag");
                        var button = (BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Pair);
                        Click(button); Tick(.3f);
                        Require(game.Tracker.GetPowerUpCount(RewardType.Pair) == 1 && pair.Current.Matches(answer) && game.View.HasPairClue, "Manual button uses the same effect and consumes exactly one");
                        Click(button);
                        Require(game.Tracker.GetPowerUpCount(RewardType.Pair) == 1 && !pair.TryApply(() => throw new Exception("Cannot consume twice")), "Repeated use is rejected before inventory consumption");
                        Require(memory.Data.PairWasApplied && memory.Data.PairClue.Matches(answer), "Manual application checkpoints the relationship");

                        if (slots == 3)
                        {
                            game._saves.GetLevelSaveData().PairClue = new PairClue(answer[1], answer[2]);
                            var bomb = new BombPowerUpController(game.PowerMessages, game.Board, game.Placement, game.Target, game._info, game._counts, game.Round);
                            Field(bomb, "_pair", pair);
                            Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetBombPowerUpButton()); Tick(.3f);
                            Require(game.Target.GetTargetCardsList().Count == 2 && pair.Current.Matches(game.Target.GetTargetCardsList()), "Bomb repairs a pair containing the removed last fruit");
                            Require(!pair.CanApply && game.Tracker.GetPowerUpCount(RewardType.Pair) == 1, "Two-slot board cannot consume another Pair");
                        }
                        else
                        {
                            // Real layout/raycast checks with six visible rows and the relationship open.
                            var rect = (RectTransform)canvas.transform;
                            var scroll = (ScrollRect)new SerializedObject(canvas.GetComponentInChildren<ResultAreaView>(true)).FindProperty("scrollRect").objectReferenceValue;
                            foreach (bool caption in new[] {false,true})
                            foreach (float width in new[] {320f,375f,430f})
                            foreach (float height in new[] {568f,667f,812f})
                            {
                                game.View.SetMechanicStatus(caption ? LevelMechanicRules.Description(LevelMechanic.Pair) : "");
                                rect.sizeDelta = new Vector2(width, height); canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); Canvas.ForceUpdateCanvases();
                                var view = canvas.GetComponentInChildren<ResultAreaView>(true);
                                var spacing = scroll.content.GetComponent<VerticalLayoutGroup>();
                                float required = view.GetRowHeight() * 6 + spacing.spacing * 5 + spacing.padding.top + spacing.padding.bottom;
                                Require(required <= scroll.viewport.rect.height + .1f, "At least six full guesses fit with the Pair clue");
                                var clue = canvas.GetComponentInChildren<PairClueView>(true);
                                clue.RefreshRope(1f / 60f);
                                Require(clue.GetComponentsInChildren<TMPro.TMP_Text>(true).Length == 0, "Pair uses existing cards without an extra caption area");
                                foreach (var label in clue.GetComponentsInChildren<TMPro.TMP_Text>()) { label.ForceMeshUpdate(); Require(!label.isTextOverflowing, "Pair caption fits"); }
                                layouts++;
                            }
                            game.View.SetMechanicStatus("");
                            rect.sizeDelta = new Vector2(375,812); canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                            game._area.EnableReview(); Layer(canvas.transform); game.AssertHit(scroll.viewport, typeof(UnityEngine.EventSystems.IScrollHandler));
                            for (int n = 0; n < 6; n++) game._area.AddResultBlock(new ResultBlockModel { finalNumbers = Enumerable.Range(1,slots).ToList(), correctPosCount = 1, wrongPosCount = 2 });
                            Layer(canvas.transform); Draw(camera); Capture(camera, Folder + "/preview-pair-" + slots + ".png");
                        }
                        game._saves.GetLevelSaveData().PairClue = null; game._saves.GetLevelSaveData().PairWasApplied = false;
                        game.Round.Initialize();
                        game.Target.RevealMultiplayerAnswer(new[] {4,1});
                        Require(pair.CanApply, "Unordered Pair remains meaningful on a fresh two-slot board");
                        game.Target.RevealMultiplayerAnswer(answer);
                        foreach (int fruit in answer) game._info.GetCardItemInfoList()[fruit - 1].isLocked = true;
                        Require(!pair.CanApply, "Already fully revealed answer cannot consume a Pair");
                        foreach (int fruit in answer) game._info.GetCardItemInfoList()[fruit - 1].isLocked = false;
                        game.Round.AddTriedCards(answer.ToList()); Require(!pair.CanApply, "Winning guess blocks subsequent use");
                        game.Round.Initialize();
                        while (game.Round.GetRemainingGuessCount() > 0) game.Round.DecreaseRemainingGuessCount();
                        Require(!pair.TryApply(() => throw new Exception("Cannot consume after fail")), "No Pair after final failure");
                        game.Tracker.SetGameOption(GameOption.MultiPlayer);
                        Require(!pair.CanApply && !pair.TryApply(), "Pair cannot be used in multiplayer");
                        game.View.SetOpponentInfoStatus(true); Require(!game.View.HasPairClue, "Multiplayer hides the clue row");
                    }
                    cases++;
                }
            }
            ClearPreviewActors(canvas);
            using (TutorialProgress.UseTestValues(new Dictionary<string,int>()))
            using (var game = new Fixture(canvas, camera, new LevelData { NumOfBoardHolders = 3, NumOfCards = 7, MaxNumOfTries = 8, Mechanic = LevelMechanic.Pair }))
            {
                Field(game.Tracker, "_levelId", 37); Field(game.Tracker, "_pairPowerUpCount", 1);
                game.BuildActors();
                var pair = new PairEffect(game.Round, game.Target, game._saves, game._info, game.Tracker);
                Require(pair.Prepare(true), "First encounter has an actual pair clue"); game.View.SetPairClue(pair.Current);
                Field(game.Bootstrap, "_levels", new TestLevel(new LevelData { Mechanic = LevelMechanic.Pair }));
                game.Bootstrap.StartRound(); Require(game.Message != null, "Pair level has a contextual explanation");
                game.Next(); Require(TutorialProgress.Seen(TutorialProgress.PairLevel), "Pair-level explanation is acknowledged once");
                Field(game.Tracker, "_levelId", 45);
                Require(game.Gate.TryExplain(GameUIButtonType.PairPowerUp, () => { }), "Pair inventory use has a separate explanation");
                game.Next(); Require(TutorialProgress.Seen(TutorialProgress.Pair), "Pair-powerup explanation is acknowledged once");
                Layer(canvas.transform); Draw(camera); Capture(camera, Folder + "/preview-pair-level.png");
                game.View.SetPairClue(null);
            }
            Require(errors.Count == 0, "No scene/tween errors: " + string.Join("; ", errors));
            File.WriteAllText(Folder + "/pair-review.json", JsonConvert.SerializeObject(new { passed = true, cases, layouts,
                checks = new[] { "real scene DI", "unordered adjacency", "shared level/powerup", "one inventory consumption", "no automatic placement", "save before first guess", "same pair on resume", "retry reset", "bomb reconciliation", "six visible guesses", "history input", "multiplayer isolation", "unchanged real PlayerPrefs" }
            }, Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = oldUpdate; Application.logMessageReceived -= collect; }
        Debug.Log("PAIR_PASS: shared effect, inventory, resume/retry, Bomb and mobile layouts");
    }
}

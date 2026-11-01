using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Game;
using Newtonsoft.Json;
using Scripts;
using UnityEngine;

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewPlaytest(Canvas canvas, Camera camera)
    {
        var oldUpdate = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        int stages = 0;
        var expectedMechanics = new[] { LevelMechanic.Normal, LevelMechanic.Helper, LevelMechanic.Pair,
            LevelMechanic.Normal, LevelMechanic.Normal, LevelMechanic.Normal, LevelMechanic.Normal, LevelMechanic.Normal,
            LevelMechanic.Split,LevelMechanic.Split,LevelMechanic.Normal,LevelMechanic.Growing,LevelMechanic.Dynamite };
        var expectedPowers = new RewardType?[] { null, null, null, RewardType.Bomb, RewardType.Revealing,
            RewardType.Life, RewardType.Helper, RewardType.Pair,null,null,RewardType.Split,null,null };
        try
        {
            ValidateMechanicBindings();
            Require(LevelPlaytestSchedule.Settings.Enabled && LevelPlaytestSchedule.Settings.StartLevel == 5, "Playtest starts at display level 5");
            Require(LevelPlaytestSchedule.Settings.Cycle.Count == 13, "Six level types, two split sizes and six powerup trials");
            for (int id = 0; id < 4; id++) Require(!LevelPlaytestSchedule.TryCreate(id, out _), "First four levels are untouched");
            for (int cycle = 0; cycle < 12; cycle++)
            for (int index = 0; index < expectedMechanics.Length; index++)
            {
                int id = 4 + cycle * expectedMechanics.Length + index;
                Require(LevelPlaytestSchedule.TryCreate(id, out var data) && data.Mechanic == expectedMechanics[index]
                    && data.TrialPowerUp == expectedPowers[index] && data.PlaytestStep == index + 1, "Deterministic repeated sequence at " + (id + 1));
                Require(LevelPlaytestSchedule.CanResume(id, data), "Matching playtest definitions resume");
                var changed = data.Copy(); changed.PlaytestRevision++;
                Require(!LevelPlaytestSchedule.CanResume(id, changed), "Old test revisions cannot override the new content profile");
                Require(!LevelPlaytestSchedule.CanResume(id, new LevelData { Mechanic = LevelMechanic.LegacyElimination }), "Old scheduled content migrates to the test cycle");
                Require(!LevelMechanicRules.ShouldTrigger(LevelMechanic.LegacyElimination, new GuessEvaluation(3, new[] {1,2}, new[] {3,4}), 4), "Removed rule never triggers");
                foreach (var power in PowerUpTypeUtility.OrderedRewardTypes) Require(PowerUpTypeUtility.IsUnlocked(power, id), "All test powers are unlocked");
            }
            Require(LevelDataGetter.GetStartingLevelsData().All(data => data.Mechanic == LevelMechanic.Normal)
                && LevelDataGetter.GetLoopLevelsData().All(data => data.Mechanic == LevelMechanic.Normal), "No permanent special-level placement remains");

            for (int index = 0; index < expectedMechanics.Length; index++)
            {
                ClearPreviewActors(canvas);
                int id = 4 + index;
                LevelPlaytestSchedule.TryCreate(id, out var definition);
                using (var game = new Fixture(canvas, camera, definition))
                {
                    Field(game.Tracker, "_levelId", id); Field(game.Tracker, "_starCount", 73); Field(game.Tracker, "_giftStarCount", 2);
                    foreach (string field in new[] { "_bombPowerUpCount", "_revealingPowerUpCount", "_lifePowerUpCount", "_helperPowerUpCount", "_pairPowerUpCount", "_splitPowerUpCount" }) Field(game.Tracker, field, 0);
                    game.BuildActors(); game.Gate.Clear();
                    game.Target.RevealMultiplayerAnswer(new[] {4,1,3,5,2}.Take(definition.NumOfBoardHolders).ToArray());
                    var helper = new HelperEffect(game.Round, game.Target, game._saves);
                    var pair = new PairEffect(game.Round, game.Target, game._saves, game._info, game.Tracker);
                    Field(game._powers, "_helper", helper); Field(game._powers, "_pair", pair);
                    Field(game.PowerMessages, "_helper", helper); Field(game.PowerMessages, "_pair", pair);
                    var snapshot = new GameSaveSnapshotProvider(game.Target, game._info, new PlayingFlow(), game._counts, game.Round);
                    Field(snapshot, "_saveData", game._saves); Field(snapshot, "_levels", new TestLevel(definition)); Field(snapshot, "_tracker", game.Tracker);
                    using (var rules = new LevelMechanicsController(game.Results, helper, game.Round, game._saves,
                        game.Target, game._area, game._powers, game.Tracker, pair,(BoardSplitService)Get(game.Results,"_split"),game.Board))
                    {
                        Field(rules, "_levels", new TestLevel(definition)); rules.Attach(game.View, null);
                        LevelPlaytestSchedule.PrepareTrialStock(game.Tracker, game._saves, definition);
                        rules.Begin(definition.Mechanic); game._powers.Refresh(); Tick(.3f);
                        var caption = canvas.GetComponentsInChildren<TMPro.TMP_Text>(true).Single(text => text.name == "MechanicStatus");
                        Require(caption.text.StartsWith("Test " + (index + 1) + "/13"), "Current test case is identified in the header");
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
                        if (definition.TrialPowerUp.HasValue)
                        {
                            var type = definition.TrialPowerUp.Value;
                            Require(game.Tracker.GetPowerUpCount(type) == 1, "Trial replenishes an empty inventory");
                            LevelPlaytestSchedule.PrepareTrialStock(game.Tracker, game._saves, definition);
                            Require(game.Tracker.GetPowerUpCount(type) == 1, "Preparation cannot accumulate extra stock");
                            if (type == RewardType.Helper) WrongGuess();
                            if (type == RewardType.Life) for (int n = 0; n < 3; n++) WrongGuess();
                            if (type == RewardType.Bomb)
                            {
                                var bomb = new BombPowerUpController(game.PowerMessages, game.Board, game.Placement, game.Target, game._info, game._counts, game.Round);
                                Field(bomb, "_pair", pair);
                            }
                            var button = (BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(type);
                            int remaining = game.Round.GetRemainingGuessCount();
                            Click(button);
                            if (type == RewardType.Revealing) Require(game.Reveal.TryRequestRevealCard(0), "Reveal trial accepts a chosen slot");
                            if (type == RewardType.Life)
                            {
                                Require(game.Round.GetRemainingGuessCount() == remaining + 3, "Extra lives commit before the visual animation");
                                Require(snapshot.TryCreateSnapshot(out var lifeSave) && lifeSave.RemainingGuessCount == remaining + 3, "Pausing a life animation preserves the awarded lives");
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
            using (LevelPlaytestSchedule.UseForTests(new LevelPlaytestSettings { Enabled = false }))
            {
                Require(!LevelPlaytestSchedule.TryCreate(99, out _), "One setting disables the temporary sequence");
                Require(!PowerUpTypeUtility.IsUnlocked(RewardType.Pair, 99) && !PowerUpTypeUtility.IsUnlocked(RewardType.Helper, 99), "New powers have no assigned production unlocks yet");
            }
            Require(errors.Count == 0, "No runtime/tween errors: " + string.Join("; ", errors));
            File.WriteAllText(Folder + "/playtest-review.json", JsonConvert.SerializeObject(new { passed = true, stages,
                repeated_cycles = 12, first_level = 5, checks = new[] { "first four preserved", "all current level types", "six actual powerup uses", "removed elimination", "deterministic repeats", "trial resupply", "no duplicate on resume", "retry resupply", "life save during animation", "round definition migration", "currency/progress preserved", "test profile can be disabled", "real scene DI", "unchanged real PlayerPrefs" }
            }, Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = oldUpdate; Application.logMessageReceived -= collect; }
        Debug.Log("PLAYTEST_PASS: repeated eleven-step sequence and every playable powerup trial");
    }
}

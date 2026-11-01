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
using Object = UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeMechanicViews()
    {
        foreach (string path in new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/Menu.unity" })
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool close = !scene.isLoaded;
            if (close) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var view in root.GetComponentsInChildren<PowerUpListHolderView>(true))
                    {
                        view.GetPowerUpButton(RewardType.Helper); view.SetPowerUpCount(RewardType.Helper, 0);
                        EditorUtility.SetDirty(view);
                    }
                    foreach (var view in root.GetComponentsInChildren<VerticalCrystalProgressView>(true))
                    { view.ApplyLabStyle(); EditorUtility.SetDirty(view); }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (close) EditorSceneManager.CloseScene(scene, true); }
        }
        var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/CrystalProgressBar.prefab");
        try
        {
            prefab.GetComponent<VerticalCrystalProgressView>().ApplyLabStyle();
            PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/Prefabs/UI/CrystalProgressBar.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        return "Saved Helper button references in Game/Menu and the shared crystal prefab";
    }

    public static void ReviewLevelMechanics(Canvas canvas, Camera camera)
    {
        var oldUpdate = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        try
        {
            ValidateMechanicBindings();
            Require(errors.Count == 0, "Game scene service bindings validate: " + string.Join("; ", errors));
            Require(LevelDataGetter.GetStartingLevelsData().All(l => l.Mechanic == LevelMechanic.Normal)
                && LevelDataGetter.GetLoopLevelsData().All(l => l.Mechanic == LevelMechanic.Normal), "Permanent special-level distribution is deferred");
            int evaluations = 0;
            for (int size = 2; size <= 5; size++)
            {
                var answer = Enumerable.Range(1, size).ToArray();
                foreach (var guess in new[] { answer.ToArray(), answer.Reverse().ToArray(), Enumerable.Range(5, size).ToArray() })
                {
                    var result = new GuessEvaluation(3, guess, answer);
                    Require(result.Correct == guess.Where((card, i) => answer[i] == card).Count(), "Exact positions");
                    Require(result.Correct + result.Misplaced == guess.Count(answer.Contains), "Matching fruits");
                    Require(!LevelMechanicRules.ShouldTrigger(LevelMechanic.Normal, result, 3)
                        && !LevelMechanicRules.ShouldTrigger(LevelMechanic.Helper, result, 0)
                        && LevelMechanicRules.ShouldTrigger(LevelMechanic.Helper, result, 3) == !result.Won, "Win/final-fail/normal trigger guards");
                    guess[0] = 99; Require(result.Cards[0] != 99, "Submitted guess is an immutable copy");
                    evaluations++;
                }
            }
            Require(new GuessEvaluation(1, new[] {1,1,2}, new[] {1,2,3}).Misplaced == 1, "Duplicate-safe evaluator");
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas, camera))
            {
                game.BuildActors(); game.Gate.Clear();
                Field(game.Tracker, "_levelId", 24); Field(game.Tracker, "_helperPowerUpCount", 2);
                var helper = new HelperEffect(game.Round, game.Target, game._saves);
                var eliminate = new EliminateFruitEffect(game.Target, game._info, game.Placement, game._saves);
                Field(game._powers, "_helper", helper); Field(game.PowerMessages, "_helper", helper);
                var snapshot = new GameSaveSnapshotProvider(game.Target, game._info, new PlayingFlow(), game._counts, game.Round);
                Field(snapshot, "_saveData", game._saves); Field(snapshot, "_tracker", game.Tracker);
                var memory = new MemorySave();
                using (var rules = new LevelMechanicsController(game.Results, helper, game.Round, game._saves,
                    game.Target, game._area, game._powers, game.Tracker))
                {
                    rules.Attach(game.View, () => { if (snapshot.TryCreateSnapshot(out var data)) memory.Save(data); });
                    rules.Begin(LevelMechanic.Helper); game._powers.Refresh();
                    Require(!helper.CanApply && !helper.TryApply(), "No Helper before a submitted guess");
                    for (int i = 0; i < 3; i++) { game.Place(0); game.Place(1); game.Check(); Tick(.6f); }
                    Require(game._saves.GetLevelSaveData().RevealedGuessNumbers.SequenceEqual(new[] {3}), "Every third wrong guess reveals only its own row");
                    Require(!helper.CanApply && !helper.TryApply() && game.Tracker.GetPowerUpCount(RewardType.Helper) == 2, "Already revealed guess cannot consume inventory");
                    Require(canvas.GetComponentsInChildren<FruitLabGlyph>(true).Any(g => g.name == "HelperAbsent"), "Absent feedback has a visible cross");
                    game.Place(2); game.Place(0); game.Check(); Tick(.6f);
                    Require(helper.CanApply, "New last guess can be inspected");
                    var helperButton = canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Helper);
                    Click((BaseButtonView)helperButton);
                    Require(game.Tracker.GetPowerUpCount(RewardType.Helper) == 1 && game._saves.GetLevelSaveData().RevealedGuessNumbers.SequenceEqual(new[] {3,4}), "Manual inventory and automatic rule share Helper effect");
                    Require(memory.Data.RevealedGuessNumbers.SequenceEqual(new[] {3,4}), "Checkpoint includes inspection state");
                    game._saves.SetLevelSaveDataAsSaved(memory.Data); game.Round.Initialize();
                    game._area.RemoveResultBlocks(); game.Results.TryAddTriedCards(); rules.Begin(LevelMechanic.Helper);
                    Require(!helper.CanApply, "Resume cannot re-spend on the same guess");
                    // The preferred fruit must be absent from the answer, independent of manual notes.
                    int preferred = 2;
                    Require(eliminate.TryApply(new[] {preferred}, 5, out int removed) && removed == preferred - 1, "Prefer an absent fruit from the submitted guess");
                    Require(game.Initial.TryGetCardItem(removed, out _), "Effect model commits before animation");
                    Require(!game.Placement.TryPlaceCardOnBoard(removed, 0), "Eliminated card cannot be re-placed during its exit animation");
                    game.Initial.DestroyCard(removed);
                    Require(!game.Initial.TryGetCardItem(removed, out _) && canvas.GetComponentsInChildren<InitialHolderView>(true).Length == 5, "Eliminating a fruit preserves its holder");
                    Require(!eliminate.TryApply(new[] {3}, 5, out _), "Duplicate crystal elimination cannot apply twice on the same guess");
                    Require(eliminate.TryApply(new[] {4,1}, 6, out int fallback) && !game.Target.GetTargetCardsList().Contains(fallback + 1), "Fallback always eliminates a genuinely absent fruit");
                    Require(snapshot.TryCreateSnapshot(out var saved), "Mutated round can be saved");
                    string json = JsonConvert.SerializeObject(saved);
                    var loaded = JsonConvert.DeserializeObject<LevelSaveData>(json);
                    Require(!loaded.CardItemInfoList[removed].isExisted && loaded.RevealedGuessNumbers.Count == 2, "Round-trip preserves eliminations and revealed guesses");
                    saved.TargetCards[0] = 99; saved.CardItemInfoList[0].possibleCardHolderIndicatorIndexes.Clear();
                    Require(game.Target.GetTargetCardsList()[0] != 99 && game._info.GetCardItemInfoList()[0].possibleCardHolderIndicatorIndexes.Count > 0, "Save is a deep snapshot");
                    var legacy = JsonConvert.DeserializeObject<LevelSaveData>("{\"RemainingGuessCount\":4}");
                    Require(legacy.RevealedGuessNumbers.Count == 0 && legacy.Definition == null, "Old saves default to no new effects");
                    game.View.SetMechanicStatus(LevelMechanicRules.Description(LevelMechanic.Helper) + " · 1/3");
                    Layer(canvas.transform); Draw(camera); Capture(camera, Folder + "/preview-helper-feedback.png");
                }
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas, camera))
            {
                game.BuildActors(); game.Gate.Clear(); Field(game.Tracker, "_levelId", 24);
                var helper = new HelperEffect(game.Round, game.Target, game._saves);
                using (var rules = new LevelMechanicsController(game.Results, helper, game.Round, game._saves,
                    game.Target, game._area, game._powers, game.Tracker))
                {
                    rules.Attach(game.View, null); rules.Begin(LevelMechanic.LegacyElimination);
                    for (int i = 0; i < 3; i++) { game.Place(0); game.Place(1); game.Check(); Tick(.6f); }
                    Require(game._info.GetCardItemInfoList().All(card => card.isExisted)
                        && game._saves.GetLevelSaveData().RevealedGuessNumbers.Count == 0, "Retired elimination saves behave as normal levels");
                }
            }
            Require(errors.Count == 0, "No Unity/tween errors: " + string.Join("; ", errors));
            using (TutorialProgress.UseTestValues(new Dictionary<string, int>()))
            using (var game = new Fixture(canvas, camera))
            {
                game.BuildActors(); Field(game.Tracker, "_levelId", 21); Field(game.Tracker, "_helperPowerUpCount", 1);
                Field(game.Tracker, "_levelId", 18); Field(game.Tracker, "_currentRewardType", RewardType.Life);
                Field(game.Tracker, "_giftStarCount", ConstantValues.NUM_OF_STARS_FOR_WILD - 1);
                RewardType preview = game.Tracker.GetCurrentRewardType();
                int before = game.Tracker.GetPowerUpCount(preview);
                game.Tracker.IncrementLevelIdWithRewards(0, 1);
                Require(game.Tracker.GetPowerUpCount(preview) == before + 1, "Reward matches preview when crossing an unlock with old inventory state");
                Field(game.Tracker, "_levelId", 21);
                var levels = new MechanicLevel { Mechanic = LevelMechanic.Helper };
                Field(game.Bootstrap, "_levels", levels);
                game.Bootstrap.StartRound(); Require(game.Message != null, "First special level explains its rule");
                game.Next(); Require(TutorialProgress.Seen(TutorialProgress.HelperLevel), "Rule introduction acknowledges once");
                Require(game.Gate.TryExplain(GameUIButtonType.HelperPowerUp, () => { }), "First Helper inventory use explains its feedback");
                game.Next(); Require(TutorialProgress.Seen(TutorialProgress.Helper), "Helper explanation has its own flag");
            }
            File.WriteAllText(Folder + "/level-mechanics-review.json", JsonConvert.SerializeObject(new {
                passed = true, evaluations, permanent_special_levels = 0, retired_elimination_checked = true,
                checks = new[] { "real submitted guesses", "shared automatic/manual Helper", "inventory consumed once", "no pre-guess Helper", "no last-life trigger", "deep save/resume", "old save defaults", "preferred/fallback elimination", "holder retained", "no duplicate elimination", "unchanged player preferences" }
            }, Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = oldUpdate; Application.logMessageReceived -= collect; }
        Debug.Log("LEVEL_MECHANICS_PASS: real guesses, shared effects, inventory, replay safety and save migration");
    }
    private sealed class MechanicLevel : ILevelDataCreator
    {
        public LevelMechanic Mechanic;
        public LevelData GetLevelData() => new LevelData { NumOfBoardHolders = 2, NumOfCards = 5, MaxNumOfTries = 6, Mechanic = Mechanic };
        public void SetSinglePlayerLevelData() { } public void SetMultiplayerLevelData() { }
    }

    private static void ValidateMechanicBindings()
    {
            Require(!Zenject.ProjectContext.HasInstance, "DI validation runs outside Play mode");
            var bindingScene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
            try
            {
                Zenject.ProjectContext.ValidateOnNextRun = true;
                var context = bindingScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Zenject.SceneContext>(true)).Single();
                context.Validate();
            }
            finally
            {
                Zenject.ProjectContext.ValidateOnNextRun = false;
                if (Zenject.ProjectContext.HasInstance) Object.DestroyImmediate(Zenject.ProjectContext.Instance.gameObject);
                EditorSceneManager.ClosePreviewScene(bindingScene);
            }
    }

    public static void ReviewGrowingBoard(Canvas canvas,Camera camera)
    {
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        string profile = Profile(); var errors = new List<string>(); var checkpoints = new List<LevelSaveData>();
        LevelSaveData lockedCheckpoint = null; var capacityCheckpoints = new List<LevelSaveData>();
        Application.LogCallback collect = (message,stack,type) => { if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message); };
        Application.logMessageReceived += collect;
        try
        {
            ValidateMechanicBindings();
            CheckGrowingCapMigration(canvas,camera);
            Require((int)LevelMechanic.Split == 4 && (int)LevelMechanic.Growing == 5,"Existing mechanic IDs are preserved");
            for (int id = 4; id < 70; id++)
            {
                int oldStep = (id-4)%11;
                var legacy = LevelPlaytestSchedule.Settings.Cycle[oldStep].Copy();
                legacy.PlaytestRevision = 3; legacy.PlaytestStepCount = 11; legacy.PlaytestStep = oldStep+1;
                Require(LevelPlaytestSchedule.CanResume(id,legacy),"Appending Growing preserves the previous 11-step round at "+(id+1));
            }
            var definition = new LevelData { NumOfBoardHolders = 2,NumOfCards = 9,MaxNumOfTries = 8,Mechanic = LevelMechanic.Growing };
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear();
                game._saves.GetLevelSaveData().GrowingBoard.FullTarget = new List<int> {1,2,3,4}; game.Target.SetTargetNumber(2);
                var growth = GrowthFor(game,definition);
                using (var rules = GrowingRules(game,definition,growth))
                {
                    var provider = GrowingSnapshot(game,definition); LevelSaveData immediate = null;
                    rules.Attach(game.View,() => provider.TryCreateSnapshot(out immediate)); rules.Begin(LevelMechanic.Growing);
                    Require(provider.TryCreateSnapshot(out var initial) && GrowingBoardState.IsValid(initial),"An untouched Growing round saves all four future fruits");
                    checkpoints.Add(initial);
                    for (int number = 1; number <= 6; number++)
                    {
                        var prefix = game.Target.GetTargetCardsList().ToArray();
                        SubmitSplit(game,prefix.Reverse().ToArray());
                        int count = Math.Min(4,2+number/2);
                        Require(game._counts.GetBoardHolderCount() == count && game.Target.GetTargetCardsList().SequenceEqual(new[] {1,2,3,4}.Take(count)),"Growth preserves the established answer prefix");
                        Require(game.Round.GetRemainingGuessCount() == 8-number+(number >= 4 ? 4 : 0),"One life per miss; one final four-life grant");
                        Require(provider.TryCreateSnapshot(out var saved) && GrowingBoardState.IsValid(saved),"The checkpoint is valid immediately after guess "+number);
                        Require(immediate != null && GrowingBoardState.IsValid(immediate) && immediate.GrowingBoard.LastProcessedGuess == number,"Checkpoint commits before holder/life animations finish");
                        checkpoints.Add(JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(immediate)));
                        var rows = canvas.GetComponentsInChildren<ResultBlockView>().ToArray();
                        Require(rows.Length == number && rows.Select(row => row.GetComponentsInChildren<NonDraggableCardItemView>().Length).SequenceEqual(game.Round.GetTriedCardsList().Select(cards => cards.Count)),"History retains its original 2/3/4-card widths");
                        if (number == 2 || number == 4)
                        { Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-growing-"+count+".png"); }
                    }
                    int remaining = game.Round.GetRemainingGuessCount();
                    Require(!growth.TryAdvance(new GuessEvaluation(6,game.Round.GetTriedCardsList()[5],game.Target.GetTargetCardsList()))
                        && game.Round.GetRemainingGuessCount() == remaining,"Repeated resolution cannot grow or grant lives twice");
                    var helper = new HelperEffect(game.Round,game.Target,game._saves); Field(helper,"_split",Get(game.Results,"_split"));
                    Require(helper.TryApply() && helper.EvaluateGuess(1).Cards.Count == 2,"Helper can explain both the latest and an early short guess");
                    var split = (BoardSplitService)Get(game.Results,"_split"); Require(split.TryApply(() => true),"Manual Split works on the grown four-slot board");
                    game._area.RemoveResultBlocks(); game.Results.TryAddTriedCards(); game._area.RevealAnswer(game.Target.GetTargetCardsList());
                    Require(provider.TryCreateSnapshot(out var decorated),"Powerup metadata stays in the growth checkpoint");
                    checkpoints.Add(decorated);
                }
            }
            // Real actor reconstruction at every boundary, including one miss before growth.
            foreach (var saved in checkpoints)
            {
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,definition))
                {
                    Field(game.Tracker,"_levelId",20); var loaded = JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(saved));
                    game._saves.SetLevelSaveDataAsSaved(loaded); game.Round.Initialize(); game.BuildActors(); game.Results.TryAddTriedCards(); game.Gate.Clear();
                    using (var rules = GrowingRules(game,definition,GrowthFor(game,definition)))
                    {
                        rules.Attach(game.View,null); rules.Begin(LevelMechanic.Growing);
                        Require(game.Target.GetTargetCardsList().SequenceEqual(saved.TargetCards) && game._counts.GetBoardHolderCount() == saved.GrowingBoard.ActiveCount,"Resume restores the exact visible answer and slot count");
                        Require(game.Round.GetRemainingGuessCount() == saved.RemainingGuessCount && game.Round.GetMaxGuessCount() == 8+(saved.GrowingBoard.BonusGranted ? 4 : 0),"Resume preserves capacity and never repeats the bonus");
                        Require(game.Round.GetLifeBarRewardInfoList().Select(reward => reward.IsActive).SequenceEqual(saved.ActiveRewardTokens),"Consumed reward tokens do not return after resume");
                        if (!saved.BoardSplitApplied)
                        {
                            int next = saved.TriedCardsList.Count+1;
                            SubmitSplit(game,game.Target.GetTargetCardsList().AsEnumerable().Reverse().ToArray());
                            Require(game._counts.GetBoardHolderCount() == Math.Min(4,2+next/2),"The next miss uses the restored growth counter");
                        }
                    }
                }
            }
            // Revealed cards survive holder growth and actual save/rebuild.
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear();
                Field(game.Tracker,"_bombPowerUpCount",1); game._powers.Refresh();
                Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Bomb));
                Require(game.Tracker.GetPowerUpCount(RewardType.Bomb) == 1 && game._counts.GetBoardHolderCount() == 2,"Bomb is disabled before the first Growing guess without spending inventory");
                var full = game._saves.GetLevelSaveData().GrowingBoard.FullTarget.ToArray();
                Require(game.Placement.TryRevealAndLockCard(0,full[0]-1,false),"Reveal an existing slot before growth");
                var holder = game.Board.GetRectTransformOfGarden(0); var card = game.Initial.GetRectTransformOfCardItem(full[0]-1);
                using (var rules = GrowingRules(game,definition,GrowthFor(game,definition)))
                {
                    rules.Begin(LevelMechanic.Growing);
                    int absent = Enumerable.Range(1,9).First(fruit => !full.Contains(fruit));
                    for (int n = 0; n < 2; n++)
                    {
                        Require(game.Placement.TryPlaceCardOnBoard(absent-1,1),"Place a wrong fruit beside the fixed card");
                        game.Check(); Tick(.4f);
                    }
                    Require(game.Board.GetRectTransformOfGarden(0) == holder && card.IsChildOf(holder) && game._info.GetCardItemInfoList()[full[0]-1].isLocked,"Growth keeps the same locked fruit and holder");
                    var elimination = new EliminateFruitEffect(game.Target,game._info,game.Placement);
                    while (elimination.TryApply(new[] {full[3]},0,out int removed)) Require(!full.Contains(removed+1),"Elimination protects unopened target fruit");
                    Require(full.All(fruit => game._info.GetCardItemInfoList()[fruit-1].isExisted),"All future answer fruit remain playable");
                    Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out var locked),"Locked-card growth saves");
                    lockedCheckpoint = locked;
                }
                Field(game.Tracker,"_bombPowerUpCount",1); game._powers.Refresh();
                Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Bomb));
                Require(game.Tracker.GetPowerUpCount(RewardType.Bomb) == 1 && !game._counts.CanShrink,"Growing cannot spend an incompatible Bomb");
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20); game._saves.SetLevelSaveDataAsSaved(JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(lockedCheckpoint)));
                game.Round.Initialize(); game.BuildActors(); int fruit = lockedCheckpoint.TargetCards[0]-1;
                Require(game._info.GetCardItemInfoList()[fruit].isLocked && game.Initial.GetRectTransformOfCardItem(fruit).IsChildOf(game.Board.GetRectTransformOfGarden(0)),"A revealed fruit restores onto the same grown slot");
            }
            foreach (int startingLives in new[] {4,8})
            {
                ClearPreviewActors(canvas); var capacity = definition.Copy(); capacity.MaxNumOfTries = startingLives;
                using (var game = new Fixture(canvas,camera,capacity))
                {
                    Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear(); int failures = 0; game._guess.LevelFailEvent += (s,e) => failures++;
                    using (var rules = GrowingRules(game,capacity,GrowthFor(game,capacity)))
                    {
                        rules.Begin(LevelMechanic.Growing);
                        for (int n = 0; n < 3; n++) SubmitSplit(game,game.Target.GetTargetCardsList().AsEnumerable().Reverse().ToArray());
                        if (startingLives == 8) Require(game._guess.GrantExtraLives(3),"A life powerup can precede the growth bonus");
                        int before = game.Round.GetRemainingGuessCount();
                        SubmitSplit(game,game.Target.GetTargetCardsList().AsEnumerable().Reverse().ToArray()); Tick(2f);
                        Require(failures == 0 && game.Round.GetRemainingGuessCount() == before-1+4 && game.Round.GetMaxGuessCount() == startingLives+4,"The fourth slot safely rescues a last life and can exceed the original capacity");
                        Require(GrowingSnapshot(game,capacity).TryCreateSnapshot(out var saved) && GrowingBoardState.IsValid(saved),"Capacity-expanded saves validate");
                        capacityCheckpoints.Add(saved);
                        while (game.Round.GetRemainingGuessCount() > 0) SubmitSplit(game,game.Target.GetTargetCardsList().AsEnumerable().Reverse().ToArray());
                        Tick(2f); game._area.RevealAnswer(game.Target.GetTargetCardsList());
                        Require(failures == 1 && !GrowingSnapshot(game,capacity).TryCreateSnapshot(out _),"A later exhausted grown board fails once and is not resumable");
                        var memory = new MemorySave(); memory.Save(saved);
                        var lifecycleRoot = new GameObject("GrowthPauseReview");
                        try
                        {
                            var lifecycle = lifecycleRoot.AddComponent<GamePlayContext>();
                            Field(lifecycle,"_gameSaveService",memory); Field(lifecycle,"_gameSaveSnapshotProvider",GrowingSnapshot(game,capacity));
                            Field(lifecycle,"_levelTracker",game.Tracker); Field(lifecycle,"_levelFlowOrchestrator",new PlayingFlow()); Field(lifecycle,"_roundStateManager",game.Round);
                            typeof(GamePlayContext).GetMethod("TrySave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(lifecycle,null);
                            Require(!memory.HasSavedGame(),"Closing during the last-life animation cannot resurrect the previous checkpoint");
                        }
                        finally { Object.DestroyImmediate(lifecycleRoot); }
                    }
                }
            }
            foreach (var saved in capacityCheckpoints)
            {
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,saved.Definition))
                {
                    Field(game.Tracker,"_levelId",20); game._saves.SetLevelSaveDataAsSaved(JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(saved)));
                    game.Round.Initialize(); game.BuildActors();
                    Require(game.Round.GetRemainingGuessCount() == saved.RemainingGuessCount && game.Round.GetMaxGuessCount() == saved.Definition.MaxNumOfTries+4,"A resume preserves a life powerup stacked with the one-time growth grant");
                }
            }
            // Early success and retry have no forced growth or inherited bonus.
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear(); int wins = 0; game.Results.LevelSuccessEvent += (s,e) => wins++;
                using (var rules = GrowingRules(game,definition,GrowthFor(game,definition)))
                {
                    rules.Begin(LevelMechanic.Growing); SubmitSplit(game,game.Target.GetTargetCardsList().ToArray());
                    Require(wins == 1 && game._counts.GetBoardHolderCount() == 2 && !game._saves.GetLevelSaveData().GrowingBoard.BonusGranted,"A correct two-slot answer wins immediately");
                }
                game._saves.CreateDefaultLevelSaveData(); game.Round.Initialize(); game.BuildActors();
                Require(game._counts.GetBoardHolderCount() == 2 && game.Round.GetMaxGuessCount() == 8 && game.Round.GetTriedCardsList().Count == 0,"Retry creates a fresh two-slot round without a bonus");
            }
            Require(errors.Count == 0 && profile == Profile(),"No Unity/tween errors or live PlayerPrefs changes: "+string.Join("; ",errors));
            File.WriteAllText(Folder+"/growing-review.json",JsonConvert.SerializeObject(new { passed = true,checkpoints = checkpoints.Count,
                checks = new[] {"actual DI bindings", "2→3→4", "no fifth slot after further misses", "five-cap save migration and history", "fixed future answer", "original-width history", "one +4 grant", "checkpoint before first guess", "resume at every boundary", "legacy cycle save compatibility", "reward flags preserved", "locked holders retained", "future fruit protected", "Helper and Split compatibility", "zero-life rescue", "life powerup capacity", "early win", "retry reset", "unchanged PlayerPrefs"}
            },Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = previous; Application.logMessageReceived -= collect; }
    }

    private static GrowingBoardService GrowthFor(Fixture game,LevelData definition) => new GrowingBoardService(new TestLevel(definition),game.Tracker,game._saves,game.Target,game.Board,game._info,game.Round,game._guess);
    private static LevelMechanicsController GrowingRules(Fixture game,LevelData definition,GrowingBoardService growth)
    {
        var helper = new HelperEffect(game.Round,game.Target,game._saves); Field(helper,"_split",Get(game.Results,"_split"));
        var rules = new LevelMechanicsController(game.Results,helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker,null,(BoardSplitService)Get(game.Results,"_split"),game.Board,growth);
        Field(rules,"_levels",new TestLevel(definition)); return rules;
    }
    private static GameSaveSnapshotProvider GrowingSnapshot(Fixture game,LevelData definition)
    {
        var provider = new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
        Field(provider,"_saveData",game._saves); Field(provider,"_levels",new TestLevel(definition)); Field(provider,"_tracker",game.Tracker); return provider;
    }

    private static void CheckGrowingCapMigration(Canvas canvas,Camera camera)
    {
        for (int misses = 0; misses <= 9; misses++)
        {
            var definition = new LevelData { NumOfBoardHolders = 2,NumOfCards = 9,MaxNumOfTries = 8,Mechanic = LevelMechanic.Growing };
            var source = new LevelSaveDataManager(new TestLevel(definition)); source.CreateDefaultLevelSaveData();
            var saved = source.GetLevelSaveData(); int oldCount = Math.Min(5,2+misses/2);
            saved.GrowingBoard = new GrowingBoardState { FullTarget = new List<int> {1,2,3,4,5},ActiveCount = oldCount,
                BonusGranted = oldCount == 5,WrongSinceGrowth = oldCount == 5 ? 0 : misses%2,LastProcessedGuess = misses };
            saved.TargetCards = saved.GrowingBoard.FullTarget.Take(oldCount).ToList();
            for (int n = 0; n < misses; n++) saved.TriedCardsList.Add(Enumerable.Range(1,Math.Min(5,2+n/2)).Reverse().ToList());
            saved.RemainingGuessCount = 8-misses+(oldCount == 5 ? 4 : 0);
            int expectedLives = saved.RemainingGuessCount+(oldCount == 4 ? 4 : 0);
            if (oldCount == 5)
            {
                saved.CardItemInfoList[4].isLocked = true;
                saved.CardItemInfoList[4].probabilityType = ProbabilityType.Certain;
                saved.CardItemInfoList[4].possibleCardHolderIndicatorIndexes = new List<int> {4};
                saved.CardItemInfoList[0].isLocked = true;
                saved.CardItemInfoList[0].possibleCardHolderIndicatorIndexes = new List<int> {0};
            }
            GrowingBoardState.UpgradeLegacy(saved);
            Require(GrowingBoardState.IsValid(saved) && saved.TargetCards.Count <= 4 && saved.GrowingBoard.FullTarget.SequenceEqual(new[] {1,2,3,4}),"A previous five-cap save upgrades without restarting");
            Require(saved.RemainingGuessCount == expectedLives && saved.GrowingBoard.BonusGranted == (misses >= 4),"The four-slot bonus migrates once without duplicating an old bonus");
            GrowingBoardState.UpgradeLegacy(saved); Require(saved.RemainingGuessCount == expectedLives,"Legacy migration is idempotent");
            if (oldCount == 5) Require(!saved.CardItemInfoList[4].isLocked && saved.CardItemInfoList[0].isLocked,"Only a lock on the removed fifth slot is released");
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",15); game._saves.SetLevelSaveDataAsSaved(JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(saved)));
                game.Round.Initialize(); game.BuildActors(); game.Results.TryAddTriedCards(); game.Gate.Clear();
                Require(game._counts.GetBoardHolderCount() == Math.Min(4,2+misses/2) && (game.Target.GetTargetCardsList().Count < 4 || !game.Target.TryAppendTargetCard(9)),"Legacy resume builds at most four slots and rejects a fifth target");
                Require(game.Target.GetProtectedTargetCards().Count == 4,"The removed fifth fruit is not protected as a future live answer");
                if (misses > 0)
                {
                    var helper = new HelperEffect(game.Round,game.Target,game._saves); Field(helper,"_split",Get(game.Results,"_split"));
                    Require(helper.EvaluateGuess(misses).Cards.Count == saved.TriedCardsList[misses-1].Count && helper.TryApply(),"Legacy five-fruit history keeps its original Helper feedback");
                    var split = (BoardSplitService)Get(game.Results,"_split");
                    Require(split.CanApply == (saved.GrowingBoard.ActiveCount == 4),"Split availability can inspect an old five-fruit last guess");
                }
                game._area.RevealAnswer(game.Target.GetHistoryTargetCards());
                Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out var migrated) && GrowingBoardState.IsValid(migrated)
                    && migrated.TriedCardsList.Select(row => row.Count).SequenceEqual(saved.TriedCardsList.Select(row => row.Count)),"Migrated history survives another checkpoint unchanged");
                if (misses >= 7)
                {
                    Require(migrated.GrowingBoard.LegacyHistoryTarget.SequenceEqual(new[] {1,2,3,4,5}),"The archived answer survives deep-copy serialization");
                    int wins = 0; game.Results.LevelSuccessEvent += (s,e) => wins++;
                    for (int slot = 1; slot < 4; slot++) Require(game.Placement.TryPlaceCardOnBoard(slot,slot),"Finish with four correct cards beside the restored lock");
                    game.Check(); Tick(.4f);
                    Require(wins == 1 && game.Target.GetTargetCardsList().Count == 4,"Four correct fruit win even when an old row has five cards");
                    game._area.RevealAnswer(game.Target.GetHistoryTargetCards());
                }
            }
        }
    }

    public static string BakeDynamiteView()
    {
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/BoardHolder.prefab");
        try
        {
            var view = root.GetComponent<BoardHolderView>();
            view.ShowDynamite(new DynamiteRoundState { BoardIndex = 0,LastProcessedGuess = 0,GuessesUsed = 0 });
            view.ShowDynamite(null); EditorUtility.SetDirty(view);
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/BoardHolder.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return "Saved compact nonblocking dynamite badge in BoardHolder prefab";
    }

    public static void ReviewDynamite(Canvas canvas,Camera camera)
    {
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        string profile = Profile(); var errors = new List<string>(); var checkpoints = new List<LevelSaveData>();
        var definition = new LevelData { NumOfBoardHolders = 3,NumOfCards = 7,MaxNumOfTries = 8,Mechanic = LevelMechanic.Dynamite };
        Application.LogCallback collect = (message,stack,type) => { if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message); };
        Application.logMessageReceived += collect;
        int layouts = 0;
        try
        {
            ValidateMechanicBindings();
            Require((int)LevelMechanic.Growing == 5 && (int)LevelMechanic.Dynamite == 6,"Mechanic IDs append without renumbering existing content");
            for (int id = 4; id < 70; id++)
            {
                int step = (id-4)%12; var old = LevelPlaytestSchedule.Settings.Cycle[step].Copy();
                old.PlaytestRevision = 3; old.PlaytestStepCount = 12; old.PlaytestStep = step+1;
                Require(LevelPlaytestSchedule.CanResume(id,old),"Existing twelve-step saves survive the appended trial");
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                PrepareDynamiteFixture(game); var danger = DangerFor(game,definition);
                using (var rules = DangerRules(game,definition,danger))
                {
                    var provider = GrowingSnapshot(game,definition); LevelSaveData immediate = null;
                    rules.Attach(game.View,() => provider.TryCreateSnapshot(out immediate)); rules.Begin(LevelMechanic.Dynamite);
                    var state = danger.Current;
                    Require(provider.TryCreateSnapshot(out var initial) && DynamiteRoundState.IsValid(initial),"Visible slot and full counter save before the first guess"); checkpoints.Add(initial);
                    Require(state.Revealed && state.Remaining == 4,"Dynamite is visible below the slot before the first guess");
                    var initialBadge = game.Board.GetRectTransformOfGarden(1).GetComponentInParent<BoardHolderView>().transform.Find("DynamiteBadge");
                    Require(initialBadge.gameObject.activeInHierarchy && initialBadge.Find("Moves").GetComponent<TMPro.TMP_Text>().text == "4","Initial counter is four");
                    Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-dynamite-start.png");
                    game.Check();
                    Require(game.Round.GetTriedCardsList().Count == 0 && state.GuessesUsed == 0,"Incomplete guesses cannot spend dynamite moves");
                    Require(game.Placement.TryPlaceCardOnBoard(1,1),"Temporarily arrange the correct fruit without submitting"); Tick(.4f);
                    Require(!state.Disarmed && state.GuessesUsed == 0,"Ordinary card movement does not disarm or count as a move");
                    game.UI.TriggerResetNumbers(); Tick(.4f); int failures = 0; game._guess.LevelFailEvent += (s,e) => failures++;
                    for (int number = 1; number <= 4; number++)
                    {
                        if (number == 4) Require(game._guess.GrantExtraLives(3),"Extra life can restore global guesses but not the dynamite deadline");
                        SubmitDynamite(game,new[] {4,5,6});
                        Require(state.GuessesUsed == number && state.Remaining == 4-number,"The first submitted guess counts toward the four-move deadline");
                        Require(!danger.Resolve(new GuessEvaluation(number,new[] {4,5,6},game.Target.GetTargetCardsList())),"Repeated guess delivery cannot spend another move");
                        if (number < 4)
                        {
                            Require(immediate != null && DynamiteRoundState.IsValid(immediate) && immediate.Dynamite.GuessesUsed == number,"Counter checkpoint commits before animations finish");
                            checkpoints.Add(JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(immediate)));
                            Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-dynamite-"+state.Remaining+".png");
                        }
                    }
                    Tick(2f);
                    Require(state.Exploded && failures == 1 && game.Round.GetRemainingGuessCount() == 0,"Fourth wrong fruit explodes even with extra global life");
                    var exhaustedBadge = game.Board.GetRectTransformOfGarden(1).GetComponentInParent<BoardHolderView>().transform.Find("DynamiteBadge");
                    Require(exhaustedBadge != null && !exhaustedBadge.gameObject.activeInHierarchy,"Explosion hides both the dynamite icon and the zero counter");
                    Require(!game._guess.FailRound() && !game._guess.GrantExtraLives(3,true),"Terminal defeat cannot repeat or be revived by a late callback");
                    int rows = game.Round.GetTriedCardsList().Count; game.Check(); Require(rows == game.Round.GetTriedCardsList().Count,"No fifth guess after explosion");
                    Require(!provider.TryCreateSnapshot(out _),"An exploded game cannot resume");
                    Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-dynamite-exploded.png");
                }
                game._saves.CreateDefaultLevelSaveData(); game.Round.Initialize(); game.BuildActors();
                using (var retry = DangerRules(game,definition,DangerFor(game,definition)))
                {
                    retry.Begin(LevelMechanic.Dynamite);
                    Require(game._saves.GetLevelSaveData().Dynamite.Remaining == 4 && game._saves.GetLevelSaveData().Dynamite.Revealed,"Retry immediately shows a fresh four-move counter");
                }
            }
            foreach (var saved in checkpoints)
            {
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,definition))
                {
                    LoadDynamiteFixture(game,saved); var danger = DangerFor(game,definition);
                    using (var rules = DangerRules(game,definition,danger))
                    {
                        rules.Begin(LevelMechanic.Dynamite);
                        Require(danger.Current.BoardIndex == saved.Dynamite.BoardIndex && danger.Current.Remaining == saved.Dynamite.Remaining
                            && game.Target.GetTargetCardsList().SequenceEqual(saved.TargetCards),"Resume restores the same slot, answer and exact countdown");
                        SubmitDynamite(game,new[] {4,5,6});
                        Require(danger.Current.GuessesUsed == saved.Dynamite.GuessesUsed+1,"Resume does not restart the deadline");
                    }
                }
            }
            // Every legal move, including the fourth, may solve just the danger slot.
            for (int solveAt = 1; solveAt <= 4; solveAt++)
            {
                ClearPreviewActors(canvas);
                LevelSaveData safeSave;
                using (var game = new Fixture(canvas,camera,definition))
                {
                    PrepareDynamiteFixture(game); var danger = DangerFor(game,definition); int failures = 0; game._guess.LevelFailEvent += (s,e) => failures++;
                    using (var rules = DangerRules(game,definition,danger))
                    {
                        rules.Begin(LevelMechanic.Dynamite);
                        for (int n = 1; n < solveAt; n++) SubmitDynamite(game,new[] {4,5,6});
                        SubmitDynamite(game,new[] {4,2,6},0f);
                        // Observe every frame: a stale return tween used to reparent this locked card back to the tray.
                        for (int frame = 0; frame < 90; frame++)
                        {
                            AssertDynamiteCardPinned(game,frame);
                            Tick(1f/120f);
                        }
                        AssertDynamiteCardPinned(game);
                        Require(danger.Current.Disarmed && !danger.Current.Exploded && failures == 0 && game._info.GetCardItemInfoList()[1].isLocked,"Correct fruit disarms and fixes the slot, including on move four");
                        int used = danger.Current.GuessesUsed; SubmitDynamite(game,new[] {4,2,6});
                        AssertDynamiteCardPinned(game);
                        Require(danger.Current.GuessesUsed == used,"A safe dynamite never starts counting again");
                        Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out safeSave) && DynamiteRoundState.IsValid(safeSave),"Safe slot and lock checkpoint together");
                        if (solveAt == 4) { Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-dynamite-safe.png"); }
                    }
                }
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,definition))
                {
                    LoadDynamiteFixture(game,safeSave);
                    using (var rules = DangerRules(game,definition,DangerFor(game,definition)))
                    {
                        rules.Begin(LevelMechanic.Dynamite);
                        Require(game.Initial.GetRectTransformOfCardItem(1).IsChildOf(game.Board.GetRectTransformOfGarden(1)) && game._saves.GetLevelSaveData().Dynamite.Disarmed,"A safe fruit resumes fixed on its original slot");
                    }
                }
            }
            foreach (bool beforeGuess in new[] {true,false})
            {
                ClearPreviewActors(canvas);
                using (var game = new Fixture(canvas,camera,definition))
                {
                    PrepareDynamiteFixture(game); var danger = DangerFor(game,definition);
                    using (var rules = DangerRules(game,definition,danger))
                    {
                        LevelSaveData immediate = null; var provider = GrowingSnapshot(game,definition);
                        rules.Attach(game.View,() => provider.TryCreateSnapshot(out immediate)); rules.Begin(LevelMechanic.Dynamite);
                        if (!beforeGuess) SubmitDynamite(game,new[] {2,5,6},.05f);
                        Field(game.Tracker,"_revealingPowerUpCount",1); game._powers.Refresh();
                        Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Revealing));
                        Require(game.Reveal.TryRequestRevealCard(1) && danger.Current.Disarmed && game.Tracker.GetPowerUpCount(RewardType.Revealing) == 0,"The real Reveal powerup disarms the danger slot");
                        Require(immediate != null && immediate.Dynamite.Disarmed && DynamiteRoundState.IsValid(immediate),"Authoritative locks save immediately without another guess");
                        Tick(.6f); AssertDynamiteCardPinned(game);
                    }
                }
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                PrepareDynamiteFixture(game); var danger = DangerFor(game,definition);
                using (var rules = DangerRules(game,definition,danger))
                {
                    rules.Begin(LevelMechanic.Dynamite); SubmitDynamite(game,new[] {4,5,6});
                    Require(game.Placement.TryRevealAndLockCard(1,1) && danger.Current.Disarmed,"Crystal hint callbacks use the same disarm rule as Reveal");
                }
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                PrepareDynamiteFixture(game); game._saves.GetLevelSaveData().Dynamite.BoardIndex = 2; var danger = DangerFor(game,definition);
                var pair = new PairEffect(game.Round,game.Target,game._saves,game._info,game.Tracker);
                using (var rules = DangerRules(game,definition,danger,pair))
                {
                    var provider = GrowingSnapshot(game,definition);
                    rules.Attach(game.View,() => {
                        if (provider.TryCreateSnapshot(out var current)) Require(DynamiteRoundState.IsValid(current),"Every intermediate Pair/Bomb checkpoint has a valid danger slot");
                    });
                    rules.Begin(LevelMechanic.Dynamite); Require(pair.TryApply(),"Pair may precede Bomb on a danger level");
                    var bomb = new BombPowerUpController(game.PowerMessages,game.Board,game.Placement,game.Target,game._info,game._counts,game.Round); Field(bomb,"_dynamite",danger); Field(bomb,"_pair",pair);
                    Field(game.Tracker,"_bombPowerUpCount",1); game._powers.Refresh();
                    Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Bomb)); Tick(1.2f);
                    Require(game._counts.GetBoardHolderCount() == 2 && danger.Current.BoardIndex < 2 && danger.Current.Remaining == 4,"Bomb reconciles a removed hidden danger slot without spending a move");
                    Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out var saved) && DynamiteRoundState.IsValid(saved),"Bomb and danger state checkpoint together");
                }
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                PrepareDynamiteFixture(game); var danger = DangerFor(game,definition);
                using (var rules = DangerRules(game,definition,danger))
                {
                    rules.Begin(LevelMechanic.Dynamite); int wins = 0; game.Results.LevelSuccessEvent += (s,e) => wins++;
                    SubmitDynamite(game,new[] {1,2,3}); Require(wins == 1 && danger.Current.Disarmed && !danger.Current.Exploded,"A complete answer always wins and never explodes");
                }
            }
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,definition))
            {
                PrepareDynamiteFixture(game); var danger = DangerFor(game,definition);
                using (var rules = DangerRules(game,definition,danger))
                {
                    rules.Begin(LevelMechanic.Dynamite); SubmitDynamite(game,new[] {4,5,6});
                    foreach (float width in new[] {320f,375f,430f})
                    foreach (float height in new[] {568f,667f,812f})
                    {
                        ((RectTransform)canvas.transform).sizeDelta = new Vector2(width,height);
                        canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); game.Board.RefreshLayout();
                        var badge = game.Board.GetRectTransformOfGarden(1).GetComponentInParent<BoardHolderView>().transform.Find("DynamiteBadge") as RectTransform;
                        Require(badge != null && badge.rect.width == 48 && badge.rect.height == 28 && badge.gameObject.activeSelf,"Attention badge uses the reserved area below its slot");
                        Require(badge.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(graphic => !graphic.raycastTarget),"Badge graphics never block board/card touches");
                        var holder = badge.parent as RectTransform;
                        var boardArea = canvas.GetComponentInChildren<BoardAreaView>(true).GetRectTransform();
                        var badgeBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(boardArea,badge);
                        var corners = new Vector3[4]; holder.GetWorldCorners(corners);
                        float holderBottom = corners.Min(corner => boardArea.InverseTransformPoint(corner).y);
                        Require(badgeBounds.max.y > holderBottom && badgeBounds.min.y < holderBottom && badgeBounds.min.y >= boardArea.rect.yMin-.1f,"Badge straddles the holder's lower edge and stays clear of fruit choices");
                        var metrics = new FruitLabLayoutMetrics(width,height,true,true);
                        Require(metrics.HistoryRowHeight*6+metrics.HistorySpacing*7 <= metrics.HistoryHeight+.1f,"Six guess rows still fit with the dynamite footer");
                        var count = badge.Find("Moves").GetComponent<TMPro.TMP_Text>(); count.ForceMeshUpdate(); Require(!count.isTextOverflowing && count.text == "3","Countdown remains legible");
                        layouts++;
                    }
                    ((RectTransform)canvas.transform).sizeDelta = new Vector2(375,812); canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); game.Board.RefreshLayout();
                    Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-dynamite.png");
                    Field(game.Tracker,"_gameOption",GameOption.MultiPlayer);
                    Require(danger.Current == null && !danger.Resolve(new GuessEvaluation(2,new[] {4,5,6},new[] {1,2,3})),"Dynamite does not affect multiplayer");
                }
            }
            Require(errors.Count == 0 && profile == Profile(),"No Unity/tween errors or actual PlayerPrefs changes: "+string.Join("; ",errors));
            File.WriteAllText(Folder+"/dynamite-review.json",JsonConvert.SerializeObject(new { passed = true,limit = 4,layouts,resumeCheckpoints = checkpoints.Count,
                checks = new[] {"actual scene DI", "accepted guesses only", "first guess counts", "fourth move failure", "fourth move correct disarms", "locked card remains on board throughout return animations and resume", "visible initial counter and running resume", "Reveal and crystal lock path", "life cannot extend deadline", "Bomb reconciliation including Pair checkpoints", "no repeated fail callback", "retry resets", "complete answer wins", "badge overlaps lower board edge", "exhausted badge hidden", "compact nonblocking badge", "existing test cycle saves", "multiplayer isolated", "unchanged PlayerPrefs"}
            },Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = previous; Application.logMessageReceived -= collect; }
    }

    private static void PrepareDynamiteFixture(Fixture game)
    {
        Field(game.Tracker,"_levelId",16); game.BuildActors(); game.Gate.Clear();
        game.Target.RevealMultiplayerAnswer(new[] {1,2,3}); game._saves.GetLevelSaveData().Dynamite.BoardIndex = 1;
    }
    private static void LoadDynamiteFixture(Fixture game,LevelSaveData saved)
    {
        Field(game.Tracker,"_levelId",16); var loaded = JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(saved));
        ((IGameSaveService)Get(game,"_memory")).Save(loaded); game._saves.SetLevelSaveDataAsSaved(loaded);
        game.Round.Initialize(); game.BuildActors(); game.Results.TryAddTriedCards(); game.Gate.Clear();
    }
    private static DynamiteService DangerFor(Fixture game,LevelData definition) => new DynamiteService(new TestLevel(definition),game.Tracker,game._saves,game.Target,game._info,game.Placement,game._guess);
    private static LevelMechanicsController DangerRules(Fixture game,LevelData definition,DynamiteService danger,PairEffect pair = null)
    {
        var helper = new HelperEffect(game.Round,game.Target,game._saves); Field(helper,"_split",Get(game.Results,"_split"));
        var rules = new LevelMechanicsController(game.Results,helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker,pair,(BoardSplitService)Get(game.Results,"_split"),game.Board,null,danger);
        Field(rules,"_levels",new TestLevel(definition)); rules.Attach(game.View,null); return rules;
    }
    private static void AssertDynamiteCardPinned(Fixture game,int frame = -1)
    {
        var card = game.Initial.GetRectTransformOfCardItem(1);
        var holder = game.Board.GetRectTransformOfGarden(1);
        var query = (IBoardPlacementQuery)Get(game.Placement,"_boardPlacementQuery");
        Require(card.IsChildOf(holder) && card.localPosition.sqrMagnitude < .01f && query.GetPlacedCardIndexesOnBoard()[1] == 1,
            "A solved dynamite fruit must stay physically and logically on the board after every animation frame; frame="+frame+", parent="+card.parent?.name+", expected="+holder.name+", local="+card.localPosition+", occupied="+query.GetPlacedCardIndexesOnBoard()[1]+", locked="+game._info.GetCardItemInfoList()[1].isLocked+", safe="+game._saves.GetLevelSaveData().Dynamite.Disarmed+", guesses="+game.Round.GetTriedCardsList().Count);
    }
    private static void SubmitDynamite(Fixture game,int[] cards,float settleTime = .4f)
    {
        var query = (IBoardPlacementQuery)Get(game.Placement,"_boardPlacementQuery");
        for (int i = 0; i < cards.Length; i++)
            if (query.GetPlacedCardIndexesOnBoard()[i] != cards[i]-1) Require(game.Placement.TryPlaceCardOnBoard(cards[i]-1,i),"Place a dynamite trial guess");
        game.Check(); if (settleTime > 0) Tick(settleTime);
    }

}

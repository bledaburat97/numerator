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
                    container.Bind<IGameSaveService>().To<GameSaveService>().AsSingle();
                    container.Bind<IHapticController>().To<HapticController>().AsSingle();
                    container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
                    var installer = Find<GameMonoInstaller>(scene);
                    typeof(MonoInstallerBase).GetProperty("Container", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(installer, container);
                    installer.InstallBindings();
                    container.Resolve(typeof(ILevelFlowOrchestrator));
                    container.Resolve(typeof(SinglePlayerSession)); container.Resolve(typeof(MultiplayerSession));
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
                Require(game._area.GetHistoryEntry(0).reviewStates[0] == CardResultState.Correct
                    && game._area.GetHistoryEntry(0).reviewStates[1] == CardResultState.Misplaced,
                    "End review persists correct and misplaced states for offscreen rows");
                Require(area.AllocatedRowCount == allocated, "Scrolling and review reuse the existing row pool");
                game._area.RemoveResultBlocks();
                Require(area.VisibleRowCount == 0 && game._area.HistoryCount == 0, "Rematch clears history and releases visible rows");
                for (int i = 0; i < 8; i++) game._area.AddResultBlock(new ResultBlockModel { finalNumbers = new List<int> { 2, 1 } });
                Require(area.GetComponentsInChildren<ResultBlockView>().All(row => !row.IsSplit && row.IsLocalGuess == null
                    && !row.GetComponentsInChildren<Transform>().Any(t => t.name == "HelperAbsent")), "New rounds clear old groups, players, hints and review frames");
                using (var notes = new CardAnnotationController(game.Initial, game.Placement, game.UI, canvas.GetComponent<FruitLabGameLayout>(), game.Gate, null, () => true))
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
                var title = FruitLabStyle.CreateText("Title", root.transform, 18, FruitLabStyle.Mint);
                title.fontStyle = FontStyles.Bold; title.alignment = TextAlignmentOptions.MidlineLeft;
                var body = FruitLabStyle.CreateText("Body", root.transform, 13, FruitLabStyle.Text);
                body.fontStyle = FontStyles.Normal;
                title.enableAutoSizing = true; title.fontSizeMin = 15; title.fontSizeMax = 18;
                body.alignment = TextAlignmentOptions.TopLeft; body.textWrappingMode = TextWrappingModes.Normal;
                body.overflowMode = TextOverflowModes.Overflow;
                var next = Button(root.transform, "NextButton", true); next.SetText("Devam et");
                var secondary = Button(root.transform, "SecondaryButton", false); secondary.SetText("Kapat"); secondary.SetButtonStatus(false);
                Set(view, "rectTransform", root.transform); Set(view, "text", body); Set(view, "title", title);
                Set(view, "nextButton", next); Set(view, "secondaryButton", secondary);
                view.Show("Meyvelerin yerini bul", "İki meyveyi doğru sıraya yerleştir.", false);
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

    public static void ReviewCardAnnotations(Canvas canvas, Camera camera)
    {
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || message.Contains("error inside a tween callback")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        UpdateType update = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        try
        {
            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas, camera))
            {
                game.BuildActors(); game.Gate.Clear();
                var layout = canvas.GetComponent<FruitLabGameLayout>();
                bool playable = true;
                using (var notes = new CardAnnotationController(game.Initial, game.Placement, game.UI, layout, game.Gate, null, () => playable))
                {
                    Require(game.Initial.TryGetCardItem(0, out var card), "A normal card exists");
                    var view = (NormalCardItemView)card.GetView();
                    Require(notes.TrySelect(0), "An unlocked card can enter annotation mode"); notes.Cancel();
                    card.GetCardMoveHandler().OnPointerDown(null); Tick(.4f);
                    Require(!notes.IsSelecting, "A short hold cannot open annotations");
                    Tick(.2f); Require(notes.IsSelecting, "A half-second hold opens annotations");
                    card.GetCardMoveHandler().OnPointerUp(null);
                    Require(game.Placement.GetCardsOnBoard().Count == 0, "Releasing a long press cannot place a card");
                    Layer(canvas.transform); game.AssertHit((RectTransform)notes.GetButton(0).transform, typeof(Button));
                    notes.GetButton(0).onClick.Invoke();
                    Require(view.Annotation == CardAnnotation.Excluded && !notes.IsSelecting, "Red marks the fruit and closes selection");
                    var cross = (RectTransform)view.transform.Find("AnimationProvider/AnnotationCross");
                    Require(cross.GetComponent<Image>() == null && cross.GetComponentInChildren<FruitLabGlyph>().rectTransform.rect.width >= view.GetRectTransform().rect.width * .9f,
                        "The red mark is a large plain cross without a background plate");
                    card.GetCardMoveHandler().OnPointerDown(null); card.GetCardMoveHandler().OnPointerUp(null); Tick(.4f);
                    Require(game.Placement.GetCardsOnBoard().Count == 1 && view.Annotation == CardAnnotation.Excluded, "A marked fruit stays movable and keeps its note");
                    card.GetCardMoveHandler().OnPointerDown(null); Tick(1.1f); card.GetCardMoveHandler().OnPointerUp(null);
                    notes.GetButton(2).onClick.Invoke();
                    Require(view.Annotation == CardAnnotation.Candidate && !card.GetCardMoveHandler().IsMovementLocked(), "Green annotates a board card without fixing it");
                    Require(view.GetComponentsInChildren<Image>(true).Any(image => image.name == "ResultOutline" && image.gameObject.activeSelf), "Green uses the existing result outline");
                    card.GetCardMoveHandler().OnPointerDown(null); Tick(1.1f); card.GetCardMoveHandler().OnPointerUp(null);
                    notes.GetButton(1).onClick.Invoke(); Require(view.Annotation == CardAnnotation.None, "Yellow clears both annotation types");
                    card.GetCardMoveHandler().OnPointerDown(null); Tick(.3f); game.UI.TriggerResetNumbers(); Tick(1.1f);
                    card.GetCardMoveHandler().OnPointerUp(null);
                    Require(!notes.IsSelecting && game.Placement.GetCardsOnBoard().Count == 0, "Resetting during a hold prevents delayed selection and accidental release taps");
                    notes.TrySelect(0); notes.Apply(CardAnnotation.Excluded); notes.TrySelect(0);
                    ExecuteEvents.Execute(notes.Toolbar.parent.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
                    Require(!notes.IsSelecting && view.Annotation == CardAnnotation.Excluded, "An outside press dismisses without changing the previous note");
                    Require(game.Placement.TryRevealAndLockCard(0, 0, false), "The real reveal path fixes a card to the board");
                    Require(view.Annotation == CardAnnotation.None && !notes.TrySelect(0), "Hints clear notes and fixed cards cannot be selected");
                    view.SetResultOutline(CardResultState.Correct); view.SetAnnotation(CardAnnotation.Excluded);
                    Require(view.Annotation == CardAnnotation.None, "Final truth takes precedence over annotations");
                    var restored = layout.PowerUpArea.rect.height;
                    game.View.SetOpponentInfoStatus(true); layout.ApplyLayout();
                    Require(!layout.PowerUpArea.gameObject.activeSelf, "Multiplayer normally hides the powerup area");
                    Require(notes.TrySelect(1) && layout.PowerUpArea.gameObject.activeSelf && layout.PowerUpArea.rect.height > 0,
                        "Multiplayer temporarily opens a visible annotation row");
                    notes.Cancel(); Require(!layout.PowerUpArea.gameObject.activeSelf, "Dismissing restores the multiplayer layout");
                    game.View.SetOpponentInfoStatus(false); layout.ApplyLayout();
                    Require(layout.PowerUpArea.gameObject.activeSelf && Mathf.Approximately(restored, layout.PowerUpArea.rect.height), "Single-player restores the original powerup row");
                    game.Gate.Block(); Require(!notes.TrySelect(1), "Mandatory tutorial steps cannot be interrupted by annotations"); game.Gate.Clear();
                    notes.TrySelect(1); playable = false; notes.Tick();
                    Require(!notes.IsSelecting && !notes.TrySelect(1), "Ending or transitioning a match closes the annotation controls"); playable = true;
                    notes.TrySelect(2); notes.Apply(CardAnnotation.Excluded);
                    notes.TrySelect(3); notes.Apply(CardAnnotation.Candidate);
                    notes.TrySelect(4); Layer(canvas.transform); Draw(camera);
                    var before = RenderTexture.active; RenderTexture.active = camera.targetTexture;
                    var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
                    try { image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply(); File.WriteAllBytes(Folder + "/preview-card-annotations.png", image.EncodeToPNG()); }
                    finally { Object.DestroyImmediate(image); RenderTexture.active = before; }
                }
            }
            Require(errors.Count == 0, "Annotation review produces no Unity errors: " + errors.FirstOrDefault());
            File.WriteAllText(Folder + "/card-annotations-review.json", "{\"passed\":true,\"single_and_multiplayer_layout\":true,\"real_reveal_path\":true,\"outside_raycast\":true,\"unity_errors\":0}");
        }
        finally { DOTween.defaultUpdateType = update; Application.logMessageReceived -= collect; }
    }

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
                game.AssertNoSkip(); game.MenuAndResume();
                Require(game.Step == 0, "Leaving before a guess restarts level-one training with the same answer");
                Capture(camera, Folder + "/preview-tutorial-placement.png");
                game.AssertHit(game.Initial.GetRectTransformOfCardItem(0), typeof(IPointerUpHandler));
                Require(!game.Placement.TryPlaceCardOnBoard(1, 0) && !game.Placement.TryPlaceCardOnBoard(0, 1), "Only the instructed fruit and slot can be used");
                game.Check(); Require(game.Round.GetTriedCardsList().Count == 0, "Premature guess is blocked");
                game.Place(0); game.Place(1); Require(game.Step == 2, "First two placements advance to submit");
                game.AssertHit(game.UI.GetCheckButtonRectTransform(), typeof(IPointerDownHandler));
                game.Check(); game.MenuAndResume(); Tick(.5f); Require(game.Step == 3, "Wrong-guess return must finish before explanation");
                game.Feedback(0, new[] { 1, 2 }, 0, 1);
                Capture(camera, Folder + "/preview-tutorial-feedback.png");
                game.Next(); game.Place(2); game.Place(0); game.Check(); Tick(.5f);
                game.MenuAndResume(); Require(game.Step == 7, "Second explanation follows the second guess"); game.Feedback(1, new[] { 3, 1 }, 1, 0);
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
                foreach (var type in new[] { GameUIButtonType.BombPowerUp, GameUIButtonType.RevealingPowerUp, GameUIButtonType.LifePowerUp })
                {
                    int uses = 0;
                    Require(game.Gate.TryExplain(type, () => uses++), "First powerup encounter must show an explanation: " + type);
                    Require(uses == 0 && game.Message != null, "Explanation cannot spend stock or use a powerup early");
                    game.Next(); Require(uses == 1 && game.Message == null, "Continue resumes the original powerup action exactly once");
                    Require(!game.Gate.TryExplain(type, () => uses++), "Acknowledged powerup intro must not repeat");
                }
                flags.Remove(TutorialProgress.Reveal); int cancelledUses = 0;
                Require(game.Gate.TryExplain(GameUIButtonType.RevealingPowerUp, () => cancelledUses++), "Unseen reveal can be explained");
                game.Next(); Require(cancelledUses == 1 && !game.Gate.IsRestricted, "Tamam acknowledges the first-use tip and continues the action");
                flags.Remove(TutorialProgress.Reveal); Field(game.Tracker, "_revealingPowerUpCount", 0);
                Require(!game.Gate.TryExplain(GameUIButtonType.RevealingPowerUp, () => cancelledUses++), "No-stock powerups do not open a use tutorial");
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
                    "completion only on win", "no skip or replay controls", "old skipped flag recovery", "menu exit and resume with 0/1/2 guesses", "pending animation cancellation on menu exit", "fixed level-one answer and legacy save policy", "three powerup intros before action",
                    "first-use cancel and zero-stock guards", "first crystal-hint round",
                    "coin/crystal intro", "320/375/430 width layouts", "unchanged real PlayerPrefs", "no Unity/tween errors" },
                flags, player_preferences_written = false
            }, Newtonsoft.Json.Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = update; Application.logMessageReceived -= collect; }
        Debug.Log("FRUIT_LAB_TUTORIAL_PASS: scripted real placements/results, mandatory completion/menu resume/fixed answer, first-use tips, mobile layouts and player-data isolation.");
    }

    private sealed class Fixture : IDisposable
    {
        public readonly Canvas Canvas; public readonly Camera Camera;
        public readonly TutorialInputGate Gate = new TutorialInputGate();
        public readonly LevelTracker Tracker; public readonly TargetNumberCreator Target;
        public readonly RoundStateManager Round; public readonly BoardAreaController Board;
        public readonly InitialCardAreaController Initial; public readonly CardPlacementCoordinator Placement;
        public ResultManager Results; public PowerUpMessageController PowerMessages; public RevealingPowerUpController Reveal;
        public readonly GameUIController UI; public readonly GameUIView View; public readonly TutorialBootstrapper Bootstrap;
        private readonly CardInteractionManager _interaction;
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
            var boardLayout = new BoardLayoutManager(boardView,sizes); Field(boardLayout,"_split",split);
            Board = new BoardAreaController(boardView, boardLayout, counts, indexes, indexes); Field(Board,"_split",split);
            Target = _objects.AddComponent<TargetNumberCreator>();
            Field(Target, "_levelDataCreator", level); Field(Target, "_gameSaveService", _memory); Field(Target, "_levelTracker", Tracker); Field(Target,"_saveData",saves);
            Field(split,"_save",saves); Field(split,"_target",Target); Field(split,"_round",Round);
            var initialView = canvas.GetComponentInChildren<InitialCardAreaView>(true); var layout = new InitialCardAreaLayoutManager(initialView, sizes);
            var haptic = new HapticController();
            Initial = new InitialCardAreaController(initialView, Board, counts, level, saves, Target, layout, new InitialCardAreaFactory(initialView, haptic, layout));
            _info = new CardItemInfoManager(saves, null, counts);
            var container = new DiContainer(); container.Bind<IHapticController>().FromInstance(haptic); container.Bind<TutorialInputGate>().FromInstance(Gate);
            container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            _buttons = container.Resolve<BaseButtonControllerFactory>();
            _powers = new GamePowerUpAreaController(_buttons, canvas.GetComponentInChildren<PowerUpListHolderView>(true), Tracker, Round, _info, counts);
            Field(_powers,"_split",split);
            View = canvas.GetComponentInChildren<GameUIView>(true);
            UI = new GameUIController(_buttons, Tracker, null, haptic, View, _powers); Field(UI, "_tutorialGate", Gate);
            var fade = new FadePanelController(canvas.GetComponentInChildren<FadePanelView>(true));
            var powerMessages = PowerMessages = new PowerUpMessageController(_powers, fade, Tracker, Round, _info, counts); Field(powerMessages, "_tutorialGate", Gate);
            Field(powerMessages,"_split",split);
            var reveal = Reveal = new RevealingPowerUpController(Board, powerMessages, Tracker, _powers, haptic, Target);
            var locator = new CardItemLocator(Board, UI); locator.Initialize();
            Placement = new CardPlacementCoordinator(Board, locator, UI, indexes, indexes, reveal, Initial, _info, Gate);
            _area = new ResultAreaController(canvas.GetComponentInChildren<ResultAreaView>(true));
            var results = Results = new ResultManager(UI, _area, Target, indexes, Round, _powers);
            Field(results,"_split",split);
            _guess = new GuessManager(results, new LifeBarController(canvas.GetComponentInChildren<LifeBarView>(true)), Target, _info, powerMessages, UI, Round);
            _interaction = new CardInteractionManager(UI, new BoardHolderClickRouter(Board, reveal), Initial, haptic, Placement);
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
            var hits = new List<RaycastResult>(); Canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
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
        public void MenuAndResume()
        {
            AssertNoSkip();
            Require(Gate.AllowsButton(GameUIButtonType.Settings), "Settings stays available during the mandatory lesson");
            AssertHit(View.GetSettingsButton().GetButtonRectTransform(), typeof(IPointerDownHandler));
            SettingsPopupView popup = null; var scenes = new List<string>();
            EventHandler open = (sender, args) => {
                popup = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SettingsPopupView>("Assets/Prefabs/SettingsPopup.prefab"), Canvas.GetComponentInChildren<GamePopupCreator>(true).transform, false);
                Layer(popup.transform);
                new SettingsPopupController(scene => scenes.Add(scene), () => { }).Initialize(popup, () => { }, () => {
                    var provider = new GameSaveSnapshotProvider(Target, _info, new PlayingFlow(), _counts, Round);
                    if (provider.TryCreateSnapshot(out var snapshot)) _memory.Save(snapshot); else _memory.DeleteSave();
                }, () => throw new Exception("Menu cannot delete a saved lesson"), Tracker, _buttons);
            };
            UI.OpenSettings += open;
            try
            {
                Click((BaseButtonView)View.GetSettingsButton());
                Require(popup != null && popup.transform.Find("Image/ReplayButton") == null, "Settings opens with no replay action");
                Click((BaseButtonView)popup.GetMenuButton());
                Require(scenes.SequenceEqual(new[] { "Menu" }), "Menu action saves then requests the Menu scene");
            }
            finally { UI.OpenSettings -= open; if (popup != null) Object.DestroyImmediate(popup.gameObject); }
            int guesses = Round.GetTriedCardsList().Count;
            Require(!TutorialProgress.Seen(TutorialProgress.First), "Returning to menu cannot mark training completed");
            Require(guesses == 0 ? !_memory.HasSavedGame() : FirstLevelTutorialController.CanResume(_memory.Data, 6), "Actual menu snapshot is safe to resume");
            Bootstrap.Cancel(); UI.TriggerResetNumbers(); Tick(.5f);
            Require(Message == null && !Gate.IsRestricted && !TutorialProgress.Seen(TutorialProgress.First), "Menu exit cancels pending callbacks without completing training");
            if (_memory.HasSavedGame()) _saves.SetLevelSaveDataAsSaved(_memory.Data); else _saves.CreateDefaultLevelSaveData();
            Round.Initialize(); _info.Initialize(); Target.SetTargetNumber(2);
            Bootstrap.PrepareRound(); Bootstrap.StartRound(); Tick(.001f);
            Require(Bootstrap.IsFirstActive && Step == (guesses == 0 ? 0 : guesses == 1 ? 3 : 7)
                && Target.GetTargetCardsList().SequenceEqual(new[] { 4, 1 }), "Return to level one resumes the guided lesson with portakal/elma");
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
            View.GetPairView()?.Unbind(); _guess.Dispose();
            Bootstrap.Dispose(); _interaction.Unsubscribe(); Tick(2f);
            Object.DestroyImmediate(_events); Object.DestroyImmediate(_objects);
        }
    }

    public static string ResetLocalProgress()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play mode before the requested profile reset");
        string[] ints = new[] { "level_id", "star_count", "gift_star_count", "revealing_power_up_count", "life_power_up_count", "bomb_power_up_count", "helper_power_up_count", "pair_power_up_count", "split_power_up_count", "reward_type" }.Concat(TutorialProgress.Keys).ToArray();
        string[] strings = { "saved_game_key", "star_count_of_levels" };
        string backup = Folder + "/PlayerPrefsBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json";
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));
        File.WriteAllText(backup, Newtonsoft.Json.JsonConvert.SerializeObject(new {
            company = PlayerSettings.companyName, product = PlayerSettings.productName,
            ints = ints.ToDictionary(key => key, key => new { exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) }),
            strings = strings.ToDictionary(key => key, key => new { exists = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetString(key) })
        }, Newtonsoft.Json.Formatting.Indented));
        int coins = PlayerPrefs.GetInt("star_count"), crystals = PlayerPrefs.GetInt("gift_star_count");
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
    private sealed class PlayingFlow : ILevelFlowOrchestrator
    {
        public void Begin(LevelEntryMode mode) { } public void Retry() { } public void GoToNextLevel() { }
        public bool IsGameOver() => false; public bool IsTransitioning() => false;
        public LevelFlowState GetState() => LevelFlowState.Playing;
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
        view.name = name; view.ApplyLabActionStyle(primary); return view;
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

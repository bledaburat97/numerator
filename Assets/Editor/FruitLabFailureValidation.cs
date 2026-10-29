using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using Game;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

#pragma warning disable CS0067
public static class FruitLabFailureValidation
{
    public static void ValidateLogic()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode before failure regression checks.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert
                || message.Contains("error inside a tween callback") || message.Contains("missing target or field")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        try
        {
            CheckBoundary(4, 3, true, false);
            CheckBoundary(2, 3, true, true);
            CheckBoundary(1, 3, false, false);
            CheckBoundary(1, 2, false, false);
            CheckDestroyedOwner(false);
            CheckDestroyedOwner(true);
            CheckHit(false);
            CheckHit(true);
            FruitLabCompletionValidation.ValidateFeedback();
            Require(errors.Count == 0, "Tween lifecycle errors: " + string.Join("; ", errors));
        }
        finally
        {
            Application.logMessageReceived -= collect;
            EditorSceneManager.CloseScene(scene, true);
            if (previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
        Debug.Log("FRUIT_LAB_FAIL_CHECKS_PASS: first/second/final crystal behavior, duplicate final guess, destroyed-token callbacks and hint-owner cleanup.");
    }

    private static void CheckBoundary(int remaining, int crystalCount, bool expectedHint, bool expectedReveal)
    {
        var round = new RoundStateManager(null, null);
        Field(round, "_maxGuessCount", 8); Field(round, "_remainingGuessCount", remaining);
        var rewards = new List<LifeBarRewardInfo>();
        int[] boundaries = { 0, 1, 3 };
        for (int i = 0; i < 3; i++) rewards.Add(new LifeBarRewardInfo(boundaries[i],
            crystalCount < 3 - i ? LifeBarRewardType.Coin : LifeBarRewardType.Crystal, remaining > boundaries[i]));
        Field(round, "_lifeBarRewardInfoList", rewards);
        var result = new ResultSource();
        var token = new TokenProbe();
        var life = new LifeProbe(token);
        var manager = new GuessManager(result, life, null, null, new PowerSource(), new UiProbe(), round);
        int hints = 0, failures = 0;
        bool reveal = false;
        manager.HintRewardTokenEvent += (sender, args) => { hints++; reveal = args.CanRevealCard; };
        manager.LevelFailEvent += (sender, args) => failures++;
        Vector3 position = token.Rect.position;
        result.Wrong();
        Require(hints == (expectedHint ? 1 : 0), "Only non-terminal crystal boundaries emit hints");
        if (expectedHint) Require(reveal == expectedReveal, "First crystal eliminates; second reveals");
        if (remaining == 1)
        {
            Require(token.Fades == 0 && token.Rect.position == position, "Final crystal must not fade or move to a card");
            Require(token.Destroyed == (crystalCount == 3), "Final crystal is removed at its boundary; coin behavior stays unchanged");
            life.Progress.Complete(true);
            result.Wrong();
            Require(failures == 1 && round.GetRemainingGuessCount() == 0, "Terminal guesses cannot trigger a second failure or negative lives");
        }
        else life.Progress.Kill();
        Object.DestroyImmediate(token.Rect.gameObject);
    }

    private static void CheckDestroyedOwner(bool destroyTarget)
    {
        var target = new GameObject("HintTarget", typeof(RectTransform)).GetComponent<RectTransform>();
        var root = new GameObject("CrystalOwner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CrystalView));
        var token = root.GetComponent<CrystalView>();
        var serialized = new SerializedObject(token);
        serialized.FindProperty("rectTransform").objectReferenceValue = root.GetComponent<RectTransform>();
        serialized.FindProperty("image").objectReferenceValue = root.GetComponent<Image>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
        IRewardTokenView heldInterface = token;
        Action delayedDestroy = heldInterface.Destroy;
        int hits = 0;
        Sequence animation = new RewardTokenAnimationManager().RevealCard(heldInterface, target, () => hits++);
        animation.SetUpdate(UpdateType.Manual).Play();
        DOTween.ManualUpdate(.05f, .05f);
        Require(root.transform.parent == target, "Hint token must first attach to its target card");
        if (destroyTarget) Object.DestroyImmediate(target.gameObject); else Object.DestroyImmediate(root);
        delayedDestroy();
        Require(heldInterface.GetRectTransform() == null, "Destroyed interface wrappers must not access Transform");
        DOTween.ManualUpdate(3f, 3f);
        Require(!animation.IsActive(), "Destroying token or its card must cancel the whole hint sequence before its next update (target=" + destroyTarget + ")");
        Require(hits == 0, "Cancelled hints cannot apply a late action to a removed card");
        if (target != null) Object.DestroyImmediate(target.gameObject);
    }

    private static void CheckHit(bool reveal)
    {
        var target = new GameObject("ValidHintTarget", typeof(RectTransform)).GetComponent<RectTransform>();
        var token = new TokenProbe();
        int hits = 0;
        var manager = new RewardTokenAnimationManager();
        Sequence animation = reveal ? manager.RevealCard(token, target, () => hits++) : manager.DestroyCard(token, target, () => hits++);
        animation.Complete(true);
        Require(hits == 1 && token.Destroyed, "Valid hint applies its action once, then disposes its token");
        Object.DestroyImmediate(target.gameObject);
        if (token.Rect != null) Object.DestroyImmediate(token.Rect.gameObject);
    }

    public static void ValidatePanelBounds(Canvas canvas)
    {
        var popup = canvas.GetComponentInChildren<LevelFinishPopupView>(true);
        var board = canvas.GetComponentInChildren<BoardAreaView>(true).GetRectTransform();
        var results = canvas.GetComponentInChildren<ResultAreaView>(true);
        var scroll = results.GetComponentInParent<ScrollRect>();
        Rect p = Bounds((RectTransform)popup.transform, (RectTransform)canvas.transform);
        Require(!p.Overlaps(Bounds(board, (RectTransform)canvas.transform)), "Fail panel must not overlap the answer board");
        Require(!p.Overlaps(Bounds(scroll.viewport, (RectTransform)canvas.transform)), "Fail panel must not overlap guess history");
        var tray = (RectTransform)canvas.GetComponentInChildren<InitialCardAreaView>(true).transform;
        Rect t = Bounds(tray, (RectTransform)canvas.transform);
        Require(p.yMin >= t.yMin && p.yMax <= t.yMax, "Fail panel must fit within the fruit selection row");
    }

    public static void ValidateReview(Canvas canvas, Camera camera, bool success = false)
    {
        if (success) FruitLabCompletionValidation.ValidatePanelBounds(canvas); else ValidatePanelBounds(canvas);
        var results = canvas.GetComponentInChildren<ResultAreaView>(true);
        var scroll = results.GetComponentInParent<ScrollRect>();
        Require(scroll.enabled && scroll.vertical && results.GetCanvasGroup().blocksRaycasts, "History remains interactive after failure");
        var eventObject = new GameObject("FailReviewEvents", typeof(EventSystem));
        SceneManager.MoveGameObjectToScene(eventObject, canvas.gameObject.scene);
        var pointer = new PointerEventData(eventObject.GetComponent<EventSystem>()) {
            position = RectTransformUtility.WorldToScreenPoint(camera, scroll.viewport.TransformPoint(scroll.viewport.rect.center)),
            scrollDelta = Vector2.up * 3f
        };
        var hits = new List<RaycastResult>();
        canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
        Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IScrollHandler>(hits[0].gameObject) == scroll.gameObject,
            "Pointer over history must reach ScrollRect while fail panel is open; position=" + pointer.position
            + "; hits=" + string.Join(", ", hits.ConvertAll(hit => hit.gameObject.name + " in " + hit.gameObject.transform.parent.name)));
        float before = scroll.verticalNormalizedPosition;
        ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.scrollHandler);
        Require(scroll.verticalNormalizedPosition > before + .001f, "Mouse/touch scroll must move earlier guesses into view");
        scroll.verticalNormalizedPosition = 0f;
        Object.DestroyImmediate(eventObject);
        Debug.Log((success ? "FRUIT_LAB_SUCCESS_REVIEW_PASS" : "FRUIT_LAB_FAIL_REVIEW_PASS") + ": panel clears board/history; history raycast and scroll verified.");
    }

    public static Rect Bounds(RectTransform rect, RectTransform relative)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        Vector3 min = relative.InverseTransformPoint(corners[0]), max = relative.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private static void Field(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Failure regression: " + message); }

    private class ResultSource : IResultManager
    {
        public event EventHandler LevelSuccessEvent;
        public event EventHandler WrongGuessEvent;
        public void Wrong() => WrongGuessEvent?.Invoke(this, EventArgs.Empty);
        public void TryAddTriedCards() { }
    }
    private class PowerSource : IPowerUpMessageController
    {
        public event EventHandler RemoveBoardHolderEvent;
        public event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        public event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        public event EventHandler AddLifeEvent;
        public void CloseActivePowerUp() { }
    }
    private class TokenProbe : IRewardTokenView
    {
        public readonly RectTransform Rect = new GameObject("TokenProbe", typeof(RectTransform)).GetComponent<RectTransform>();
        public bool Destroyed; public int Fades;
        public TokenProbe() { Rect.sizeDelta = Vector2.one * 16f; }
        public void SetLocalPosition(Vector2 value) => Rect.localPosition = value;
        public void SetLocalScale(Vector3 value) => Rect.localScale = value;
        public void SetStatus(bool status) { }
        public void SetSize(Vector2 value) => Rect.sizeDelta = value;
        public RectTransform GetRectTransform() => Rect;
        public void SetParent(RectTransform parent) => Rect.SetParent(parent);
        public void Destroy() => Destroyed = true;
        public CurvedAnimationPreset GetCurvedAnimationPreset() => null;
        public Sequence AnimateFadeOut(float duration) { Fades++; return DOTween.Sequence().AppendInterval(duration); }
        public Tween AnimatePunchScale(Vector3 punch, float duration, int vibrato, float elasticity) => DOTween.Sequence().AppendInterval(duration);
    }
    private class LifeProbe : ILifeBarController
    {
        private readonly IRewardTokenView _token;
        public Tween Progress;
        public LifeProbe(IRewardTokenView token) => _token = token;
        public void SetFade(bool isNewGame) { }
        public void DisableProgressBar() { }
        public void ClearBoundaries() { }
        public void ClearLifeBarRewardInfoList() { }
        public void SetLifeBar(int max, IReadOnlyList<LifeBarRewardInfo> rewards, int remaining, bool deferRewardIntroAnimation = false) { }
        public Sequence PlayRewardIntroAnimation() => DOTween.Sequence();
        public Sequence ChangeFade(float duration, float alpha) => DOTween.Sequence();
        public Tween UpdateProgressBar(float target, float duration, Action complete) => Progress = DOTween.Sequence().AppendInterval(duration).AppendCallback(() => complete?.Invoke()).Pause();
        public void SetRewardStatus(bool status, int index, bool keepRewardItemVisibleWhenDisabled = false) { }
        public IRewardTokenView GetRewardToken(int index) => _token;
        public IReadOnlyList<IRewardTokenView> GetActiveRewardTokens(LifeBarRewardType type) => Array.Empty<IRewardTokenView>();
    }
    private class UiProbe : IGameUIController
    {
        public event EventHandler CheckFinalNumbers;
        public event EventHandler NotAbleToCheck;
        public event EventHandler ResetNumbers;
        public event EventHandler OpenSettings;
        public event EventHandler<bool> CardInfoToggleChanged;
        public void Initialize(bool isNewGame) { }
        public void InitializeForMultiplayer() { }
        public RectTransform GetCheckButtonRectTransform() => null;
        public RectTransform GetResetButtonRectTransform() => null;
        public RectTransform GetCardInfoButtonRectTransform() => null;
        public void TriggerResetNumbers() { }
        public void SetAllButtonsUnclickable() { }
        public void SetButtonClickable(bool clickable, GameUIButtonType type) { }
        public Sequence ChangeFadeTopAreaButtons(float duration, float alpha) => DOTween.Sequence();
        public Sequence ChangeFadeMiddleAreaButtons(float duration, float alpha) => DOTween.Sequence();
        public IBaseButtonController GetButton(GameUIButtonType type) => null;
        public Sequence ChangeFadeUserText(float duration, float alpha) => DOTween.Sequence();
        public void RefreshCoinCounter() { }
        public Sequence PlayCoinCounterHit(int count, float duration) => DOTween.Sequence();
        public RectTransform GetCoinImageRectTransform() => null;
    }
}
#pragma warning restore CS0067

public static class FruitLabCompletionValidation
{
    public static void ValidateFeedback()
    {
        var C = CardResultState.Correct;
        var M = CardResultState.Misplaced;
        var A = CardResultState.Absent;
        Check(new[] { 1, 3, 5 }, new[] { 1, 2, 3 }, new[] { C, M, A });
        Check(new[] { 2, 2, 1 }, new[] { 1, 2, 3 }, new[] { A, C, M });
        Check(new[] { 3, 1, 4, 2 }, new[] { 1, 2, 3, 4 }, new[] { M, M, M, M });
        Check(new[] { 1, 5, 3, 7, 4 }, new[] { 1, 2, 3, 4, 5 }, new[] { C, M, C, A, M });
        for (int length = 2; length <= 5; length++)
        {
            var answer = new int[length]; var states = new CardResultState[length];
            for (int i = 0; i < length; i++) { answer[i] = i + 1; states[i] = C; }
            Check(answer, answer, states);
        }
        var state = new GameObject("CompletionFeedbackState");
        state.SetActive(false);
        try
        {
            var tracker = state.AddComponent<LevelTracker>();
            var view = new AreaProbe(state.AddComponent<CanvasGroup>());
            var area = new ResultAreaController(tracker, null, null, view);
            area.AddNewResultBlock(531, 1, 1);
            area.RevealAnswer(new[] { 1, 2, 3 });
            Require(view.Rows[0].Cards[0].State == C && view.Rows[0].Cards[1].State == M && view.Rows[0].Cards[2].State == A,
                "History rows receive per-position feedback on completion");
            Require(view.Review && view.Group.alpha == 1f, "Completed history must remain visible and interactive");
            area.Initialize(false);
            Require(!view.Rows[0].Destroyed, "Review/resume must retain existing guesses");
            area.Initialize(true);
            Require(view.Empty && view.Rows[0].Destroyed, "A fresh round must clear the completed history");
            area.AddNewResultBlock(321, 3, 0);
            foreach (var card in view.Rows[1].Cards) Require(card.State == CardResultState.Unrevealed, "Next round cannot inherit answer highlights");
        }
        finally { Object.DestroyImmediate(state); }
        Debug.Log("FRUIT_LAB_COMPLETION_FEEDBACK_PASS: eight classification cases, duplicate-safe feedback, late reveal and next-round cleanup.");
    }

    private static void Check(int[] guess, int[] answer, CardResultState[] expected)
    {
        var view = new RowProbe();
        var row = new ResultBlockController();
        row.Initialize(view, new ResultBlockModel { finalNumbers = new List<int>(guess) }, 331f, 64f);
        foreach (var card in view.Cards) Require(card.State == CardResultState.Unrevealed, "Only aggregate results may be visible during play");
        row.RevealAnswer(answer);
        row.RevealAnswer(answer);
        for (int i = 0; i < expected.Length; i++) Require(view.Cards[i].State == expected[i], "Feedback classification at position " + i);
    }

    public static void ValidatePanelBounds(Canvas canvas)
    {
        var root = (RectTransform)canvas.transform;
        var popup = canvas.GetComponentInChildren<LevelFinishPopupView>(true);
        Rect panel = FruitLabFailureValidation.Bounds((RectTransform)popup.transform, root);
        var board = canvas.GetComponentInChildren<BoardAreaView>(true).GetRectTransform();
        var history = canvas.GetComponentInChildren<ResultAreaView>(true).GetComponentInParent<ScrollRect>().viewport;
        Require(!panel.Overlaps(FruitLabFailureValidation.Bounds(board, root)), "Win panel must leave the completed board visible");
        Require(!panel.Overlaps(FruitLabFailureValidation.Bounds(history, root)), "Win panel must leave guess history visible");
        Require(panel.yMin >= root.rect.yMin && panel.yMax <= root.rect.yMax, "Win panel must fit the screen");
        foreach (RectTransform child in new[] { popup.GetVerticalCrystalProgressView().GetRectTransform(),
            (RectTransform)popup.transform.Find("RewardStage"),
            (RectTransform)((Component)popup.GetButton(LevelFinishButtonType.Game)).transform,
            (RectTransform)((Component)popup.GetButton(LevelFinishButtonType.Menu)).transform,
            (RectTransform)((Component)popup.GetButton(LevelFinishButtonType.Claim)).transform })
        {
            Rect bounds = FruitLabFailureValidation.Bounds(child, root);
            Require(bounds.xMin >= panel.xMin - .1f && bounds.xMax <= panel.xMax + .1f
                && bounds.yMin >= panel.yMin - .1f && bounds.yMax <= panel.yMax + .1f, "Crystal/reward/buttons must fit inside the compact win panel");
        }
    }

    public static void ValidateBoard(Canvas canvas, int count)
    {
        var cards = canvas.GetComponentInChildren<BoardAreaView>(true).GetComponentsInChildren<NormalCardItemView>();
        Require(cards.Length == count, "Every answer fruit must stay on the board at the end");
        foreach (var card in cards)
        {
            var outline = card.transform.Find("ResultOutline")?.GetComponent<Image>();
            Require(outline != null && outline.gameObject.activeSelf && outline.color == FruitLabStyle.Correct,
                "Every winning board card must have a bright green outline");
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Completion regression: " + message); }
    private sealed class CardProbe : INonDraggableCardItemView
    {
        public CardResultState State;
        public void Init(int number) => State = CardResultState.Unrevealed;
        public void InitLocalScale() { }
        public void SetSize(Vector2 size) { }
        public void SetLocalPosition(Vector3 position) { }
        public void SetAlpha(float alpha) { }
        public void SetResultOutline(CardResultState result) => State = result;
        public void MultiplyPixelsPerUnit() { }
    }
    private sealed class RowProbe : IResultBlockView
    {
        public readonly List<CardProbe> Cards = new List<CardProbe>();
        public bool Destroyed;
        public void Init(NonDraggableCardItemViewFactory factory, float width, float height) { }
        public INonDraggableCardItemView CreateCardItem() { var card = new CardProbe(); Cards.Add(card); return card; }
        public void DisplayFeedback(int correct, int misplaced) { }
        public void SetCardResult(int index, CardResultState result) => Cards[index].SetResultOutline(result);
        public void Destroy() => Destroyed = true;
    }
    private sealed class AreaProbe : IResultAreaView
    {
        public readonly List<RowProbe> Rows = new List<RowProbe>();
        public readonly CanvasGroup Group;
        public bool Empty, Review;
        public AreaProbe(CanvasGroup group) => Group = group;
        public ResultAreaInfo GetResultAreaInfo() => default;
        public IResultBlockView CreateResultBlock() { var row = new RowProbe(); Rows.Add(row); return row; }
        public void SetScrollPositionToBottom() { }
        public CanvasGroup GetCanvasGroup() => Group;
        public float GetWidth() => 331f;
        public float GetRowHeight() => 64f;
        public void SetEmptyState(bool empty) => Empty = empty;
        public void EnableReview() { Review = true; Group.alpha = 1f; }
    }
}

public static class FruitLabRetryValidation
{
    public static void Validate(Canvas canvas, Camera camera)
    {
        var objects = new GameObject("RetryValidationState");
        objects.SetActive(false); // In-memory tracker, with no PlayerPrefs reads/writes.
        SceneManager.MoveGameObjectToScene(objects, canvas.gameObject.scene);
        var events = new GameObject("RetryValidationEvents", typeof(EventSystem));
        SceneManager.MoveGameObjectToScene(events, canvas.gameObject.scene);
        try
        {
            var tracker = objects.AddComponent<LevelTracker>();
            Field(tracker, "_levelId", 11);
            Field(tracker, "_bombPowerUpCount", 2);
            Field(tracker, "_revealingPowerUpCount", 2);
            Field(tracker, "_lifePowerUpCount", 2);
            var round = new RoundStateManager(null, null);
            var container = new DiContainer();
            var haptics = new HapticController();
            container.Bind<IHapticController>().FromInstance(haptics);
            container.BindFactory<IBaseButtonView, Action, IBaseButtonController, BaseButtonControllerFactory>().To<BaseButtonController>();
            var factory = container.Resolve<BaseButtonControllerFactory>();
            var powerView = canvas.GetComponentInChildren<PowerUpListHolderView>(true);
            var powers = new GamePowerUpAreaController(factory, powerView, tracker, round,
                new CardItemInfoManager(null, null, null), new BoardHolderCountManager(null, null));
            powers.Initialize();
            var view = canvas.GetComponentInChildren<GameUIView>(true);
            var ui = new GameUIController(factory, tracker, null, haptics, view, powers);
            var counts = new int[6];
            ui.CheckFinalNumbers += (sender, args) => counts[0]++;
            ui.ResetNumbers += (sender, args) => counts[1]++;
            ui.OpenSettings += (sender, args) => counts[2]++;
            powers.PowerUpClickedEvent += (sender, type) => counts[type == GameUIButtonType.BombPowerUp ? 3 : type == GameUIButtonType.RevealingPowerUp ? 4 : 5]++;
            IBaseButtonView[] buttons = { view.GetCheckButton(), view.GetResetButton(), view.GetSettingsButton(),
                powerView.GetBombPowerUpButton(), powerView.GetRevealingPowerUpButton(), powerView.GetLifePowerUpButton() };
            var pointer = new PointerEventData(events.GetComponent<EventSystem>());
            ui.Initialize(false);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                // Same persistent controllers and entry points used by OnLevelFail
                // and BeginSinglePlayer(Retry); changing alpha alone cannot unlock them.
                ui.SetAllButtonsUnclickable();
                for (int i = 0; i < buttons.Length; i++)
                {
                    Press(buttons[i], pointer, canvas, camera);
                    Require(counts[i] == cycle, "Fail must block gameplay button " + i);
                }
                ui.Initialize(true);
                ui.ChangeFadeTopAreaButtons(.1f, 1f).Complete(true);
                ui.ChangeFadeMiddleAreaButtons(.1f, 1f).Complete(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    Press(buttons[i], pointer, canvas, camera);
                    Require(counts[i] == cycle + 1, "Retry must restore exactly one callback for button " + i);
                }
            }
            // A retry unlocks the round; it must preserve inventory and bomb rules.
            Field(tracker, "_lifePowerUpCount", 0);
            round.AddTriedCards(new List<int> { 1, 2 });
            ui.SetAllButtonsUnclickable();
            ui.Initialize(false);
            Press(buttons[3], pointer, canvas, camera);
            Press(buttons[4], pointer, canvas, camera);
            Press(buttons[5], pointer, canvas, camera);
            Require(counts[3] == 3 && counts[4] == 4 && counts[5] == 3,
                "Used bomb and zero-inventory life must stay unavailable after restoring input");
            // The raised fruit visuals and their holder stamps must pass input to the card.
            var card = canvas.GetComponentInChildren<NormalCardItemView>();
            var hit = Hit(card.GetRectTransform(), pointer, canvas, camera);
            Require(hit != null && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit) == card.gameObject,
                "Fruit card must remain reachable by the raycaster after retry initialization");
            Debug.Log("FRUIT_LAB_RETRY_INPUT_PASS: three fail/retry cycles; check/reset/settings and three powerups; raycasts, one callback per press and availability restrictions verified.");
        }
        finally
        {
            Object.DestroyImmediate(events);
            Object.DestroyImmediate(objects);
        }
    }

    private static void Press(IBaseButtonView view, PointerEventData pointer, Canvas canvas, Camera camera)
    {
        var target = ((Component)view).gameObject;
        var hit = Hit(view.GetButtonRectTransform(), pointer, canvas, camera);
        Require(hit != null && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit) == target,
            "Raycast must reach " + target.name + "; top hit=" + (hit == null ? "none" : hit.name));
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
    }

    private static GameObject Hit(RectTransform target, PointerEventData pointer, Canvas canvas, Camera camera)
    {
        Canvas.ForceUpdateCanvases();
        pointer.position = RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
        var hits = new List<RaycastResult>();
        canvas.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
        return hits.Count == 0 ? null : hits[0].gameObject;
    }
    private static void Field(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Retry regression: " + message); }
}

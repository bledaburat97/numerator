using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public enum CardAnnotation { None, Excluded, Candidate }

    // Local pencil marks only; never written into the answer, lock state or network history.
    public sealed class CardAnnotationController : IDisposable
    {
        private readonly IInitialCardAreaController _cards;
        private readonly ICardPlacementCoordinator _placement;
        private readonly IGameUIController _ui;
        private readonly FruitLabGameLayout _layout;
        private readonly TutorialInputGate _gate;
        private readonly IHapticController _haptic;
        private readonly Func<bool> _canInteract;
        private RectTransform _overlay, _toolbar;
        private readonly Button[] _buttons = new Button[3];
        private readonly Vector3[] _corners = new Vector3[4];
        private NormalCardItemView _selected;
        private ICardMoveHandler _selectedMove;
        private CanvasGroup _powerGroup;
        private float _previousAlpha;
        private bool _previousRaycasts, _previousInteractable;
        public bool IsSelecting => _selected != null;
        public RectTransform Toolbar => _toolbar;
        public Button GetButton(int index) => _buttons[index];

        public CardAnnotationController(IInitialCardAreaController cards, ICardPlacementCoordinator placement,
            IGameUIController ui, FruitLabGameLayout layout, TutorialInputGate gate,
            IHapticController haptic, Func<bool> canInteract)
        {
            _cards = cards; _placement = placement; _ui = ui; _layout = layout;
            _gate = gate; _haptic = haptic; _canInteract = canInteract;
            _placement.SetLongPressHandler(TrySelect);
            _placement.OnCardDragStartedEvent += CancelOnAction;
            _ui.ResetNumbers += CancelAllOnAction;
            _ui.CheckFinalNumbers += CancelAllOnAction;
            _ui.OpenSettings += CancelAllOnAction;
        }

        public bool TrySelect(int cardIndex)
        {
            if (_layout == null || !_canInteract() || (_gate != null && _gate.IsRestricted)
                || !_cards.TryGetCardItem(cardIndex, out var card) || card.GetCardMoveHandler().IsMovementLocked()
                || !(card.GetView() is NormalCardItemView view) || !view.CanAnnotate || !view.gameObject.activeInHierarchy) return false;
            Cancel(); EnsureToolbar();
            _selected = view; _selectedMove = card.GetCardMoveHandler();
            _layout.SetCardAnnotationMode(true);
            _powerGroup = _layout.PowerUpView.GetComponent<CanvasGroup>();
            if (_powerGroup == null) _powerGroup = _layout.PowerUpView.gameObject.AddComponent<CanvasGroup>();
            _previousAlpha = _powerGroup.alpha; _previousRaycasts = _powerGroup.blocksRaycasts;
            _previousInteractable = _powerGroup.interactable;
            _powerGroup.alpha = 0; _powerGroup.blocksRaycasts = _powerGroup.interactable = false;
            _overlay.gameObject.SetActive(true); _overlay.SetAsLastSibling();
            PositionToolbar(); view.SetCardAnimation(true);
            _haptic?.Vibrate(HapticType.CardGrab);
            return true;
        }

        public void Tick()
        {
            if (_selectedMove == null) return;
            if (_selected == null || !_canInteract() || (_gate != null && _gate.IsRestricted)
                || _selectedMove.IsMovementLocked() || !_selected.CanAnnotate || !_selected.gameObject.activeInHierarchy)
            { Cancel(); return; }
            PositionToolbar();
        }

        public void Apply(CardAnnotation annotation)
        {
            if (_selected != null && _selected.CanAnnotate && !_selectedMove.IsMovementLocked() && _canInteract())
            { _selected.SetAnnotation(annotation); _haptic?.Vibrate(HapticType.ButtonClick); }
            Cancel();
        }

        public void Cancel()
        {
            if (_selected != null) _selected.SetCardAnimation(false);
            _selected = null; _selectedMove = null;
            if (_overlay != null) _overlay.gameObject.SetActive(false);
            if (_powerGroup != null)
            {
                _powerGroup.alpha = _previousAlpha; _powerGroup.blocksRaycasts = _previousRaycasts;
                _powerGroup.interactable = _previousInteractable; _powerGroup = null;
            }
            if (_layout != null) _layout.SetCardAnnotationMode(false);
        }

        private void CancelOnAction(object sender, EventArgs args) => Cancel();
        private void CancelAllOnAction(object sender, EventArgs args) => CancelAllGestures();
        public void CancelAllGestures()
        {
            Cancel();
            for (int i = 0; i < _cards.GetCardCount(); i++)
                if (_cards.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().CancelPress();
        }

        private void EnsureToolbar()
        {
            if (_overlay != null) return;
            _overlay = new GameObject("CardAnnotationOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(EventTrigger)).GetComponent<RectTransform>();
            _overlay.SetParent(_layout.transform, false); FruitLabStyle.Stretch(_overlay);
            _overlay.GetComponent<Image>().color = Color.clear;
            var dismiss = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            dismiss.callback.AddListener(_ => Cancel()); _overlay.GetComponent<EventTrigger>().triggers.Add(dismiss);
            _toolbar = new GameObject("CardAnnotationToolbar", typeof(RectTransform)).GetComponent<RectTransform>();
            _toolbar.SetParent(_overlay, false);
            var colors = new Color[] { new Color32(210, 100, 112, 255), new Color32(235, 194, 98, 255), new Color32(102, 194, 157, 255) };
            var inks = new Color[] { new Color32(57, 24, 31, 255), FruitLabStyle.Background, new Color32(15, 54, 42, 255) };
            var marks = new[] { CardAnnotation.Excluded, CardAnnotation.None, CardAnnotation.Candidate };
            var names = new[] { "Red", "Yellow", "Green" };
            for (int i = 0; i < 3; i++)
            {
                var item = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                item.transform.SetParent(_toolbar, false);
                var depth = FruitLabStyle.AddSurface(item, Color.Lerp(colors[i], FruitLabStyle.Background, .38f)); depth.raycastTarget = true;
                var face = FruitLabStyle.Detail(item.transform, "Face", colors[i]);
                face.rectTransform.anchorMin = Vector2.zero; face.rectTransform.anchorMax = Vector2.one;
                face.rectTransform.offsetMin = new Vector2(1, 3.5f); face.rectTransform.offsetMax = new Vector2(-1, -1);
                var edge = FruitLabStyle.Detail(face.transform, "Edge", new Color(1, 1, 1, .19f), true);
                FruitLabStyle.Stretch(edge.rectTransform, .5f);
                var light = FruitLabStyle.Detail(face.transform, "TopLight", new Color(1, 1, 1, .16f));
                light.rectTransform.anchorMin = new Vector2(.12f, 1); light.rectTransform.anchorMax = new Vector2(.88f, 1);
                light.rectTransform.sizeDelta = new Vector2(0, 1); light.rectTransform.anchoredPosition = new Vector2(0, -1.5f);
                var button = _buttons[i] = item.GetComponent<Button>(); button.targetGraphic = face;
                var states = button.colors;
                states.normalColor = states.selectedColor = Color.white;
                states.highlightedColor = new Color(.97f, .99f, 1);
                states.pressedColor = new Color(.76f, .82f, .84f);
                states.disabledColor = new Color(.6f, .6f, .6f, .7f); states.fadeDuration = .08f; button.colors = states;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var mark = marks[i]; button.onClick.AddListener(() => Apply(mark));
                if (i == 1) continue; // The yellow action deliberately has no icon or label.
                var symbol = FruitLabGlyph.Create(face.transform, i == 0 ? FruitLabGlyph.Symbol.Cross : FruitLabGlyph.Symbol.Check, inks[i]);
                symbol.rectTransform.anchorMin = symbol.rectTransform.anchorMax = Vector2.one * .5f;
                symbol.rectTransform.sizeDelta = Vector2.one * 25; symbol.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private void PositionToolbar()
        {
            Canvas.ForceUpdateCanvases(); _layout.PowerUpArea.GetWorldCorners(_corners);
            Vector2 min = _overlay.InverseTransformPoint(_corners[0]), max = _overlay.InverseTransformPoint(_corners[2]);
            FruitLabStyle.Place(_toolbar, _overlay, min.x - _overlay.rect.xMin, min.y - _overlay.rect.yMin, max.x - min.x, max.y - min.y);
            float gap = 8, width = (_toolbar.rect.width - gap * 2) / 3;
            for (int i = 0; i < 3; i++) FruitLabStyle.Place((RectTransform)_buttons[i].transform, _toolbar, i * (width + gap), 0, width, _toolbar.rect.height);
        }

        public void Dispose()
        {
            CancelAllGestures(); _placement.SetLongPressHandler(null);
            _placement.OnCardDragStartedEvent -= CancelOnAction;
            _ui.ResetNumbers -= CancelAllOnAction; _ui.CheckFinalNumbers -= CancelAllOnAction; _ui.OpenSettings -= CancelAllOnAction;
            if (_overlay != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_overlay.gameObject);
                else UnityEngine.Object.DestroyImmediate(_overlay.gameObject);
            }
        }
    }
}

using System;
using UnityEngine;
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
        private readonly CardAnnotationView _view;
        private NormalCardItemView _selected;
        private ICardMoveHandler _selectedMove;
        public bool IsSelecting => _selected != null;
        public RectTransform Toolbar => _view.Toolbar;
        public Button GetButton(int index) => _view.GetButton(index);

        public CardAnnotationController(IInitialCardAreaController cards, ICardPlacementCoordinator placement,
            IGameUIController ui, FruitLabGameLayout layout, TutorialInputGate gate,
            IHapticController haptic, Func<bool> canInteract)
        {
            _cards = cards; _placement = placement; _ui = ui; _layout = layout;
            _gate = gate; _haptic = haptic; _canInteract = canInteract;
            _view = new CardAnnotationView(layout, Cancel, Apply);
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
            Cancel();
            _selected = view; _selectedMove = card.GetCardMoveHandler();
            _view.Show(); view.SetCardAnimation(true);
            _haptic?.Vibrate(HapticType.CardGrab);
            return true;
        }

        public void Tick()
        {
            if (_selectedMove == null) return;
            if (_selected == null || !_canInteract() || (_gate != null && _gate.IsRestricted)
                || _selectedMove.IsMovementLocked() || !_selected.CanAnnotate || !_selected.gameObject.activeInHierarchy)
            { Cancel(); return; }
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
            _view.Hide();
        }

        private void CancelOnAction(object sender, EventArgs args) => Cancel();
        private void CancelAllOnAction(object sender, EventArgs args) => CancelAllGestures();
        public void CancelAllGestures()
        {
            Cancel();
            for (int i = 0; i < _cards.GetCardCount(); i++)
                if (_cards.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().CancelPress();
        }

        public void Dispose()
        {
            CancelAllGestures(); _placement.SetLongPressHandler(null);
            _placement.OnCardDragStartedEvent -= CancelOnAction;
            _ui.ResetNumbers -= CancelAllOnAction; _ui.CheckFinalNumbers -= CancelAllOnAction; _ui.OpenSettings -= CancelAllOnAction;
            _view.Dispose();
        }
    }
}

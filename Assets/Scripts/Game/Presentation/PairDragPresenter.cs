using System;
using Game;
using UnityEngine;

namespace Scripts
{
    // Coordinates pair drag commands. CardPlacementCoordinator remains the authority on placement rules.
    public sealed class PairDragPresenter : IDisposable
    {
        private readonly IInitialCardAreaController _cards;
        private readonly ICardPlacementCoordinator _placement;
        private readonly IBoardAreaController _board;
        private readonly IBoardPlacementQuery _query;
        private readonly TutorialInputGate _gate;
        private readonly IGameUIController _ui;
        private readonly Func<bool> _canInteract;
        public bool IsDragging { get; private set; }
        public bool CanInteract => (_canInteract?.Invoke() ?? false) && (_gate == null || !_gate.IsRestricted);
        public int CardCount => _cards.GetCardCount();
        public int SlotCount => _query.GetPlacedCardIndexesOnBoard().Count;
        public RectTransform DragParent => _cards.GetDragParent();
        public RectTransform BoardRect => SlotCount == 0 ? null : _board.GetRectTransformOfGarden(0).GetComponentInParent<BoardAreaView>()?.transform as RectTransform;
        public Vector3 SlotPosition(int index) => _board.GetRectTransformOfGarden(index).position;
        public RectTransform CardRect(int index) => _cards.TryGetCardItem(index, out var card) ? card.GetRectTransform() : null;

        public PairDragPresenter(IInitialCardAreaController cards, ICardPlacementCoordinator placement, IBoardAreaController board,
            IBoardPlacementQuery query, TutorialInputGate gate, IGameUIController ui, Func<bool> canInteract)
        {
            _cards = cards; _placement = placement; _board = board; _query = query; _gate = gate; _ui = ui; _canInteract = canInteract;
            _ui.OpenSettings += OnCancel; _ui.ResetNumbers += OnCancel; _ui.CheckFinalNumbers += OnCancel;
        }
        public bool TryGetFruitRects(PairClue clue, out RectTransform a, out RectTransform b)
        {
            a = b = null;
            if (clue == null) return false;
            a = CardRect(clue.First - 1); b = CardRect(clue.Second - 1);
            return a != null && b != null;
        }
        public bool CanDrag(PairClue clue) => CanInteract && clue != null
            && _cards.TryGetCardItem(clue.First - 1, out var a) && _cards.TryGetCardItem(clue.Second - 1, out var b)
            && !a.GetCardMoveHandler().IsMovementLocked() && !b.GetCardMoveHandler().IsMovementLocked();
        public bool TryBegin(PairClue clue, RectTransform dragParent)
        {
            if (IsDragging || !CanDrag(clue) || !_placement.TryBeginPairDrag(clue.First - 1, clue.Second - 1, dragParent)) return false;
            IsDragging = true; return true;
        }
        public void End(int slot, bool reverse)
        {
            if (!IsDragging) return;
            IsDragging = false;
            _placement.EndPairDrag(CanInteract ? slot : -1, reverse);
        }
        public void Tick() { if (IsDragging && !CanInteract) Cancel(); }
        public void Cancel() => End(-1, false);
        private void OnCancel(object sender, EventArgs args) => Cancel();
        public void Dispose()
        {
            Cancel();
            _ui.OpenSettings -= OnCancel; _ui.ResetNumbers -= OnCancel; _ui.CheckFinalNumbers -= OnCancel;
        }
    }
}

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
        private INormalCardItemView _selected;
        private readonly PositionAssistanceController _notes;
        private readonly IBoardAreaController _board;
        private int _selectedFruit;
        private ICardMoveHandler _selectedMove;
        public bool IsSelecting => _selected?.GetRectTransform() != null;
        public RectTransform Toolbar => _view.Toolbar;
        public Button GetButton(int index) => _view.GetButton(index);
        public PossibleHolderIndicatorView GetPositionButton(int slot)=>_view.GetPositionButton(slot);

        public CardAnnotationController(IInitialCardAreaController cards, ICardPlacementCoordinator placement,
            IGameUIController ui, FruitLabGameLayout layout, TutorialInputGate gate,
            IHapticController haptic, Func<bool> canInteract,PositionAssistanceController notes=null,
            IInitialCardAreaPresentation cardPresentation=null,IBoardAreaController board=null)
        {
            _cards = cards; _placement = placement; _ui = ui; _layout = layout;
            _gate = gate; _haptic = haptic; _canInteract = canInteract;
            _notes=notes;_board=board;
            _view = new CardAnnotationView(layout, Cancel, Apply,cardPresentation,board==null?null:board.GetRectTransformOfGarden,TogglePosition);
            if(_notes!=null)_notes.Changed+=OnNotesChanged;
            _placement.SetLongPressHandler(TrySelect);
            _placement.OnCardDragStartedEvent += CancelOnAction;
            _ui.ResetNumbers += CancelAllOnAction;
            _ui.CheckFinalNumbers += CancelAllOnAction;
            _ui.OpenSettings += CancelAllOnAction;_ui.OpenInformation += CancelAllOnAction;
        }

        public bool TrySelect(int cardIndex)
        {
            if (_notes==null || !_notes.CanEditNotes || !_notes.CanEditFruit(cardIndex+1) || _layout == null || !_canInteract() || (_gate != null && _gate.IsRestricted)
                || !_cards.TryGetCardItem(cardIndex, out var card) || card.GetCardMoveHandler().IsMovementLocked()
                || !card.GetView().CanAnnotate || !card.GetView().GetRectTransform().gameObject.activeInHierarchy) return false;
            Cancel();
            _notes.Suspend();
            _notes.Refresh();
            _selectedFruit=cardIndex+1;_selected = card.GetView(); _selectedMove = card.GetCardMoveHandler();
            _view.Show(); _selected.SetCardAnimation(true);
            _board?.RefreshLayout();_cards.RefreshLayout();OnNotesChanged();
            _haptic?.Vibrate(HapticType.CardGrab);
            return true;
        }

        public void Tick()
        {
            if (_selectedMove == null) return;
            if (!IsSelecting || !_notes.CanEditNotes || !_canInteract() || (_gate != null && _gate.IsRestricted)
                || _selectedMove.IsMovementLocked() || !_selected.CanAnnotate || !_selected.GetRectTransform().gameObject.activeInHierarchy)
            { Cancel(); return; }
        }

        public void Apply(CardAnnotation annotation)
        {
            if (IsSelecting && _selected.CanAnnotate && !_selectedMove.IsMovementLocked() && _canInteract())
            {
                _notes.SetPresence(_selectedFruit,annotation==CardAnnotation.Excluded?FruitPresence.Absent:annotation==CardAnnotation.Candidate?FruitPresence.Present:FruitPresence.Unknown);
                _haptic?.Vibrate(HapticType.ButtonClick);
            }
        }
        private void TogglePosition(int slot)
        {if(!IsSelecting)return;_notes.TogglePosition(_selectedFruit,slot);_haptic?.Vibrate(HapticType.ButtonClick);}
        private void OnNotesChanged(){if(IsSelecting)_view.RenderPositions(_notes.State,_selectedFruit);}

        public void Cancel()
        {
            if (IsSelecting) _selected.SetCardAnimation(false);
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
            if(_notes!=null)_notes.Changed-=OnNotesChanged;
            _placement.OnCardDragStartedEvent -= CancelOnAction;
            _ui.ResetNumbers -= CancelAllOnAction; _ui.CheckFinalNumbers -= CancelAllOnAction; _ui.OpenSettings -= CancelAllOnAction;_ui.OpenInformation -= CancelAllOnAction;
            _view.Dispose();
        }
    }
}

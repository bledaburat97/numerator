using System;
using Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    // Connects the existing card views. It owns no duplicate cards or answer ordering.
    public sealed class PairClueView : MonoBehaviour
    {
        [SerializeField] private PairRopeGraphic rope;
        private readonly PairRopeSimulation _simulation = new PairRopeSimulation();
        private readonly Vector3[] _corners = new Vector3[4];
        private BaseButtonView[] _controls = Array.Empty<BaseButtonView>();
        private RectTransform _powerArea;
        private IInitialCardAreaController _initial;
        private ICardPlacementCoordinator _placement;
        private IBoardAreaController _board;
        private IBoardPlacementQuery _query;
        private IGameUIController _ui;
        private TutorialInputGate _gate;
        private Func<bool> _canInteract;
        private PairClue _clue;
        private RectTransform _a, _b;
        private bool _reset = true, _dragging;
        private int _pointer;
        private Vector2 _dragStart, _startA, _startB;
        private RectTransform _dragParent;
        private CanvasGroup _group;
        public PairClue Clue => _clue;
        public PairRopeSimulation Simulation => _simulation;
        public bool IsGroupDragging => _dragging;
        public PairRopeGraphic Graphic => rope;
        public bool CanDragGroup => _clue != null && _initial != null && _placement != null && (_canInteract?.Invoke() ?? false)
            && (_gate == null || !_gate.IsRestricted) && TryActors(out var a, out var b)
            && !a.GetCardMoveHandler().IsMovementLocked() && !b.GetCardMoveHandler().IsMovementLocked();

        public void Configure(IInitialCardAreaController initial, ICardPlacementCoordinator placement, IBoardAreaController board,
            IBoardPlacementQuery query, TutorialInputGate gate, IGameUIController ui, Func<bool> canInteract)
        {
            Unbind(); _initial = initial; _placement = placement; _board = board; _query = query;
            _gate = gate; _ui = ui; _canInteract = canInteract;
            _ui.OpenSettings += CancelFromUI; _ui.ResetNumbers += CancelFromUI; _ui.CheckFinalNumbers += CancelFromUI;
            _group = GetComponent<CanvasGroup>();
            var canvas = GetComponentInParent<Canvas>();
            _controls = canvas != null ? canvas.GetComponentsInChildren<BaseButtonView>(true) : Array.Empty<BaseButtonView>();
            _powerArea = canvas != null ? canvas.GetComponent<FruitLabGameLayout>()?.PowerUpArea : null;
            if (rope != null) rope.Owner = this;
        }
        public void Unbind()
        {
            CancelDrag();
            if (_ui != null) { _ui.OpenSettings -= CancelFromUI; _ui.ResetNumbers -= CancelFromUI; _ui.CheckFinalNumbers -= CancelFromUI; }
            _initial = null; _placement = null; _board = null; _query = null; _ui = null; _canInteract = null;
            _controls = Array.Empty<BaseButtonView>(); _powerArea = null;
        }
        public void Show(PairClue clue)
        {
            CancelDrag(); _clue = clue; _reset = true;
            gameObject.SetActive(clue != null);
            if (rope != null) { rope.Owner = this; rope.Clear(); }
        }
        private void CancelFromUI(object sender, EventArgs args) => CancelDrag();
        private void LateUpdate() => RefreshRope(Time.unscaledDeltaTime);
        public void RefreshRope(float deltaTime)
        {
            if (_dragging && (!(_canInteract?.Invoke() ?? false) || (_gate != null && _gate.IsRestricted))) CancelDrag();
            if (rope == null || !TryActors(out var a, out var b)) { rope?.Clear(); return; }
            _a = a.GetRectTransform(); _b = b.GetRectTransform();
            if (_a == null || _b == null || !_a.gameObject.activeInHierarchy || !_b.gameObject.activeInHierarchy) { rope.Clear(); return; }
            var visualA = _a.Find("AnimationProvider") as RectTransform ?? _a;
            var visualB = _b.Find("AnimationProvider") as RectTransform ?? _b;
            Rect bodyA = Bounds(visualA), bodyB = Bounds(visualB);
            var area = (RectTransform)transform;
            if (!area.rect.Contains(bodyA.center) || !area.rect.Contains(bodyB.center)) { rope.Clear(); return; }
            Vector2 anchorA = BottomAnchor(visualA), anchorB = BottomAnchor(visualB);
            if (_reset) { _simulation.Reset(anchorA,anchorB); _reset = false; }
            _simulation.Advance(anchorA,anchorB,deltaTime);
            if (_group != null) _group.alpha = Mathf.Min(Opacity(_a),Opacity(_b));
            rope.UpdateCurve(_simulation);
        }
        private float Opacity(Transform card)
        {
            float alpha = 1;
            foreach (var group in card.GetComponentsInParent<CanvasGroup>()) alpha *= group.alpha;
            return alpha;
        }
        private Rect Bounds(RectTransform rect)
        {
            rect.GetWorldCorners(_corners);
            Vector2 min = transform.InverseTransformPoint(_corners[0]), max = min;
            for (int i = 1; i < 4; i++) { Vector2 p = transform.InverseTransformPoint(_corners[i]); min = Vector2.Min(min,p); max = Vector2.Max(max,p); }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private Vector2 BottomAnchor(RectTransform card)
        {
            // A point in the card's local space remains attached during motion,
            // rotation, scaling and reparenting between the initial area and board.
            return transform.InverseTransformPoint(card.TransformPoint(new Vector3(card.rect.center.x, card.rect.yMin, 0f)));
        }
        private bool TryActors(out INormalCardItemController a, out INormalCardItemController b)
        {
            a = b = null;
            return _clue != null && _initial != null && _initial.TryGetCardItem(_clue.First-1,out a) && _initial.TryGetCardItem(_clue.Second-1,out b);
        }
        public bool IsOverFruit(Vector2 screen, Camera camera)
        {
            if (_initial == null) return false;
            for (int i = 0; i < _initial.GetCardCount(); i++)
                if (_initial.TryGetCardItem(i,out var card) && card.GetRectTransform() != null
                    && RectTransformUtility.RectangleContainsScreenPoint(card.GetRectTransform(),screen,camera)) return true;
            return false;
        }
        public bool IsOverControls(Vector2 screen,Camera camera)
        {
            if (_powerArea != null && _powerArea.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_powerArea,screen,camera)) return true;
            foreach (var control in _controls)
                if (control != null && control.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(control.GetButtonRectTransform(),screen,camera)) return true;
            return false;
        }
        public void BeginGroupDrag(PointerEventData data)
        {
            if (!CanDragGroup || !TryActors(out var a,out var b)) return;
            _dragParent = _initial.GetDragParent();
            if (_dragParent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragParent,data.position,data.pressEventCamera,out _dragStart)) return;
            if (!_placement.TryBeginPairDrag(_clue.First-1,_clue.Second-1,_dragParent)) return;
            _a = a.GetRectTransform(); _b = b.GetRectTransform();
            _startA = _dragParent.InverseTransformPoint(_a.position); _startB = _dragParent.InverseTransformPoint(_b.position);
            _dragging = true; _pointer = data.pointerId;
        }
        public void DragGroup(PointerEventData data)
        {
            if (!_dragging || data.pointerId != _pointer || _a == null || _b == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragParent,data.position,data.pressEventCamera,out var point)) return;
            Vector2 delta = point-_dragStart;
            _a.position = _dragParent.TransformPoint(_startA+delta); _b.position = _dragParent.TransformPoint(_startB+delta);
        }
        public void EndGroupDrag(PointerEventData data)
        {
            if (!_dragging || data.pointerId != _pointer) return;
            int best = -1;
            if ((_canInteract?.Invoke() ?? false) && (_gate == null || !_gate.IsRestricted))
            {
                var boardView = _board.GetRectTransformOfGarden(0).GetComponentInParent<BoardAreaView>();
                if (boardView != null && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)boardView.transform,data.position,data.pressEventCamera))
                {
                    float distance = float.MaxValue;
                    for (int i = 0; i < _query.GetPlacedCardIndexesOnBoard().Count-1; i++)
                    {
                        Vector2 center = RectTransformUtility.WorldToScreenPoint(data.pressEventCamera,
                            (_board.GetRectTransformOfGarden(i).position+_board.GetRectTransformOfGarden(i+1).position)*.5f);
                        float current = (center-data.position).sqrMagnitude;
                        if (current < distance) { distance = current; best = i; }
                    }
                }
            }
            _dragging = false;
            _placement.EndPairDrag(best,_a != null && _b != null && _a.position.x > _b.position.x);
        }
        public void CancelDrag()
        {
            if (!_dragging) return;
            _dragging = false; _placement?.EndPairDrag(-1,false);
        }
        private void OnDisable() => CancelDrag();
        private void OnDestroy() => Unbind();
    }
}

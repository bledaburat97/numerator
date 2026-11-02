using System;
using System.Collections.Generic;
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
        private PairDragPresenter _presenter;
        private PairClue _clue;
        private RectTransform _a, _b;
        private bool _reset = true;
        private int _pointer;
        private Vector2 _dragStart, _startA, _startB;
        private RectTransform _dragParent;
        private CanvasGroup _group;
        private readonly CardVisualCache _visualA = new CardVisualCache();
        private readonly CardVisualCache _visualB = new CardVisualCache();
        public PairClue Clue => _clue;
        public PairRopeSimulation Simulation => _simulation;
        public bool IsGroupDragging => _presenter != null && _presenter.IsDragging;
        public PairRopeGraphic Graphic => rope;
        public Func<Vector2, Camera, bool> AdditionalControlHitTest { get; set; }
        public bool CanDragGroup => _presenter != null && _presenter.CanDrag(_clue);

        public void Configure(PairDragPresenter presenter)
        {
            Unbind();
            _presenter = presenter;
            _group = GetComponent<CanvasGroup>();
            var canvas = GetComponentInParent<Canvas>();
            _controls = canvas != null ? canvas.GetComponentsInChildren<BaseButtonView>(true) : Array.Empty<BaseButtonView>();
            _powerArea = canvas != null ? canvas.GetComponent<FruitLabGameLayout>()?.PowerUpArea : null;
            if (rope != null) rope.Owner = this;
        }
        public void Unbind()
        {
            _presenter?.Dispose(); _presenter = null;
            _controls = Array.Empty<BaseButtonView>(); _powerArea = null;
        }
        public void Show(PairClue clue)
        {
            CancelDrag(); _clue = clue; _reset = true;
            gameObject.SetActive(clue != null);
            if (rope != null) { rope.Owner = this; rope.Clear(); }
        }
        private void LateUpdate() => RefreshRope(Time.unscaledDeltaTime);
        public void RefreshRope(float deltaTime)
        {
            _presenter?.Tick();
            if (rope == null || !TryActors(out var a, out var b)) { rope?.Clear(); return; }
            _a = a; _b = b;
            if (_a == null || _b == null || !_a.gameObject.activeInHierarchy || !_b.gameObject.activeInHierarchy) { rope.Clear(); return; }
            var visualA = _visualA.Resolve(_a);
            var visualB = _visualB.Resolve(_b);
            Rect bodyA = Bounds(visualA), bodyB = Bounds(visualB);
            var area = (RectTransform)transform;
            if (!area.rect.Contains(bodyA.center) || !area.rect.Contains(bodyB.center)) { rope.Clear(); return; }
            Vector2 anchorA = BottomAnchor(visualA), anchorB = BottomAnchor(visualB);
            if (_reset) { _simulation.Reset(anchorA,anchorB); _reset = false; }
            _simulation.Advance(anchorA,anchorB,deltaTime);
            if (_group != null) _group.alpha = Mathf.Min(_visualA.Opacity, _visualB.Opacity);
            rope.UpdateCurve(_simulation);
        }

        private sealed class CardVisualCache
        {
            private RectTransform _root, _visual;
            private readonly List<Transform> _parents = new List<Transform>();
            private readonly List<CanvasGroup> _groups = new List<CanvasGroup>();
            public float Opacity
            {
                get
                {
                    float alpha = 1;
                    for (int i = 0; i < _groups.Count; i++) if (_groups[i] != null) alpha *= _groups[i].alpha;
                    return alpha;
                }
            }
            public RectTransform Resolve(RectTransform root)
            {
                if (_root != root || _visual == null)
                {
                    _root = root;
                    _visual = root.Find("AnimationProvider") as RectTransform ?? root;
                    _parents.Clear();
                }
                Transform parent = root;
                int index = 0;
                while (parent != null && index < _parents.Count && _parents[index] == parent)
                { parent = parent.parent; index++; }
                if (parent != null || index != _parents.Count)
                {
                    _parents.Clear();
                    for (parent = root; parent != null; parent = parent.parent) _parents.Add(parent);
                    root.GetComponentsInParent(false, _groups);
                }
                return _visual;
            }
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
        private bool TryActors(out RectTransform a, out RectTransform b)
        {
            a = b = null;
            return _presenter != null && _presenter.TryGetFruitRects(_clue, out a, out b);
        }
        public bool IsOverFruit(Vector2 screen, Camera camera)
        {
            if (_presenter == null) return false;
            for (int i = 0; i < _presenter.CardCount; i++)
            {
                var rect = _presenter.CardRect(i);
                if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, camera)) return true;
            }
            return false;
        }
        public bool IsOverControls(Vector2 screen,Camera camera)
        {
            if (AdditionalControlHitTest?.Invoke(screen, camera) == true) return true;
            if (_powerArea != null && _powerArea.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_powerArea,screen,camera)) return true;
            foreach (var control in _controls)
                if (control != null && control.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(control.GetButtonRectTransform(),screen,camera)) return true;
            return false;
        }
        public void BeginGroupDrag(PointerEventData data)
        {
            if (!CanDragGroup || !TryActors(out var a,out var b)) return;
            _dragParent = _presenter.DragParent;
            if (_dragParent == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragParent,data.position,data.pressEventCamera,out _dragStart)) return;
            if (!_presenter.TryBegin(_clue, _dragParent)) return;
            _a = a; _b = b;
            _startA = _dragParent.InverseTransformPoint(_a.position); _startB = _dragParent.InverseTransformPoint(_b.position);
            _pointer = data.pointerId;
        }
        public void DragGroup(PointerEventData data)
        {
            if (!IsGroupDragging || data.pointerId != _pointer || _a == null || _b == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragParent,data.position,data.pressEventCamera,out var point)) return;
            Vector2 delta = point-_dragStart;
            _a.position = _dragParent.TransformPoint(_startA+delta); _b.position = _dragParent.TransformPoint(_startB+delta);
        }
        public void EndGroupDrag(PointerEventData data)
        {
            if (!IsGroupDragging || data.pointerId != _pointer) return;
            int best = -1;
            if (_presenter.CanInteract)
            {
                var boardView = _presenter.BoardRect;
                if (boardView != null && RectTransformUtility.RectangleContainsScreenPoint(boardView,data.position,data.pressEventCamera))
                {
                    float distance = float.MaxValue;
                    for (int i = 0; i < _presenter.SlotCount-1; i++)
                    {
                        Vector2 center = RectTransformUtility.WorldToScreenPoint(data.pressEventCamera,
                            (_presenter.SlotPosition(i)+_presenter.SlotPosition(i+1))*.5f);
                        float current = (center-data.position).sqrMagnitude;
                        if (current < distance) { distance = current; best = i; }
                    }
                }
            }
            _presenter.End(best,_a != null && _b != null && _a.position.x > _b.position.x);
        }
        public void CancelDrag() => _presenter?.Cancel();
        private void OnDisable() => CancelDrag();
        private void OnDestroy() => Unbind();
    }
}

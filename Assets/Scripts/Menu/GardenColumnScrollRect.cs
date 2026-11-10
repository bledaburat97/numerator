using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    // A two-column window moves by ONE column, including when more columns are added.
    public sealed class GardenColumnScrollRect : ScrollRect
    {
        public event System.Action<int> ColumnSettled;
        public int Column { get; private set; }
        public float ColumnWidth { get; private set; }
        public int LastColumn { get; private set; }
        public int DragVersion { get; private set; }
        private Tween _snap;

        public void Configure(float columnWidth, int totalColumns)
        {
            ColumnWidth = columnWidth;
            LastColumn = Mathf.Max(0, totalColumns - 2);
            horizontal = true; vertical = false; inertia = false;
            movementType = MovementType.Clamped;
            SetColumn(Column, false);
        }
        public override void OnBeginDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            DragVersion++;
            _snap?.Kill(); StopMovement(); base.OnBeginDrag(data);
        }
        public override void OnEndDrag(PointerEventData data)
        {
            base.OnEndDrag(data);
            if (data.button == PointerEventData.InputButton.Left && ColumnWidth > 0)
                SetColumn(Mathf.RoundToInt(-content.anchoredPosition.x / ColumnWidth), true);
        }
        public void SetColumn(int column, bool animated)
        {
            _snap?.Kill(); StopMovement(); Column = Mathf.Clamp(column, 0, LastColumn);
            if (content == null || ColumnWidth <= 0) return;
            var target = new Vector2(-Column * ColumnWidth, 0);
            if (animated && Application.isPlaying)
                _snap = content.DOAnchorPos(target, .24f).SetEase(Ease.OutCubic).SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable).OnComplete(()=>ColumnSettled?.Invoke(Column));
            else {content.anchoredPosition = target;ColumnSettled?.Invoke(Column);}
        }
        protected override void OnDisable()
        {
            _snap?.Kill(); base.OnDisable();
            if (content != null) content.anchoredPosition = new Vector2(-Column * ColumnWidth, 0);
        }
    }
}

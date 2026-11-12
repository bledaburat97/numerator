using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu
{
    // Free horizontal panning. Column coordinates remain useful for focusing a fruit.
    public sealed class GardenColumnScrollRect : ScrollRect
    {
        public event System.Action<int> ColumnSettled;
        public int Column { get; private set; }
        public float ColumnWidth { get; private set; }
        public int LastColumn { get; private set; }
        public int DragVersion { get; private set; }
        public float ColumnOffset => content != null && ColumnWidth > 0 ? Mathf.Clamp(-content.anchoredPosition.x / ColumnWidth, 0, LastColumn) : 0;
        private Tween _snap;

        public void Configure(float columnWidth, int totalColumns)
        {
            float position = ColumnOffset;
            _snap?.Kill(); StopMovement(); ColumnWidth = columnWidth;
            LastColumn = Mathf.Max(0, totalColumns - 2);
            horizontal = true; vertical = false; inertia = false;
            movementType = MovementType.Clamped;
            SetPosition(position, false);
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
            { Column = Mathf.FloorToInt(ColumnOffset); ColumnSettled?.Invoke(Column); }
        }
        public bool IsColumnVisible(int column) => column + 1 > ColumnOffset + .01f && column < ColumnOffset + 2 - .01f;
        public void FocusColumn(int column)
        {
            if (column < ColumnOffset) SetPosition(column, true);
            else if (column + 1 > ColumnOffset + 2) SetPosition(column - 1, true);
        }
        public void SetColumn(int column, bool animated) => SetPosition(column, animated);
        private void SetPosition(float position, bool animated)
        {
            _snap?.Kill(); StopMovement(); position = Mathf.Clamp(position, 0, LastColumn); Column = Mathf.FloorToInt(position);
            if (content == null || ColumnWidth <= 0) return;
            var target = new Vector2(-position * ColumnWidth, 0);
            if (animated && Application.isPlaying)
                _snap = content.DOAnchorPos(target, .24f).SetEase(Ease.OutCubic).SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable).OnComplete(()=>ColumnSettled?.Invoke(Column));
            else {content.anchoredPosition = target;ColumnSettled?.Invoke(Column);}
        }
        protected override void OnDisable()
        {
            _snap?.Kill(); base.OnDisable();
        }
    }
}

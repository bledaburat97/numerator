using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menu
{
    // A gesture carries a BATCH identity, never a mutable cell index. Rebinding
    // a just-freed cell cannot redirect an old click to the next planting.
    public sealed class GardenHarvestCell : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler
    {
        private string _batchId, _pressedBatchId;
        private int _pointerId, _dragVersion;
        private Vector2 _start;
        private GardenColumnScrollRect _scroll;
        private Action<string> _harvest;
        public void Bind(string batchId, Action<string> harvest)
        { _batchId=batchId;_harvest=harvest;_scroll=GetComponentInParent<GardenColumnScrollRect>(); }
        public void OnPointerDown(PointerEventData data)
        {
            if(data.button!=PointerEventData.InputButton.Left)return;
            _pressedBatchId=_batchId;_pointerId=data.pointerId;_start=data.position;_dragVersion=_scroll!=null?_scroll.DragVersion:0;
        }
        public void OnPointerUp(PointerEventData data) { }
        public void OnPointerClick(PointerEventData data)
        {
            string batch=_pressedBatchId;_pressedBatchId=null;
            if(data.button!=PointerEventData.InputButton.Left || batch==null || data.pointerId!=_pointerId || data.dragging
                || (_scroll!=null && _scroll.DragVersion!=_dragVersion) || Vector2.Distance(_start,data.position)>10)return;
            _harvest?.Invoke(batch);
        }
        private void OnDisable()=>_pressedBatchId=null;
    }
}

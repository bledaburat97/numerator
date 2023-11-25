using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class DraggableCardItemView : BaseCardItemView, IDraggableCardItemView, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        protected Action<PointerEventData> _onDrag;
        protected Action<PointerEventData> _onPointerDown;
        protected Action<PointerEventData> _onPointerUp;
        
        public void SetParent(RectTransform parentTransform)
        {
            rectTransform.SetParent(parentTransform);
        }
        
        public RectTransform GetParent()
        {
            return rectTransform.parent.GetComponent<RectTransform>();
        }
        
        public void SetAnchoredPosition(Vector2 localPosition)
        {
            rectTransform.anchoredPosition = localPosition;
        }
        
        public void SetOnDrag(Action<PointerEventData> onDrag)
        {
            _onDrag = onDrag;
        }

        public void SetOnPointerDown(Action<PointerEventData> onPointerDown)
        {
            _onPointerDown = onPointerDown;
        }

        public void SetOnPointerUp(Action<PointerEventData> onPointerUp)
        {
            _onPointerUp = onPointerUp;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            _onDrag?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDown?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUp?.Invoke(eventData);
        }
    }

    public interface IDraggableCardItemView : IBaseCardItemView
    {
        void SetParent(RectTransform parentTransform);
        RectTransform GetParent();
        void SetAnchoredPosition(Vector2 localPosition);
        void SetOnDrag(Action<PointerEventData> onDrag);
        void SetOnPointerDown(Action<PointerEventData> onPointerDown);
        void SetOnPointerUp(Action<PointerEventData> onPointerUp);
    }
}
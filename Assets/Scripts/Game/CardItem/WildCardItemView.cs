using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class WildCardItemView : BaseCardItemView, IWildCardItemView, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Action<PointerEventData> _onDrag;
        private Action<PointerEventData> _onPointerDown;
        private Action<PointerEventData> _onPointerUp;
        private Vector3 _localPositionGap = Vector2.zero;
        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetLocalPositionGap(int cardItemIndex)
        {
            _localPositionGap += new Vector3(-3.5f, -2f, 0f) * cardItemIndex;
        }
        
        public override void InitPosition()
        {
            rectTransform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero + _localPositionGap;
        }
        
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

    public interface IWildCardItemView : IBaseCardItemView
    {
        void Destroy();
        void SetParent(RectTransform parentTransform);
        RectTransform GetParent();
        void SetAnchoredPosition(Vector2 localPosition);
        void SetOnDrag(Action<PointerEventData> onDrag);
        void SetOnPointerDown(Action<PointerEventData> onPointerDown);
        void SetOnPointerUp(Action<PointerEventData> onPointerUp);
        void SetLocalPositionGap(int cardItemIndex);
    }
}
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class DraggableCardItemView : BaseCardItemView, IDraggableCardItemView, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image lockImage;
        private Action<PointerEventData> _onDrag;
        private Action<PointerEventData> _onPointerDown;
        private Action<PointerEventData> _onPointerUp;

        public override void Init(int cardNumber)
        {
            base.Init(cardNumber);
            SetLockImageStatus(false);
        }

        public void SetColor(Color color)
        {
            innerBGImage.color = color;
            outerBGImage.color = color;
        }

        public void SetLockImageStatus(bool status)
        {
            lockImage.gameObject.SetActive(status);
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

    public interface IDraggableCardItemView : IBaseCardItemView
    {
        void SetColor(Color color);
        void SetLockImageStatus(bool status);
        void SetParent(RectTransform parentTransform);
        RectTransform GetParent();
        void SetAnchoredPosition(Vector2 localPosition);
        void SetOnDrag(Action<PointerEventData> onDrag);
        void SetOnPointerDown(Action<PointerEventData> onPointerDown);
        void SetOnPointerUp(Action<PointerEventData> onPointerUp);
    }
}
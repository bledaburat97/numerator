using UnityEngine;
using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Views
{
    public class CardItemView : MonoBehaviour, ICardItemView, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text cardNumberText;
        [SerializeField] private Image cardImage;
        [SerializeField] private Image cardFrame;
        [SerializeField] private RectTransform cardLetterHolder;
        [SerializeField] private RectTransform existenceButtonHolder;
        
        private Action<PointerEventData> _onDrag;
        private Action<PointerEventData> _onPointerDown;
        private Action<PointerEventData> _onPointerUp;
        
        public void Init(int cardNumber)
        {
            InitPosition();
            SetCardNumberText(cardNumber);
            SetFrameStatus(false);
        }

        public RectTransform GetCardLetterHolderTransform()
        {
            return cardLetterHolder;
        }
        
        public RectTransform GetExistenceButtonHolderTransform()
        {
            return existenceButtonHolder;
        }

        public void InitPosition()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
        
        private void SetCardNumberText(int number)
        {
            cardNumberText.text = number.ToString();
        }

        private void SetColor(Color color)
        {
            cardImage.color = color;
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

        public void SetFrameStatus(bool status)
        {
            cardFrame.gameObject.SetActive(status);
        }
    }

    public interface ICardItemView
    {
        void Init(int cardNumber);
        void InitPosition();
        RectTransform GetCardLetterHolderTransform();
        RectTransform GetExistenceButtonHolderTransform();
        void SetSize(Vector2 size);
        void SetOnDrag(Action<PointerEventData> onDrag);
        void SetOnPointerDown(Action<PointerEventData> onPointerDown);
        void SetOnPointerUp(Action<PointerEventData> onPointerUp);
        void SetParent(RectTransform parentTransform);
        RectTransform GetParent();
        void SetAnchoredPosition(Vector2 localPosition);
        void SetFrameStatus(bool status);

    }
}
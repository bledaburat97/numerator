using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseButtonView : MonoBehaviour, IBaseButtonView, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image outerBg;
        [SerializeField] private Image shadow;
        [SerializeField] private Image innerBg;
        [SerializeField] protected Image image;
        private Vector2 innerBgOffsetMin;
        private Vector2 innerBgOffsetMax;
        private Action _onPointerDown;
        private Action _onPointerUp;

        public void Init()
        {
            transform.localScale = Vector3.one;
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
        }

        public void SetOnPointerDownCallBack(Action onPointerDown)
        {
            _onPointerDown = onPointerDown;
        }
        
        public void SetOnPointerUpCallBack(Action onPointerUp)
        {
            _onPointerUp = onPointerUp;
        }

        public void SetButtonDown()
        {
            outerBg.enabled = false;
            shadow.enabled = false;
            innerBg.rectTransform.offsetMin = Vector2.zero;
            innerBg.rectTransform.offsetMax = Vector2.zero;
        }

        public void SetButtonUp()
        {
            outerBg.enabled = true;
            shadow.enabled = true;
            innerBg.rectTransform.offsetMin = innerBgOffsetMin;
            innerBg.rectTransform.offsetMax = innerBgOffsetMax;
        }
        
        public void SetText(string newText)
        {
            text.SetText(newText);
        }

        public void SetLocalPosition(Vector2 localPos)
        {
            transform.localPosition = localPos;
        }

        public void SetImageStatus(bool status)
        {
            image.gameObject.SetActive(status);
        }

        public void SetTextStatus(bool status)
        {
            text.gameObject.SetActive(status);
        }
        
        public void SetButtonStatus(bool status)
        {
            gameObject.SetActive(status);
        }
        
        public void SetColorOfImage(Color color)
        {
            innerBg.color = color;
            outerBg.color = color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUp?.Invoke();
        }

        public RectTransform GetRectTransform()
        {
            return outerBg.rectTransform;
        }
        public RectTransform GetButtonRectTransform()
        {
            return rectTransform;
        }
    }
    
    public interface IBaseButtonView
    {
        void Init();
        void SetText(string newText);
        void SetLocalPosition(Vector2 localPos);
        void SetImageStatus(bool status);
        void SetTextStatus(bool status);
        void SetButtonStatus(bool status);
        void SetColorOfImage(Color color);
        RectTransform GetRectTransform();
        RectTransform GetButtonRectTransform();
        void SetOnPointerDownCallBack(Action onPointerDown);
        void SetOnPointerUpCallBack(Action onPointerUp);
        void SetButtonDown();
        void SetButtonUp();
    }
    
}
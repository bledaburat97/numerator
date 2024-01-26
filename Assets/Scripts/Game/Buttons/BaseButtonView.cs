using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseButtonView : MonoBehaviour, IBaseButtonView, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Button button;
        [SerializeField] private Image outerBg;
        [SerializeField] private Image shadow;
        [SerializeField] private Image innerBg;
        [SerializeField] private Image image;
        private Vector2 innerBgOffsetMin;
        private Vector2 innerBgOffsetMax;

        public void Init(Action onClick)
        {
            transform.localScale = Vector3.one;
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
            button.onClick.AddListener(() => onClick?.Invoke());
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

        public void SetButtonEnable(bool status)
        {
            button.enabled = status;
        }
        
        public void SetColorOfImage(Color color)
        {
            innerBg.color = color;
            outerBg.color = color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            outerBg.enabled = false;
            shadow.enabled = false;
            innerBg.rectTransform.offsetMin = Vector2.zero;
            innerBg.rectTransform.offsetMax = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            outerBg.enabled = true;
            shadow.enabled = true;
            innerBg.rectTransform.offsetMin = innerBgOffsetMin;
            innerBg.rectTransform.offsetMax = innerBgOffsetMax;
        }
    }
    
    public interface IBaseButtonView
    {
        void Init(Action onClick);
        void SetText(string newText);
        void SetLocalPosition(Vector2 localPos);
        void SetImageStatus(bool status);
        void SetTextStatus(bool status);
        void SetButtonStatus(bool status);
        void SetButtonEnable(bool status);
        void SetColorOfImage(Color color);
    }
    
}
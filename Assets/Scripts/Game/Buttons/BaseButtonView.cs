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
        private Vector2 innerBgOffsetMin;
        private Vector2 innerBgOffsetMax;

        public void Init(BaseButtonModel model)
        {
            text.SetText(model.text);
            SetOnClickAction(model.OnClick);
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
        }

        private void SetOnClickAction(Action onClick)
        {
            button.onClick.AddListener(() => onClick.Invoke());
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
        void Init(BaseButtonModel model);
    }

    public class BaseButtonModel
    {
        public string text;
        public Action OnClick;
    }
}
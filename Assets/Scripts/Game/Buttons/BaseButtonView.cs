using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseButtonView : MonoBehaviour, IBaseButtonView, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] protected TMP_Text text;
        [SerializeField] protected Button button;
        [SerializeField] protected Image outerBg;
        [SerializeField] protected Image shadow;
        [SerializeField] protected Image innerBg;
        protected Vector2 innerBgOffsetMin;
        protected Vector2 innerBgOffsetMax;

        public void Init(BaseButtonModel model)
        {
            text.SetText(model.text);
            SetOnClickAction(model.OnClick);
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
        }

        protected void SetOnClickAction(Action onClick)
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

        public void SetText(string newText)
        {
            text.SetText(newText);
        }

        public void InitPosition(Vector2 localPos)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = localPos;
        }
    }
    
    public interface IBaseButtonView
    {
        void Init(BaseButtonModel model);
        void SetText(string newText);
        void InitPosition(Vector2 localPos);
    }

    public class BaseButtonModel
    {
        public string text;
        public Action OnClick;
        public Vector2 localPosition;
    }
}
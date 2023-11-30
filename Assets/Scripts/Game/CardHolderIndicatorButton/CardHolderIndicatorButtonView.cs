using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CardHolderIndicatorButtonView : BaseButtonView, ICardHolderIndicatorButtonView
    {
        [SerializeField] private Image crossImage;
        
        public void Init(CardHolderIndicatorButtonModel model)
        {
            transform.localPosition = new Vector3(model.localXPosition, 0, transform.localPosition.z);
            transform.localScale = Vector3.one;
            text.SetText(model.text);
            SetOnClickAction(model.OnClick);
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
        }
        
        public void SetStatus(bool status)
        {
            text.gameObject.SetActive(status);
            crossImage.gameObject.SetActive(!status);
        }
        
    }
    
    public interface ICardHolderIndicatorButtonView : IBaseButtonView
    {
        void Init(CardHolderIndicatorButtonModel model);
        void SetStatus(bool status);
    }
}
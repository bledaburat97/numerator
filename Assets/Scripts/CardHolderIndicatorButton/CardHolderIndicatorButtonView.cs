using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CardHolderIndicatorButtonView : MonoBehaviour, ICardHolderIndicatorButtonView
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private Image crossImage;
        
        public void Init(CardHolderIndicatorButtonModel model)
        {
            transform.localPosition = new Vector3(model.localXPosition, 0, transform.localPosition.z);
            transform.localScale = Vector3.one;
            button.onClick.AddListener(()=> model.onClickAction.Invoke());
            SetText(model.Id);
        }
        
        public void SetStatus(bool status)
        {
            buttonText.gameObject.SetActive(status);
            crossImage.gameObject.SetActive(!status);
        }

        private void SetText(string text)
        {
            buttonText.SetText(text);
        }
    }
    
    public interface ICardHolderIndicatorButtonView
    {
        void Init(CardHolderIndicatorButtonModel model);
        void SetStatus(bool status);
    }
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PowerUpMessagePopupView : MonoBehaviour, IPowerUpMessagePopupView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image powerUpImage;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text text;
        [SerializeField] private BaseButtonView continueButton;
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private RectTransform leftSide;

        public void Init()
        {
            transform.localScale = Vector3.one;
        }

        public void SetStatus(bool status)
        {
            rectTransform.gameObject.SetActive(status);
        }

        public IBaseButtonView GetContinueButton()
        {
            return continueButton;
        }
        
        public IBaseButtonView GetCloseButton()
        {
            return closeButton;
        }

        public void SetTitle(string text)
        {
            title.SetText(text);
        }

        public void SetText(string text)
        {
            this.text.SetText(text);
        }

        public void SetSprite(Sprite sprite)
        {
            powerUpImage.sprite = sprite;
        }
    }

    public interface IPowerUpMessagePopupView
    {
        void Init();
        void SetStatus(bool status);
        IBaseButtonView GetContinueButton();
        IBaseButtonView GetCloseButton();
        void SetTitle(string text);
        void SetText(string text);
        void SetSprite(Sprite sprite);
    }
}
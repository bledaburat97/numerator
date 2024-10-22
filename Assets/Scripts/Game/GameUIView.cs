using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GameUIView : MonoBehaviour, IGameUIView
    {
        [SerializeField] private TMP_Text userText;
        [SerializeField] private TMP_Text opponentText;
        [SerializeField] private RectTransform scrollAreaRectTransform;
        [SerializeField] private BaseButtonView checkButton;
        [SerializeField] private BaseButtonView resetButton;
        [SerializeField] private BaseButtonView settingsButton;
        [SerializeField] private CardInfoButtonView cardInfoButton;
        [SerializeField] private PowerUpButtonView revealingPowerUpButton;
        [SerializeField] private PowerUpButtonView lifePowerUpButton;
        [SerializeField] private PowerUpButtonView hintPowerUpButton;
        [SerializeField] private GameObject opponentInfo;
        [SerializeField] private CanvasGroup topButtonsCanvasGroup;
        [SerializeField] private CanvasGroup middleButtonsCanvasGroup;

        public CanvasGroup GetTopButtonsCanvasGroup() => topButtonsCanvasGroup;
        public CanvasGroup GetMiddleButtonsCanvasGroup() => middleButtonsCanvasGroup;
        
        public void SetUserText(string text)
        {
            userText.gameObject.SetActive(true);
            userText.SetText(text);
        }
        
        public void SetOpponentText(string text)
        {
            opponentText.gameObject.SetActive(true);
            opponentText.SetText(text);
        }

        public void SetOpponentInfoStatus(bool status)
        {
            opponentInfo.SetActive(status);
        }

        public IBaseButtonView GetCheckButton()
        {
            return checkButton;
        }
        
        public IBaseButtonView GetResetButton()
        {
            return resetButton;
        }
        
        public IBaseButtonView GetSettingsButton()
        {
            return settingsButton;
        }

        public ICardInfoButtonView GetCardInfoButton()
        {
            return cardInfoButton;
        }
        
        public IBaseButtonView GetRevealingPowerUpButton()
        {
            return revealingPowerUpButton;
        }
        
        public IBaseButtonView GetLifePowerUpButton()
        {
            return lifePowerUpButton;
        }
        
        public IBaseButtonView GetHintPowerUpButton()
        {
            return hintPowerUpButton;
        }

        public TMP_Text GetUserText()
        {
            return userText;
        }
    }

    public interface IGameUIView
    {
        void SetUserText(string text);
        void SetOpponentText(string text);
        IBaseButtonView GetCheckButton();
        IBaseButtonView GetResetButton();
        IBaseButtonView GetSettingsButton();
        ICardInfoButtonView GetCardInfoButton();
        IBaseButtonView GetRevealingPowerUpButton();
        IBaseButtonView GetLifePowerUpButton();
        IBaseButtonView GetHintPowerUpButton();
        void SetOpponentInfoStatus(bool status);
        CanvasGroup GetTopButtonsCanvasGroup();
        CanvasGroup GetMiddleButtonsCanvasGroup();
        TMP_Text GetUserText();
    }
}
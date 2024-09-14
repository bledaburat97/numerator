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
        [SerializeField] private List<PowerUpModel> powerUpModels;
        [SerializeField] private GameObject opponentInfo;

        public void SetImagesOfPowerUpButtons()
        {
            revealingPowerUpButton.SetImage(powerUpModels[0].sprite);
            lifePowerUpButton.SetImage(powerUpModels[1].sprite);
            hintPowerUpButton.SetImage(powerUpModels[2].sprite);
        }
        
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

        public PowerUpModel GetPowerUpModel(int index)
        {
            return powerUpModels[index];
        }
    }

    public interface IGameUIView
    {
        void SetImagesOfPowerUpButtons();
        void SetUserText(string text);
        void SetOpponentText(string text);
        IBaseButtonView GetCheckButton();
        IBaseButtonView GetResetButton();
        IBaseButtonView GetSettingsButton();
        ICardInfoButtonView GetCardInfoButton();
        IBaseButtonView GetRevealingPowerUpButton();
        IBaseButtonView GetLifePowerUpButton();
        IBaseButtonView GetHintPowerUpButton();
        PowerUpModel GetPowerUpModel(int index);
        void SetOpponentInfoStatus(bool status);
    }
}
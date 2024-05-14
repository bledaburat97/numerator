using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GameUIView : MonoBehaviour, IGameUIView
    {
        [SerializeField] private TMP_Text levelIdText;
        [SerializeField] private RectTransform scrollAreaRectTransform;
        [SerializeField] private BaseButtonView checkButton;
        [SerializeField] private BaseButtonView resetButton;
        [SerializeField] private BaseButtonView settingsButton;
        [SerializeField] private CardInfoButtonView cardInfoButton;
        [SerializeField] private PowerUpButtonView revealingPowerUpButton;
        [SerializeField] private PowerUpButtonView lifePowerUpButton;
        [SerializeField] private PowerUpButtonView hintPowerUpButton;
        [SerializeField] private List<PowerUpModel> powerUpModels;

        public void Init()
        {
            revealingPowerUpButton.SetImage(powerUpModels[0].sprite);
            lifePowerUpButton.SetImage(powerUpModels[1].sprite);
            hintPowerUpButton.SetImage(powerUpModels[2].sprite);
        }
        
        public void SetLevelId(string text)
        {
            levelIdText.SetText(text);
        }

        public void DisableLevelId()
        {
            levelIdText.gameObject.SetActive(false);
        }

        public void IncreaseSizeAndPositionOfScrollArea(float sizeOfResultBlock)
        {
            scrollAreaRectTransform.localPosition = new Vector2(0, scrollAreaRectTransform.localPosition.y + sizeOfResultBlock / 2);
            scrollAreaRectTransform.sizeDelta = new Vector2(scrollAreaRectTransform.sizeDelta.x,scrollAreaRectTransform.sizeDelta.y + sizeOfResultBlock);
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
        void Init();
        void SetLevelId(string text);
        void DisableLevelId();
        void IncreaseSizeAndPositionOfScrollArea(float sizeOfResultBlock);
        IBaseButtonView GetCheckButton();
        IBaseButtonView GetResetButton();
        IBaseButtonView GetSettingsButton();
        ICardInfoButtonView GetCardInfoButton();
        IBaseButtonView GetRevealingPowerUpButton();
        IBaseButtonView GetLifePowerUpButton();
        IBaseButtonView GetHintPowerUpButton();
        PowerUpModel GetPowerUpModel(int index);
    }
}
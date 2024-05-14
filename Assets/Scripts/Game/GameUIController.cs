using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GameUIController : IGameUIController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private IHapticController _hapticController;
        [Inject] private ITutorialAbilityManager _tutorialAbilityManager;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;
        
        private IGameUIView _view;
        private ICardInfoButtonController _cardInfoButtonController;
        public event EventHandler CheckFinalNumbers;
        public event EventHandler NotAbleToCheck;
        public event EventHandler ResetNumbers;
        public event EventHandler OpenSettings;
        public event EventHandler<bool> CardInfoToggleChanged;
        public event EventHandler<PowerUpClickedEventArgs> PowerUpClickedEvent;
        
        public GameUIController(IGameUIView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            _view.Init();
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _view.SetLevelId("Level " + (_levelTracker.GetLevelId() + 1));
            }
            else
            {
                _view.DisableLevelId();
                _view.IncreaseSizeAndPositionOfScrollArea(44f);
            }

            IBaseButtonController checkButtonController = _baseButtonControllerFactory.Create(_view.GetCheckButton());
            checkButtonController.Initialize(OnClickCheckButton);

            IBaseButtonController resetButtonController = _baseButtonControllerFactory.Create(_view.GetResetButton());
            resetButtonController.Initialize(OnClickResetButton);
            resetButtonController.SetText("C");

            IBaseButtonController settingsButtonController =
                _baseButtonControllerFactory.Create(_view.GetSettingsButton());
            settingsButtonController.Initialize(OnClickSettings);

            IBaseButtonController revealingPowerUpButtonController =
                _baseButtonControllerFactory.Create(_view.GetRevealingPowerUpButton());
            revealingPowerUpButtonController.Initialize(OnClickRevealingPowerUp);
            
            IBaseButtonController lifePowerUpButtonController =
                _baseButtonControllerFactory.Create(_view.GetLifePowerUpButton());
            lifePowerUpButtonController.Initialize(OnClickLifePowerUp);
            
            IBaseButtonController hintPowerUpButtonController =
                _baseButtonControllerFactory.Create(_view.GetHintPowerUpButton());
            hintPowerUpButtonController.Initialize(OnClickHintPowerUp);
            
            if (_levelTracker.GetLevelId() > 8)
            {
                _cardInfoButtonController = new CardInfoButtonController(_view.GetCardInfoButton());
                _cardInfoButtonController.Initialize(_tutorialAbilityManager, OnClickCardInfoButton);
            }
            else
            {
                _view.GetCardInfoButton().SetActive(false);
            }
            
        }
        
        private void OnClickCheckButton()
        {
            if(!_tutorialAbilityManager.IsCheckButtonClickable()) return;
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer || _turnOrderDeterminer.IsLocalTurn())
            {
                CheckFinalNumbers?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                NotAbleToCheck?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void OnClickResetButton()
        {
            if(!_tutorialAbilityManager.IsResetButtonClickable()) return;
            ResetNumbers?.Invoke(this,  null);
        }
        
        private void OnClickSettings()
        {
            if(!_tutorialAbilityManager.IsSettingsButtonClickable()) return;
            OpenSettings?.Invoke(this,  null);
        }

        private void OnClickCardInfoButton(bool isCardInfoToggleOn)
        {
            _hapticController.Vibrate(HapticType.ButtonClick);
            CardInfoToggleChanged?.Invoke(this, isCardInfoToggleOn);
        }

        private void OnClickRevealingPowerUp()
        {
            PowerUpClickedEvent?.Invoke(this, new PowerUpClickedEventArgs(_view.GetPowerUpModel(0)));
        }

        private void OnClickLifePowerUp()
        {
            PowerUpClickedEvent?.Invoke(this, new PowerUpClickedEventArgs(_view.GetPowerUpModel(1)));
        }

        private void OnClickHintPowerUp()
        {
            PowerUpClickedEvent?.Invoke(this, new PowerUpClickedEventArgs(_view.GetPowerUpModel(2)));
        }

        public RectTransform GetCheckButtonRectTransform()
        {
            return _view.GetCheckButton().GetRectTransform();
        }
        
        public RectTransform GetResetButtonRectTransform()
        {
            return _view.GetResetButton().GetRectTransform();
        }
        
        public RectTransform GetCardInfoButtonRectTransform()
        {
            return _view.GetCardInfoButton().GetRectTransform();
        }
        
    }

    public interface IGameUIController
    {
        void Initialize();
        event EventHandler CheckFinalNumbers;
        event EventHandler NotAbleToCheck;
        event EventHandler ResetNumbers;
        event EventHandler OpenSettings;
        event EventHandler<bool> CardInfoToggleChanged;
        RectTransform GetCheckButtonRectTransform();
        RectTransform GetResetButtonRectTransform();
        RectTransform GetCardInfoButtonRectTransform();
        event EventHandler<PowerUpClickedEventArgs> PowerUpClickedEvent;
    }

    public class PowerUpClickedEventArgs : EventArgs
    {
        public PowerUpModel powerUpModel;

        public PowerUpClickedEventArgs(PowerUpModel powerUpModel)
        {
            this.powerUpModel = powerUpModel;
        }
    }

    public enum PowerUpType
    {
        Revealing,
        Life,
        Hint
    }
}
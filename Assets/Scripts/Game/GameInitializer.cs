using System;
using Scripts;
using Zenject;

namespace Game
{
    public class GameInitializer : IGameInitializer
    {
        [Inject] private IGameUIController _gameUIController;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;
        [Inject] private IHapticController _hapticController;
        
        public event EventHandler CheckFinalNumbers;
        public event EventHandler NotAbleToCheck;
        public event EventHandler ResetNumbers;
        public event EventHandler OpenSettings;
        public event EventHandler<GameUIButtonType> PowerUpClickedEvent;
        public event EventHandler<bool> CardInfoToggleChanged;

        public void Initialize()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _gameUIController.SetUserText("Level " + (_levelTracker.GetLevelId() + 1));
                _gameUIController.SetOpponentInfoStatus(false);
            }

            else
            {
                _gameUIController.SetUserText(MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(0).playerName.ToString());
                _gameUIController.SetOpponentText(MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(1).playerName.ToString());
                _gameUIController.SetOpponentInfoStatus(true);
            }
            _gameUIController.SetPowerUpImages();
            _gameUIController.CreateGameUiButtons(OnButtonClick);
            
            if (_levelTracker.GetLevelId() > 8)
            {
                _gameUIController.CreateCardInfoButton(OnCardInfoButtonClick);
            }
            else
            {
                _gameUIController.SetCardInfoButtonStatus(false);
            }
        }

        private void OnButtonClick(GameUIButtonType buttonType)
        {
            switch (buttonType)
            {
                case GameUIButtonType.Check:
                    if (_levelTracker.GetGameOption() == GameOption.SinglePlayer || _turnOrderDeterminer.IsLocalTurn())
                    {
                        CheckFinalNumbers?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        NotAbleToCheck?.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case GameUIButtonType.Reset:
                    ResetNumbers?.Invoke(this,  EventArgs.Empty);
                    break;
                case GameUIButtonType.Settings:
                    OpenSettings?.Invoke(this,  EventArgs.Empty);
                    break;
                case GameUIButtonType.RevealingPowerUp:
                    PowerUpClickedEvent?.Invoke(this, GameUIButtonType.RevealingPowerUp);
                    break;
                case GameUIButtonType.LifePowerUp:
                    PowerUpClickedEvent?.Invoke(this, GameUIButtonType.LifePowerUp);
                    break;
                case GameUIButtonType.HintPowerUp:
                    PowerUpClickedEvent?.Invoke(this, GameUIButtonType.HintPowerUp);
                    break;
                default:
                    break;
            }
        }

        private void OnCardInfoButtonClick(bool isCardInfoToggleOn)
        {
            _hapticController.Vibrate(HapticType.ButtonClick);
            CardInfoToggleChanged?.Invoke(this, isCardInfoToggleOn);
        }
    }

    public interface IGameInitializer
    {
        void Initialize();
        event EventHandler CheckFinalNumbers;
        event EventHandler NotAbleToCheck;
        event EventHandler ResetNumbers;
        event EventHandler OpenSettings;
        event EventHandler<GameUIButtonType> PowerUpClickedEvent;
        event EventHandler<bool> CardInfoToggleChanged;

    }
}
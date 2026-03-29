using System;
using Game;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        private IHapticController _hapticController;
        private IFadePanelController _fadePanelController;
        private ITargetNumberCreator _targetNumberCreator;
        private ILevelTracker _levelTracker;
        private IRoundStateManager _roundStateManager;
        private IGamePowerUpAreaController _gamePowerUpAreaController;
        private GameUIButtonType _activePowerUpType;
        
        public event EventHandler RemoveBoardHolderEvent;
        public event EventHandler<LockedCardInfo> RevealWagonEvent;
        public event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        public event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        public event EventHandler AddLifeEvent;
        [Inject]
        public PowerUpMessageController(IHapticController hapticController,
            IGamePowerUpAreaController gamePowerUpAreaController, IFadePanelController fadePanelController,
            ITargetNumberCreator targetNumberCreator, ILevelTracker levelTracker, IRoundStateManager roundStateManager)
        {
            _hapticController = hapticController;
            _fadePanelController = fadePanelController;
            gamePowerUpAreaController.PowerUpClickedEvent += OnPowerUpClicked;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _targetNumberCreator = targetNumberCreator;
            _levelTracker = levelTracker;
            _roundStateManager = roundStateManager;
        }
        
        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            switch (powerUpType)
            {
                case GameUIButtonType.LifePowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        OnClosePowerUp();
                    }
                    if (!CanAddExtraLives()) return;
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Life)) return;
                    _gamePowerUpAreaController.Refresh();
                    AddLifeEvent?.Invoke(this, EventArgs.Empty);
                    break;
                case GameUIButtonType.BombPowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        OnClosePowerUp();
                    }
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Bomb)) return;
                    _gamePowerUpAreaController.Refresh();
                    RemoveBoardHolderEvent?.Invoke(this, EventArgs.Empty);
                    break;
                case GameUIButtonType.RevealingPowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        OnClosePowerUp();
                    }
                    else if (_levelTracker.GetPowerUpCount(RewardType.Revealing) <= 0)
                    {
                        return;
                    }
                    else
                    {
                        _activePowerUpType = GameUIButtonType.RevealingPowerUp;
                        _fadePanelController.SetBoardFadeImageStatus(true);
                        OpenPowerUpEvent?.Invoke(this, powerUpType);
                    }
                    break;
            }
            
        }

        private void OnClosePowerUp()
        {
            if (_activePowerUpType == GameUIButtonType.Default) return;
            _fadePanelController.SetBoardFadeImageStatus(false);
            ClosePowerUpEvent?.Invoke(this, _activePowerUpType);
            _activePowerUpType = GameUIButtonType.Default;
        }

        
        public void BoardIsClicked(int boardHolderIndex)
        {
            if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
            {
                if (!_levelTracker.TryConsumePowerUp(RewardType.Revealing))
                {
                    OnClosePowerUp();
                    return;
                }

                _gamePowerUpAreaController.Refresh();

                _hapticController.Vibrate(HapticType.CardRelease);
                int cardNumber = _targetNumberCreator.GetTargetCardsList()[boardHolderIndex];
                int cardIndex = cardNumber - 1;
                RevealWagonEvent?.Invoke(this, new LockedCardInfo(boardHolderIndex, cardIndex));
                OnClosePowerUp();
            }
        }

        private bool CanAddExtraLives()
        {
            const int extraLifeCount = 3;
            return _roundStateManager.GetRemainingGuessCount() + extraLifeCount <= _roundStateManager.GetMaxGuessCount();
        }
    }

    public interface IPowerUpMessageController
    {
        event EventHandler RemoveBoardHolderEvent;
        event EventHandler<LockedCardInfo> RevealWagonEvent;
        event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        event EventHandler AddLifeEvent;
        void BoardIsClicked(int boardHolderIndex);
    }
    
    public class LockedCardInfo : EventArgs
    {
        public int BoardHolderIndex;
        public int TargetCardIndex;

        public LockedCardInfo(int boardHolderIndex, int targetCardIndex)
        {
            BoardHolderIndex = boardHolderIndex;
            TargetCardIndex = targetCardIndex;
        }
    }
}

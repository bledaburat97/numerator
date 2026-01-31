using System;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        private IHapticController _hapticController;
        private IFadePanelController _fadePanelController;
        private IBoardCardIndexManager _boardCardIndexManager;
        private ITargetNumberCreator _targetNumberCreator;
        private GameUIButtonType _activePowerUpType;
        
        public event EventHandler RemoveBoardHolderEvent;
        public event EventHandler<LockedCardInfo> RevealWagonEvent;
        public event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        public event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        public event EventHandler AddLifeEvent;
        [Inject]
        public PowerUpMessageController(IHapticController hapticController, IGameUIController gameUIController,
            IFadePanelController fadePanelController, ITargetNumberCreator targetNumberCreator)
        {
            _hapticController = hapticController;
            _fadePanelController = fadePanelController;
            gameUIController.PowerUpClickedEvent += OnPowerUpClicked;
            _targetNumberCreator = targetNumberCreator;
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
                    AddLifeEvent?.Invoke(this, EventArgs.Empty);
                    //_guessManager.AddExtraLives(3);
                    break;
                case GameUIButtonType.BombPowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        OnClosePowerUp();
                    }
                    RemoveBoardHolderEvent?.Invoke(this, EventArgs.Empty);
                    break;
                case GameUIButtonType.RevealingPowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        OnClosePowerUp();
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
                _hapticController.Vibrate(HapticType.CardRelease);
                int cardNumber = _targetNumberCreator.GetTargetCardsList()[boardHolderIndex];
                int cardIndex = cardNumber - 1;
                Debug.Log($"Reveal Card Index: {cardIndex}");
                RevealWagonEvent?.Invoke(this, new LockedCardInfo(boardHolderIndex, cardIndex));
                OnClosePowerUp();
            }
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
using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        private IPowerUpMessagePopupView _view;
        private IHapticController _hapticController;
        private IFadePanelController _fadePanelController;
        private Dictionary<GameUIButtonType, BasePowerUpController> _powerUps;
        private IBaseButtonController _closeButton;
        private IBaseButtonController _continueButton;
        private IBoardCardIndexManager _boardCardIndexManager;
        private ITargetNumberCreator _targetNumberCreator;
        private GameUIButtonType _activePowerUpType;
        
        public event EventHandler RemoveBoardHolderEvent;
        public event EventHandler<LockedCardInfo> RevealWagonEvent;
        public event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        public event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        public event EventHandler AddLifeEvent;
        [Inject]
        public PowerUpMessageController(IHapticController hapticController,
            IGameUIController gameUIController, BaseButtonControllerFactory baseButtonControllerFactory,
            IFadePanelController fadePanelController, IPowerUpMessagePopupView view,
            ITargetNumberCreator targetNumberCreator)
        {
            _hapticController = hapticController;
            _fadePanelController = fadePanelController;
            _view = view;
            gameUIController.PowerUpClickedEvent += OnPowerUpClicked;
            _closeButton = baseButtonControllerFactory.Create(_view.GetCloseButton(), OnClosePowerUp);
            _continueButton = baseButtonControllerFactory.Create(_view.GetContinueButton(), OnUsePowerUp);
            _targetNumberCreator = targetNumberCreator;
            CreatePowerUps();
        }

        private void CreatePowerUps()
        {
            _powerUps = new Dictionary<GameUIButtonType, BasePowerUpController>();
            _powerUps.Add(GameUIButtonType.RevealingPowerUp, new RevealingPowerUpController(_hapticController, _view, _fadePanelController));
            _powerUps.Add(GameUIButtonType.LifePowerUp, new LifePowerUpController(_hapticController, _view, _fadePanelController));
            _powerUps.Add(GameUIButtonType.BombPowerUp, new BombPowerUpController(_hapticController, _view, _fadePanelController));
        }
        
        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            _activePowerUpType = powerUpType;
            _powerUps[powerUpType].Activate(_continueButton);
            OpenPowerUpEvent?.Invoke(this, powerUpType);
        }

        private void OnClosePowerUp()
        {
            if (_activePowerUpType == GameUIButtonType.Default) return;
            _fadePanelController.SetFadeImageStatus(false);
            _view.SetStatus(false);
            ClosePowerUpEvent?.Invoke(this, _activePowerUpType);
        }

        private void OnUsePowerUp()
        {
            switch (_activePowerUpType)
            {
                case GameUIButtonType.LifePowerUp:
                    AddLifeEvent?.Invoke(this, EventArgs.Empty);
                    OnClosePowerUp();
                    //_guessManager.AddExtraLives(3);
                    break;
                case GameUIButtonType.BombPowerUp:
                    RemoveBoardHolderEvent?.Invoke(this, EventArgs.Empty);
                    OnClosePowerUp();
                    break;
                default:
                    break;
            }
        }

        public void BoardIsClicked(int boardHolderIndex)
        {
            if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
            {
                _hapticController.Vibrate(HapticType.CardRelease);
                int cardNumber = _targetNumberCreator.GetTargetCardsList()[boardHolderIndex];
                int cardIndex = cardNumber - 1;
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
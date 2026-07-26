using System;
using Game;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        private IFadePanelController _fadePanelController;
        private ILevelTracker _levelTracker;
        private IRoundStateManager _roundStateManager;
        private IGamePowerUpAreaController _gamePowerUpAreaController;
        private ICardItemInfoManager _cardItemInfoManager;
        private IBoardHolderCountManager _boardHolderCountManager;
        private GameUIButtonType _activePowerUpType = GameUIButtonType.Default;
        
        public event EventHandler RemoveBoardHolderEvent;
        public event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        public event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        public event EventHandler AddLifeEvent;
        [Inject]
        public PowerUpMessageController(IGamePowerUpAreaController gamePowerUpAreaController,
            IFadePanelController fadePanelController, ILevelTracker levelTracker, IRoundStateManager roundStateManager,
            ICardItemInfoManager cardItemInfoManager, IBoardHolderCountManager boardHolderCountManager)
        {
            _fadePanelController = fadePanelController;
            gamePowerUpAreaController.PowerUpClickedEvent += OnPowerUpClicked;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _levelTracker = levelTracker;
            _roundStateManager = roundStateManager;
            _cardItemInfoManager = cardItemInfoManager;
            _boardHolderCountManager = boardHolderCountManager;
        }
        
        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            switch (powerUpType)
            {
                case GameUIButtonType.LifePowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        CloseActivePowerUp();
                    }
                    if (!CanAddExtraLives()) return;
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Life)) return;
                    _gamePowerUpAreaController.Refresh();
                    AddLifeEvent?.Invoke(this, EventArgs.Empty);
                    break;
                case GameUIButtonType.BombPowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        CloseActivePowerUp();
                    }
                    if (!CanUseBomb()) return;
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Bomb)) return;
                    RemoveBoardHolderEvent?.Invoke(this, EventArgs.Empty);
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.RevealingPowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        CloseActivePowerUp();
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

        public void CloseActivePowerUp()
        {
            if (_activePowerUpType == GameUIButtonType.Default) return;
            _fadePanelController.SetBoardFadeImageStatus(false);
            ClosePowerUpEvent?.Invoke(this, _activePowerUpType);
            _activePowerUpType = GameUIButtonType.Default;
        }

        private bool CanAddExtraLives()
        {
            const int extraLifeCount = 3;
            return _roundStateManager.GetRemainingGuessCount() + extraLifeCount <= _roundStateManager.GetMaxGuessCount();
        }

        private bool CanUseBomb()
        {
            return _roundStateManager.GetTriedCardsList().Count == 0 &&
                   _boardHolderCountManager.GetRemovedBoardHolderCount() == 0 &&
                   !HasRevealedCard();
        }

        private bool HasRevealedCard()
        {
            var cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            if (cardItemInfoList == null) return false;

            foreach (CardItemInfo cardItemInfo in cardItemInfoList)
            {
                if (cardItemInfo != null && cardItemInfo.isLocked) return true;
            }

            return false;
        }
    }

    public interface IPowerUpMessageController
    {
        event EventHandler RemoveBoardHolderEvent;
        event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        event EventHandler AddLifeEvent;
        void CloseActivePowerUp();
    }
}

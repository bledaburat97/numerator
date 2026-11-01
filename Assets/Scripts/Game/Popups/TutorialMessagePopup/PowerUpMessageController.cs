using System;
using Game;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        [InjectOptional] private HelperEffect _helper;
        [InjectOptional] private PairEffect _pair;
        [InjectOptional] private BoardSplitService _split;
        [InjectOptional] private TutorialInputGate _tutorialGate;
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
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer) return;
            if (_tutorialGate != null && !_tutorialGate.AllowsButton(powerUpType)) return;
            if (_tutorialGate != null && _tutorialGate.TryExplain(powerUpType, () => OnPowerUpClicked(sender, powerUpType))) return;
            switch (powerUpType)
            {
                case GameUIButtonType.SplitPowerUp:
                    if (_split == null || !_split.CanApply) return;
                    CloseActivePowerUp();
                    _split.TryApply(() => _levelTracker.TryConsumePowerUp(RewardType.Split));
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.PairPowerUp:
                    if (_pair == null || !_pair.CanApply) return;
                    CloseActivePowerUp();
                    _pair.TryApply(() => _levelTracker.TryConsumePowerUp(RewardType.Pair));
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.HelperPowerUp:
                    if (_helper == null || !_helper.CanApply) return;
                    CloseActivePowerUp();
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Helper)) return;
                    _helper.TryApply();
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.LifePowerUp:
                    if (_activePowerUpType == GameUIButtonType.RevealingPowerUp)
                    {
                        CloseActivePowerUp();
                    }
                    if (!CanAddExtraLives()) return;
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Life)) return;
                    AddLifeEvent?.Invoke(this, EventArgs.Empty);
                    _gamePowerUpAreaController.Refresh();
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
            return _roundStateManager.GetRemainingGuessCount() > 0 && _roundStateManager.GetRemainingGuessCount() + extraLifeCount <= _roundStateManager.GetMaxGuessCount();
        }

        private bool CanUseBomb()
        {
            return _boardHolderCountManager.CanShrink && _boardHolderCountManager.GetBoardHolderCount() > 1 &&
                   _roundStateManager.GetTriedCardsList().Count == 0 &&
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

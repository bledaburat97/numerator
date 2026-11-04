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
            ICardItemInfoManager cardItemInfoManager, IBoardHolderCountManager boardHolderCountManager,IGameUIController ui)
        {
            _fadePanelController = fadePanelController;
            gamePowerUpAreaController.PowerUpClickedEvent += OnPowerUpClicked;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _levelTracker = levelTracker;
            _roundStateManager = roundStateManager;
            _cardItemInfoManager = cardItemInfoManager;
            _boardHolderCountManager = boardHolderCountManager;
            _fadePanelController.SetBoardDismiss(CloseActivePowerUp);
            ui.ResetNumbers+=CancelOnAction;ui.CheckFinalNumbers+=CancelOnAction;ui.OpenSettings+=CancelOnAction;
        }
        
        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer) return;
            if (_tutorialGate != null && !_tutorialGate.AllowsButton(powerUpType)) return;
            if(powerUpType!=GameUIButtonType.BombPowerUp && powerUpType!=GameUIButtonType.RevealingPowerUp
                && powerUpType!=GameUIButtonType.LifePowerUp && powerUpType!=GameUIButtonType.HelperPowerUp
                && powerUpType!=GameUIButtonType.PairPowerUp && powerUpType!=GameUIButtonType.SplitPowerUp)return;
            if(_activePowerUpType==GameUIButtonType.RevealingPowerUp && powerUpType==_activePowerUpType){CloseActivePowerUp();return;}
            var reward=PowerUpTypeUtility.ToRewardType(powerUpType);
            if(!PowerUpAvailability.CanUse(reward,_levelTracker,_roundStateManager,_cardItemInfoManager,_boardHolderCountManager,_helper,_pair,_split))return;
            CloseActivePowerUp();
            if (_tutorialGate != null && _tutorialGate.TryExplain(powerUpType)) return;
            switch (powerUpType)
            {
                case GameUIButtonType.SplitPowerUp:
                    _split.TryApply(() => _levelTracker.TryConsumePowerUp(RewardType.Split));
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.PairPowerUp:
                    _pair.TryApply(() => _levelTracker.TryConsumePowerUp(RewardType.Pair));
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.HelperPowerUp:
                    _helper.TryApply(()=>_levelTracker.TryConsumePowerUp(RewardType.Helper));
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.LifePowerUp:
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Life)) return;
                    AddLifeEvent?.Invoke(this, EventArgs.Empty);
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.BombPowerUp:
                    if (!_levelTracker.TryConsumePowerUp(RewardType.Bomb)) return;
                    RemoveBoardHolderEvent?.Invoke(this, EventArgs.Empty);
                    _gamePowerUpAreaController.Refresh();
                    break;
                case GameUIButtonType.RevealingPowerUp:
                    _activePowerUpType = GameUIButtonType.RevealingPowerUp;
                    _fadePanelController.SetBoardFadeImageStatus(true);
                    OpenPowerUpEvent?.Invoke(this, powerUpType);
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

        private void CancelOnAction(object sender,EventArgs args)=>CloseActivePowerUp();
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

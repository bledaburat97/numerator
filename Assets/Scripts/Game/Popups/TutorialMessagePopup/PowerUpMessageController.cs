using System;
using Game;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        [Inject] private LazyInject<PowerUpUseService> _uses;
        [InjectOptional] private TutorialInputGate _tutorialGate;
        private readonly IFadePanelController _fadePanelController;
        private readonly ILevelTracker _levelTracker;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;
        private GameUIButtonType _activePowerUpType = GameUIButtonType.Default;
        
        public event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        public event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        [Inject]
        public PowerUpMessageController(IGamePowerUpAreaController gamePowerUpAreaController,
            IFadePanelController fadePanelController, ILevelTracker levelTracker,IGameUIController ui)
        {
            _fadePanelController = fadePanelController;
            gamePowerUpAreaController.PowerUpClickedEvent += OnPowerUpClicked;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _levelTracker = levelTracker;
            _fadePanelController.SetBoardDismiss(CloseActivePowerUp);
            ui.ResetNumbers+=CancelOnAction;ui.CheckFinalNumbers+=CancelOnAction;ui.OpenSettings+=CancelOnAction;ui.OpenInformation+=CancelOnAction;
        }
        
        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer) return;
            if (_tutorialGate != null && !_tutorialGate.AllowsButton(powerUpType)) return;
            if(powerUpType!=GameUIButtonType.BombPowerUp && powerUpType!=GameUIButtonType.RevealingPowerUp
                && powerUpType!=GameUIButtonType.LifePowerUp && powerUpType!=GameUIButtonType.HelperPowerUp
                && powerUpType!=GameUIButtonType.PairPowerUp && powerUpType!=GameUIButtonType.SplitPowerUp && powerUpType!=GameUIButtonType.UltraPowerUp)return;
            if(_activePowerUpType==GameUIButtonType.RevealingPowerUp && powerUpType==_activePowerUpType){CloseActivePowerUp();return;}
            var reward=PowerUpTypeUtility.ToRewardType(powerUpType);
            if(!_uses.Value.CanUse(reward))return;
            CloseActivePowerUp();
            if (_tutorialGate != null && _tutorialGate.TryExplain(powerUpType)) return;
            if(powerUpType==GameUIButtonType.RevealingPowerUp)
            {
                _activePowerUpType=powerUpType;
                _fadePanelController.SetBoardFadeImageStatus(true);
                OpenPowerUpEvent?.Invoke(this,powerUpType);
                return;
            }
            _uses.Value.TryApply(reward);
            _gamePowerUpAreaController.Refresh();
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
        event EventHandler<GameUIButtonType> OpenPowerUpEvent;
        event EventHandler<GameUIButtonType> ClosePowerUpEvent;
        void CloseActivePowerUp();
    }
}

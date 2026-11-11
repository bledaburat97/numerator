using System;
using Game;
using Zenject;

namespace Scripts
{
    public class RevealingPowerUpController : IRevealingPowerUpController
    {
        [Inject] private LazyInject<PowerUpUseService> _uses;
        private readonly IBoardAreaController _boardAreaController;
        private readonly IPowerUpMessageController _powerUpMessageController;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;
        private readonly IHapticController _hapticController;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private bool _isActive;

        public bool IsActive => _isActive;

        [Inject]
        public RevealingPowerUpController(
            IBoardAreaController boardAreaController,
            IPowerUpMessageController powerUpMessageController,
            IGamePowerUpAreaController gamePowerUpAreaController,
            IHapticController hapticController,
            ITargetNumberCreator targetNumberCreator)
        {
            _boardAreaController = boardAreaController;
            _powerUpMessageController = powerUpMessageController;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _hapticController = hapticController;
            _targetNumberCreator = targetNumberCreator;

            _powerUpMessageController.OpenPowerUpEvent += OnPowerUpOpened;
            _powerUpMessageController.ClosePowerUpEvent += OnPowerUpClosed;
        }

        private void OnPowerUpOpened(object sender, GameUIButtonType powerUpType)
        {
            if (powerUpType != GameUIButtonType.RevealingPowerUp) return;

            _isActive = true;
            var eligible=new System.Collections.Generic.List<int>();
            var target=_targetNumberCreator.GetTargetCardsList();
            for(int slot=0;slot<target.Count;slot++)
                if(_uses.Value.CanUse(RewardType.Revealing,slot))eligible.Add(slot);
            _boardAreaController.SetupTutorialModeOnBoardHolders(eligible);
        }

        private void OnPowerUpClosed(object sender, GameUIButtonType powerUpType)
        {
            if (powerUpType != GameUIButtonType.RevealingPowerUp) return;

            _isActive = false;
            _boardAreaController.CleanupTutorialModeOnShinyBoardHolders();
        }

        public bool TryRequestRevealCard(int boardHolderIndex)
        {
            if (!_isActive) return false;
            if (!_uses.Value.CanUse(RewardType.Revealing,boardHolderIndex))
            {
                _powerUpMessageController.CloseActivePowerUp();
                return false;
            }

            if (_uses.Value.TryApply(RewardType.Revealing,boardHolderIndex)!=PowerUpUseResult.Applied)
            {
                _powerUpMessageController.CloseActivePowerUp();
                return false;
            }

            _hapticController.Vibrate(HapticType.CardRelease);
            _gamePowerUpAreaController.Refresh();
            _powerUpMessageController.CloseActivePowerUp();
            return true;
        }

    }

    public interface IRevealingPowerUpController
    {
        bool IsActive { get; }
        bool TryRequestRevealCard(int boardHolderIndex);
    }

}

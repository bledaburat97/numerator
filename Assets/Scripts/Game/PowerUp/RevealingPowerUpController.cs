using System;
using Game;
using Zenject;

namespace Scripts
{
    public class RevealingPowerUpController : IRevealingPowerUpController
    {
        private readonly IBoardAreaController _boardAreaController;
        private readonly IPowerUpMessageController _powerUpMessageController;
        private readonly ILevelTracker _levelTracker;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;
        private readonly IHapticController _hapticController;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private bool _isActive;

        public bool IsActive => _isActive;
        public event EventHandler<LockedCardInfo> RevealCardRequestedEvent;

        [Inject]
        public RevealingPowerUpController(
            IBoardAreaController boardAreaController,
            IPowerUpMessageController powerUpMessageController,
            ILevelTracker levelTracker,
            IGamePowerUpAreaController gamePowerUpAreaController,
            IHapticController hapticController,
            ITargetNumberCreator targetNumberCreator)
        {
            _boardAreaController = boardAreaController;
            _powerUpMessageController = powerUpMessageController;
            _levelTracker = levelTracker;
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
            _boardAreaController.SetupTutorialModeOnEmptyBoardHolders();
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
            if (!TryGetTargetCardIndex(boardHolderIndex, out int cardIndex))
            {
                _powerUpMessageController.CloseActivePowerUp();
                return false;
            }

            if (!_levelTracker.TryConsumePowerUp(RewardType.Revealing))
            {
                _powerUpMessageController.CloseActivePowerUp();
                return false;
            }

            _hapticController.Vibrate(HapticType.CardRelease);
            RevealCardRequestedEvent?.Invoke(this, new LockedCardInfo(boardHolderIndex, cardIndex));
            _gamePowerUpAreaController.Refresh();
            _powerUpMessageController.CloseActivePowerUp();
            return true;
        }

        private bool TryGetTargetCardIndex(int boardHolderIndex, out int cardIndex)
        {
            cardIndex = -1;
            var targetCards = _targetNumberCreator.GetTargetCardsList();
            if (boardHolderIndex < 0 || boardHolderIndex >= targetCards.Count) return false;

            cardIndex = targetCards[boardHolderIndex] - 1;
            return true;
        }
    }

    public interface IRevealingPowerUpController
    {
        bool IsActive { get; }
        event EventHandler<LockedCardInfo> RevealCardRequestedEvent;
        bool TryRequestRevealCard(int boardHolderIndex);
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

using System;
using Game;
using Zenject;

namespace Scripts
{
    public class RevealingPowerUpController : IRevealingPowerUpController
    {
        [InjectOptional] private IceBoardService _ice;
        private readonly IBoardAreaController _boardAreaController;
        private readonly IPowerUpMessageController _powerUpMessageController;
        private readonly ILevelTracker _levelTracker;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;
        private readonly IHapticController _hapticController;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly ICardItemInfoManager _info;
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
            ITargetNumberCreator targetNumberCreator,ICardItemInfoManager info)
        {
            _boardAreaController = boardAreaController;
            _powerUpMessageController = powerUpMessageController;
            _levelTracker = levelTracker;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _hapticController = hapticController;
            _targetNumberCreator = targetNumberCreator;
            _info=info;

            _powerUpMessageController.OpenPowerUpEvent += OnPowerUpOpened;
            _powerUpMessageController.ClosePowerUpEvent += OnPowerUpClosed;
        }

        private void OnPowerUpOpened(object sender, GameUIButtonType powerUpType)
        {
            if (powerUpType != GameUIButtonType.RevealingPowerUp) return;

            _isActive = true;
            var eligible=new System.Collections.Generic.List<int>();
            var target=_targetNumberCreator.GetTargetCardsList();
            var cards=_info.GetCardItemInfoList();
            for(int slot=0;slot<target.Count;slot++)
                if(_ice?.IsBlocked(slot)!=true && target[slot]>0 && target[slot]<=cards.Count && cards[target[slot]-1].isExisted && !cards[target[slot]-1].isLocked)eligible.Add(slot);
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
            if(_ice?.IsBlocked(boardHolderIndex)==true)return false;
            var targetCards = _targetNumberCreator.GetTargetCardsList();
            if (targetCards==null || boardHolderIndex < 0 || boardHolderIndex >= targetCards.Count) return false;

            cardIndex = targetCards[boardHolderIndex] - 1;
            var info=_info.GetCardItemInfoList();
            return cardIndex>=0 && cardIndex<info.Count && info[cardIndex].isExisted && !info[cardIndex].isLocked;
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

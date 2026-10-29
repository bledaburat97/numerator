using System;
using Game;
using Zenject;

namespace Scripts
{
    public class BombPowerUpController : IBombPowerUpController
    {
        private readonly IBoardAreaController _boardAreaController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly IRoundStateManager _roundStateManager;

        [Inject]
        public BombPowerUpController(IPowerUpMessageController powerUpMessageController,
            IBoardAreaController boardAreaController,
            ICardPlacementCoordinator cardPlacementCoordinator,
            ITargetNumberCreator targetNumberCreator,
            ICardItemInfoManager cardItemInfoManager,
            IBoardHolderCountManager boardHolderCountManager,
            IRoundStateManager roundStateManager)
        {
            _boardAreaController = boardAreaController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _targetNumberCreator = targetNumberCreator;
            _cardItemInfoManager = cardItemInfoManager;
            _boardHolderCountManager = boardHolderCountManager;
            _roundStateManager = roundStateManager;
            powerUpMessageController.RemoveBoardHolderEvent += OnRemoveBoardHolder;
        }

        private void OnRemoveBoardHolder(object sender, EventArgs args)
        {
            int boardCount = _boardHolderCountManager.GetBoardHolderCount();
            if (boardCount <= 1 || _boardHolderCountManager.GetRemovedBoardHolderCount() > 0
                || _roundStateManager.GetTriedCardsList().Count > 0
                || _targetNumberCreator.GetTargetCardsList()?.Count != boardCount) return;

            _cardPlacementCoordinator.TryResetPositionOfCardOnExplodedBoardHolder();
            if (!_targetNumberCreator.TryRemoveLastTargetCard()) return;
            _boardAreaController.RemoveLastBoardHolder();
            _cardItemInfoManager.RemoveLastCardHolderIndicator();
        }
    }

    public interface IBombPowerUpController
    {
    }
}

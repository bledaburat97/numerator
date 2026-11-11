using System;
using Game;
using Zenject;

namespace Scripts
{
    public class BombPowerUpController : IBombPowerUpController
    {
        [InjectOptional] private PairEffect _pair;
        [InjectOptional] private DynamiteService _dynamite;
        private readonly IBoardAreaController _boardAreaController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly IRoundStateManager _roundStateManager;

        [Inject]
        public BombPowerUpController(IBoardAreaController boardAreaController,
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
        }

        public bool TryApply(LevelSaveData committed)
        {
            int boardCount = _boardHolderCountManager.GetBoardHolderCount();
            if (!_boardHolderCountManager.CanShrink || boardCount <= 1 || _boardHolderCountManager.GetRemovedBoardHolderCount() > 0
                || _roundStateManager.GetTriedCardsList().Count > 0
                || _targetNumberCreator.GetTargetCardsList()?.Count != boardCount) return false;

            _cardPlacementCoordinator.TryResetPositionOfCardOnExplodedBoardHolder();
            if (!_targetNumberCreator.TryRemoveLastTargetCard()) return false;
            _boardAreaController.RemoveLastBoardHolder();
            _cardItemInfoManager.RemoveLastCardHolderIndicator();
            if(committed.Dynamite!=null && _dynamite?.Current!=null)_dynamite.Current.BoardIndex=committed.Dynamite.BoardIndex;
            _dynamite?.Prepare();
            // Pair reconciliation can checkpoint; the hidden slot must already be valid.
            _pair?.RestoreClue(committed.PairClue);
            return true;
        }
    }

    public interface IBombPowerUpController
    {
        bool TryApply(LevelSaveData committed);
    }
}

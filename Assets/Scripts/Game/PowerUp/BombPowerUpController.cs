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

        [Inject]
        public BombPowerUpController(IBoardAreaController boardAreaController,
            ICardPlacementCoordinator cardPlacementCoordinator,
            ITargetNumberCreator targetNumberCreator,
            ICardItemInfoManager cardItemInfoManager,
            IBoardHolderCountManager boardHolderCountManager)
        {
            _boardAreaController = boardAreaController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _targetNumberCreator = targetNumberCreator;
            _cardItemInfoManager = cardItemInfoManager;
            _boardHolderCountManager = boardHolderCountManager;
        }

        public bool ApplyCommitted(int expectedBefore,LevelSaveData committed)
        {
            // These are scene consistency checks, not a second availability decision.
            if(_boardHolderCountManager.GetBoardHolderCount()!=expectedBefore || _targetNumberCreator.GetTargetCardsList()?.Count!=expectedBefore)return false;
            _cardPlacementCoordinator.TryResetPositionOfCardOnExplodedBoardHolder();
            _targetNumberCreator.ApplyCommittedTarget(committed.TargetCards);
            if(!_boardAreaController.ApplyCommittedShrink(expectedBefore,committed.TargetCards.Count))return false;
            _dynamite?.ApplyCommitted(committed.Dynamite);
            _cardItemInfoManager.ApplyPositionOptions(committed.CardItemInfoList);
            _pair?.RestoreClue(committed.PairClue);return true;
        }
    }

    public interface IBombPowerUpController
    {
        bool ApplyCommitted(int expectedBefore,LevelSaveData committed);
    }
}

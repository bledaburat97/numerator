using System;
using Zenject;

namespace Scripts
{
    public class BombPowerUpController : IBombPowerUpController
    {
        private readonly IBoardAreaController _boardAreaController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;

        [Inject]
        public BombPowerUpController(IPowerUpMessageController powerUpMessageController,
            IBoardAreaController boardAreaController,
            ICardPlacementCoordinator cardPlacementCoordinator)
        {
            _boardAreaController = boardAreaController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            powerUpMessageController.RemoveBoardHolderEvent += OnRemoveBoardHolder;
        }

        private void OnRemoveBoardHolder(object sender, EventArgs args)
        {
            _cardPlacementCoordinator.TryResetPositionOfCardOnExplodedBoardHolder();
            _boardAreaController.RemoveLastBoardHolder();
        }
    }

    public interface IBombPowerUpController
    {
    }
}

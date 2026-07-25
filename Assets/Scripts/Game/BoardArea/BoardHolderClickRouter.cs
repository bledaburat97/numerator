using System;
using Zenject;

namespace Scripts
{
    public class BoardHolderClickRouter : IBoardHolderClickRouter
    {
        private readonly IRevealingPowerUpController _revealingPowerUpController;

        public event EventHandler<int> BoardHolderPlacementRequestedEvent;

        [Inject]
        public BoardHolderClickRouter(
            IBoardAreaController boardAreaController,
            IRevealingPowerUpController revealingPowerUpController)
        {
            _revealingPowerUpController = revealingPowerUpController;
            boardAreaController.BoardHolderClickedEvent += OnBoardHolderClicked;
        }

        private void OnBoardHolderClicked(object sender, int boardHolderIndex)
        {
            if (_revealingPowerUpController.IsActive)
            {
                _revealingPowerUpController.TryRequestRevealCard(boardHolderIndex);
                return;
            }

            BoardHolderPlacementRequestedEvent?.Invoke(this, boardHolderIndex);
        }
    }

    public interface IBoardHolderClickRouter
    {
        event EventHandler<int> BoardHolderPlacementRequestedEvent;
    }
}

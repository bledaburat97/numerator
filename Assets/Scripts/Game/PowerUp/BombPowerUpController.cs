using System;
using Zenject;

namespace Scripts
{
    public class BombPowerUpController : IBombPowerUpController
    {
        private readonly IBoardAreaController _boardAreaController;

        [Inject]
        public BombPowerUpController(IPowerUpMessageController powerUpMessageController,
            IBoardAreaController boardAreaController)
        {
            _boardAreaController = boardAreaController;
            powerUpMessageController.RemoveBoardHolderEvent += OnRemoveBoardHolder;
        }

        private void OnRemoveBoardHolder(object sender, EventArgs args)
        {
            _boardAreaController.RemoveLastBoardHolder();
        }
    }

    public interface IBombPowerUpController
    {
    }
}

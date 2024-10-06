using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        private IPowerUpMessagePopupView _view;
        private IInitialCardAreaController _initialCardAreaController;
        private IHapticController _hapticController;
        private IBoardAreaController _boardAreaController;
        private ITargetNumberCreator _targetNumberCreator;
        private IGuessManager _guessManager;
        private IFadePanelController _fadePanelController;
        private IGameInitializer _gameInitializer;
        private Dictionary<GameUIButtonType, BasePowerUpController> _powerUps;
        private IBaseButtonController _closeButton;
        private IBaseButtonController _continueButton;
        
        [Inject]
        public PowerUpMessageController(IInitialCardAreaController initialCardAreaController, IHapticController hapticController, 
            IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator,
            IGameUIController gameUIController, IGuessManager guessManager, BaseButtonControllerFactory baseButtonControllerFactory, IFadePanelController fadePanelController, IGameInitializer gameInitializer, IPowerUpMessagePopupView view)
        {
            _initialCardAreaController = initialCardAreaController;
            _hapticController = hapticController;
            _boardAreaController = boardAreaController;
            _targetNumberCreator = targetNumberCreator;
            _guessManager = guessManager;
            _fadePanelController = fadePanelController;
            _gameInitializer = gameInitializer;
            _view = view;
            gameUIController.PowerUpClickedEvent += OnPowerUpClicked;
            _closeButton = baseButtonControllerFactory.Create(_view.GetCloseButton(), null);
            _continueButton = baseButtonControllerFactory.Create(_view.GetContinueButton(), null);
            CreatePowerUps();
        }

        private void CreatePowerUps()
        {
            _powerUps = new Dictionary<GameUIButtonType, BasePowerUpController>();
            _powerUps.Add(GameUIButtonType.RevealingPowerUp, new RevealingPowerUpController(_hapticController, _view, _fadePanelController));
            _powerUps.Add(GameUIButtonType.LifePowerUp, new LifePowerUpController(_hapticController, _view, _fadePanelController));
            _powerUps.Add(GameUIButtonType.BombPowerUp, new BombPowerUpController(_hapticController, _view, _fadePanelController));
        }
        
        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            _powerUps[powerUpType].Activate(_boardAreaController, _targetNumberCreator, _initialCardAreaController, _guessManager, _gameInitializer, _closeButton, _continueButton);
        }
    }

    public interface IPowerUpMessageController
    {
    }
}
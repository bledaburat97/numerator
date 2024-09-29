using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class PowerUpMessageController : IPowerUpMessageController
    {
        private IUnmaskServiceAreaView _unmaskServiceAreaView;
        private IPowerUpMessagePopupView _view;
        private IInitialCardAreaController _initialCardAreaController;
        private IHapticController _hapticController;
        private IBoardAreaController _boardAreaController;
        private ITargetNumberCreator _targetNumberCreator;
        private IGamePopupCreator _gamePopupCreator;
        private ITutorialAbilityManager _tutorialAbilityManager;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IGuessManager _guessManager;
        private BaseButtonControllerFactory _baseButtonControllerFactory;
        
        private bool _isOpen = false;
        private GameUIButtonType _activePowerUpType;
        private Dictionary<GameUIButtonType, BasePowerUpController> _powerUps;
        private IBaseButtonController _continueButton;

        [Inject]
        public PowerUpMessageController(IUnmaskServiceAreaView unmaskServiceAreaView, IInitialCardAreaController initialCardAreaController, IHapticController hapticController, 
            IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator, IGamePopupCreator gamePopupCreator, 
            ITutorialAbilityManager tutorialAbilityManager, IGameUIController gameUIController, IGuessManager guessManager, BaseButtonControllerFactory baseButtonControllerFactory, IPowerUpMessagePopupView view)
        {
            _unmaskServiceAreaView = unmaskServiceAreaView;
            _initialCardAreaController = initialCardAreaController;
            _hapticController = hapticController;
            _boardAreaController = boardAreaController;
            _targetNumberCreator = targetNumberCreator;
            _gamePopupCreator = gamePopupCreator;
            _tutorialAbilityManager = tutorialAbilityManager;
            _guessManager = guessManager;
            _baseButtonControllerFactory = baseButtonControllerFactory;
            _view = view;
            gameUIController.PowerUpClickedEvent += OnPowerUpClicked;
            CreateButtons();
            CreatePowerUps();
        }

        public void Initialize(ICardHolderModelCreator cardHolderModelCreator)
        {
            _cardHolderModelCreator = cardHolderModelCreator;
        }
        
        private void CreateButtons()
        {
            _continueButton = _baseButtonControllerFactory.Create(_view.GetContinueButton(), DeactivatePopup);
            IBaseButtonController closeButton = _baseButtonControllerFactory.Create(_view.GetCloseButton(), DeactivatePopup);
        }

        private void CreatePowerUps()
        {
            _powerUps = new Dictionary<GameUIButtonType, BasePowerUpController>();
            _powerUps.Add(GameUIButtonType.RevealingPowerUp, new RevealingPowerUpController(_hapticController, DeactivatePopup));
            _powerUps.Add(GameUIButtonType.LifePowerUp, new LifePowerUpController(_hapticController, DeactivatePopup));
            _powerUps.Add(GameUIButtonType.HintPowerUp, new HintPowerUpController(_hapticController, DeactivatePopup));
        }
        
        private void OnPowerUpClicked(object sender, GameUIButtonType powerUpType)
        {
            if (!_isOpen)
            {
                _powerUps[powerUpType].Activate(_unmaskServiceAreaView, _gamePopupCreator, _tutorialAbilityManager, 
                    _cardHolderModelCreator, _boardAreaController, _targetNumberCreator, _initialCardAreaController, _guessManager, _continueButton);
                _powerUps[powerUpType].SetPowerUpMessagePopup(_view);
                _isOpen = true;
                _activePowerUpType = powerUpType;
            }
            else
            {
                if (_activePowerUpType == powerUpType)
                {
                    DeactivatePopup();
                }
                else
                {
                    _powerUps[powerUpType].Activate(_unmaskServiceAreaView, _gamePopupCreator, _tutorialAbilityManager, 
                        _cardHolderModelCreator, _boardAreaController, _targetNumberCreator, _initialCardAreaController, _guessManager, _continueButton);
                    _powerUps[powerUpType].SetPowerUpMessagePopup(_view);
                    _isOpen = true;
                    _activePowerUpType = powerUpType;
                }
            }
        }

        private void DeactivatePopup()
        {
            _view.SetStatus(false);
            _unmaskServiceAreaView.CloseTutorialFade();
            _isOpen = false;
        }
    }

    public interface IPowerUpMessageController
    {
        void Initialize(ICardHolderModelCreator cardHolderModelCreator);
    }
}
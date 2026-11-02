using Scripts;
using Zenject;

namespace Game
{
    public sealed class RoundBuilder
    {
        private readonly ILevelDataCreator _levelDataCreator;
        private readonly ILevelSaveDataManager _levelSaveDataManager;
        private readonly IBoardAreaController _boardAreaController;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IResultAreaController _resultAreaController;
        private readonly ILifeBarController _lifeBarController;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IGameUIController _gameUIController;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;
        private readonly ICardItemLocator _cardItemLocator;
        private readonly IGuessManager _guessManager;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly ICardItemInfoPopupController _cardItemInfoPopupController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;
        private readonly ICardInteractionManager _cardInteractionManager;
        private readonly IResultManager _resultManager;
        private readonly IRoundStateManager _roundStateManager;

        [Inject]
        public RoundBuilder(
            ILevelDataCreator levelDataCreator,
            ILevelSaveDataManager levelSaveDataManager,
            IBoardAreaController boardAreaController,
            IBoardHolderCountManager boardHolderCountManager,
            ITargetNumberCreator targetNumberCreator,
            IResultAreaController resultAreaController,
            ILifeBarController lifeBarController,
            IInitialCardAreaController initialCardAreaController,
            IGameUIController gameUIController,
            IGamePowerUpAreaController gamePowerUpAreaController,
            ICardItemLocator cardItemLocator,
            IGuessManager guessManager,
            ICardItemInfoManager cardItemInfoManager,
            ICardItemInfoPopupController cardItemInfoPopupController,
            ICardPlacementCoordinator cardPlacementCoordinator,
            ICardInteractionManager cardInteractionManager,
            IResultManager resultManager,
            IRoundStateManager roundStateManager)
        {
            _levelDataCreator = levelDataCreator;
            _levelSaveDataManager = levelSaveDataManager;
            _boardAreaController = boardAreaController;
            _boardHolderCountManager = boardHolderCountManager;
            _targetNumberCreator = targetNumberCreator;
            _resultAreaController = resultAreaController;
            _lifeBarController = lifeBarController;
            _initialCardAreaController = initialCardAreaController;
            _gameUIController = gameUIController;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _cardItemLocator = cardItemLocator;
            _guessManager = guessManager;
            _cardItemInfoManager = cardItemInfoManager;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _cardInteractionManager = cardInteractionManager;
            _resultManager = resultManager;
            _roundStateManager = roundStateManager;
        }

        public void BuildSingle(bool isResumeFlow)
        {
            _roundStateManager.Initialize();
            _boardAreaController.CreateBoard();
            _targetNumberCreator.SetTargetNumber(_boardHolderCountManager.GetBoardHolderCount());
            _resultAreaController.Initialize(!isResumeFlow);
            _lifeBarController.SetFade(!isResumeFlow);
            _initialCardAreaController.Initialize(!isResumeFlow);
            _gameUIController.Initialize(!isResumeFlow);
        }
        public void InitializeSingleSystems(bool deferRewardIntroAnimation)
        {
            _cardItemLocator.Initialize();
            _cardItemInfoManager.Initialize();
            _cardPlacementCoordinator.Initialize();
            _guessManager.Initialize(deferRewardIntroAnimation);
            _cardItemInfoPopupController.Initialize();
            _cardInteractionManager.Initialize();
            _resultManager.TryAddTriedCards();
            _gamePowerUpAreaController.Refresh();
        }

        public void BuildMultiplayer()
        {
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            _initialCardAreaController.ClearInitialCardHolders();
            _resultAreaController.RemoveResultBlocks();
            _levelDataCreator.SetMultiplayerLevelData(); _levelSaveDataManager.CreateDefaultLevelSaveData();
            _roundStateManager.Initialize(); _boardAreaController.CreateBoard();
            _targetNumberCreator.SetTargetNumber(_levelDataCreator.GetLevelData().NumOfBoardHolders);
            _initialCardAreaController.Initialize(false); _resultAreaController.Initialize(false);
            _cardItemLocator.Initialize(); _cardItemInfoManager.Initialize();
            _cardPlacementCoordinator.Initialize(); _cardInteractionManager.Initialize(); _cardItemInfoPopupController.Initialize();
        }
    }
}

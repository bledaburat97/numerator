using System;
using Scripts;
using Zenject;

namespace Game
{
    public class GameInitializer : IGameInitializer
    {
        [Inject] private IGameUIController _gameUIController;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;
        [Inject] private IHapticController _hapticController;
        [Inject] private IResultAreaController _resultAreaController;
        [Inject] private IResultManager _resultManager;
        [Inject] private ITargetNumberCreator _targetNumberCreator;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private IGuessManager _guessManager;
        [Inject] private ILifeBarController _lifeBarController;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private ICardItemInfoPopupController _cardItemInfoPopupController;
        [Inject] private IFadePanelController _fadePanelController;
        [Inject] private IUnmaskServiceAreaView _unmaskServiceAreaView;
        [Inject] private IGamePopupCreator _gamePopupCreator;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private IUserReady _userReady;
        [Inject] private ICardInteractionManager _cardInteractionManager;
        [Inject] private ILevelSaveDataManager _levelSaveDataManager;
        [Inject] private IPowerUpMessageController _powerUpMessageController;
        private ICardHolderModelCreator _cardHolderModelCreator;
        
        public GameInitializer()
        {
            _cardHolderModelCreator = new CardHolderModelCreator();
        }

        public void Initialize()
        {
            _targetNumberCreator.Initialize();
            _levelDataCreator.Initialize();
            _levelSaveDataManager.SetLevelSaveData(_gameSaveService.GetSavedLevel(), _targetNumberCreator, _levelDataCreator, _levelTracker);
            _userReady.Initialize();
            _gameUIController.Initialize();
            _resultAreaController.Initialize();
            _resultManager.Initialize(_levelSaveDataManager.GetLevelSaveData().TriedCardsList, _targetNumberCreator.GetTargetCardsList(), _levelDataCreator.GetLevelData().NumOfBoardHolders);
            _cardItemLocator.Initialize();
            _guessManager.Initialize(_levelDataCreator.GetLevelData().MaxNumOfTries, _levelSaveDataManager.GetLevelSaveData().RemainingGuessCount, _levelDataCreator.GetLevelData().NumOfBoardHolders);
            _cardHolderModelCreator.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders, _levelDataCreator.GetLevelData().NumOfCards);
            _boardAreaController.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders, _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board));
            _cardItemInfoManager.Initialize(_levelSaveDataManager.GetLevelSaveData().CardItemInfoList, _levelDataCreator.GetLevelData().NumOfBoardHolders);
            _initialCardAreaController.Initialize(_cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Initial));
            _cardItemInfoPopupController.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders, _cardHolderModelCreator.GetCardHolderModelList(CardHolderType.Board));
            _fadePanelController.SetFadeImageStatus(false);
            _unmaskServiceAreaView.Initialize(_fadePanelController);
            _gamePopupCreator.Initialize(_cardHolderModelCreator);
            _cardInteractionManager.Initialize();
            _gameSaveService.DeleteSave();
        }
    }

    public interface IGameInitializer
    {
        void Initialize();
    }
}
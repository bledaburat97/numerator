using System.Collections.Generic;
using Scripts;
using Zenject;
/*
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
        [Inject] private ILevelSuccessManager _levelSuccessManager;
        [Inject] private IHintProvider _hintProvider;
        
        public void Initialize()
        {
            if(true){
            }

            else
            {
                _levelDataCreator.SetMultiplayerLevelData();
                _levelSaveDataManager.CreateDefaultLevelSaveData();
                _targetNumberCreator.CreateMultiplayerTargetNumber();
                _userReady.Initialize();
            }
            
            _gameUIController.Initialize(); //check which powerup button is pressable
            _resultAreaController.Initialize();
            _resultManager.Initialize(_levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount);
            _cardItemLocator.Initialize();
            _guessManager.Initialize();
            _boardAreaController.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders - _levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount);
            _cardItemInfoManager.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders - _levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount);
            _initialCardAreaController.Initialize(_cardItemInfoManager.GetCardItemInfoList());
            _cardItemInfoPopupController.Initialize();
            _fadePanelController.SetFadeImageStatus(false);
            _unmaskServiceAreaView.Initialize(_fadePanelController);
            _gamePopupCreator.Initialize();
            _cardInteractionManager.Initialize();
            _levelSuccessManager.Initialize();
            _gameSaveService.DeleteSave();
        }
    }

    public interface IGameInitializer
    {
        void Initialize();
    }
}
*/
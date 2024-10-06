using System;
using System.Collections.Generic;
using Scripts;
using UnityEngine;
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
        [Inject] private ICardHolderPositionManager _cardHolderPositionManager;

        public void Initialize()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _levelDataCreator.SetSinglePlayerLevelData(); //do not call while removing board holder.
                if (_levelTracker.IsFirstLevelTutorial())
                {
                    _levelSaveDataManager.CreateDefaultLevelSaveData();
                    _targetNumberCreator.SetSavedTargetCardList(new List<int>(){4,1});
                }
                else if (_levelTracker.IsCardInfoTutorial())
                {
                    _levelSaveDataManager.CreateDefaultLevelSaveData();
                    _targetNumberCreator.SetSavedTargetCardList(new List<int>(){4,6});
                }
                else if (_gameSaveService.GetSavedLevel() != null)
                {
                    _levelSaveDataManager.SetLevelSaveDataAsSaved(_gameSaveService.GetSavedLevel());
                    _targetNumberCreator.SetSavedTargetCardList(_gameSaveService.GetSavedLevel().TargetCards);
                }
                else
                {
                    _levelSaveDataManager.CreateDefaultLevelSaveData();
                    _targetNumberCreator.CreateTargetNumber();
                }
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
            _resultManager.Initialize();
            _cardItemLocator.Initialize();
            _guessManager.Initialize(_levelDataCreator.GetLevelData().MaxNumOfTries, _levelSaveDataManager.GetLevelSaveData().RemainingGuessCount, _levelDataCreator.GetLevelData().NumOfBoardHolders);
            _cardHolderPositionManager.Initialize();
            _boardAreaController.Initialize();
            _cardItemInfoManager.Initialize();
            _initialCardAreaController.Initialize();
            _cardItemInfoPopupController.Initialize();
            _fadePanelController.SetFadeImageStatus(false);
            _unmaskServiceAreaView.Initialize(_fadePanelController);
            _gamePopupCreator.Initialize();
            _cardInteractionManager.Initialize();
            _gameSaveService.DeleteSave();
        }

        public void RemoveLastWagon()
        {
            if (_gameSaveService.GetSavedLevel() != null || _levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                Debug.LogError("You shouldn't have clicked the bomb button");
                return;
            }
            _levelDataCreator.DecreaseNumOfBoardHolders();
            _levelSaveDataManager.CreateDefaultLevelSaveData();
            _targetNumberCreator.CreateTargetNumber();
            _gameUIController.Initialize(); //check which powerup button is pressable
            _resultManager.Initialize();
            _cardItemLocator.Initialize();
            _cardHolderPositionManager.Initialize();
            _initialCardAreaController.DeleteOneHolderIndicator();
            _boardAreaController.DeleteOneBoardHolder();
            _cardItemInfoManager.Initialize();
            _cardItemInfoPopupController.Initialize();
        }
    }

    public interface IGameInitializer
    {
        void Initialize();
        void RemoveLastWagon();
    }
}
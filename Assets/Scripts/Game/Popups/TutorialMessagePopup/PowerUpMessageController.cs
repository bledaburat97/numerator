﻿using System.Collections.Generic;
using Game;
using UnityEngine;
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
        private ICardItemInfoManager _cardItemInfoManager;
        private ILevelTracker _levelTracker;
        private IGameSaveService _gameSaveService;
        private IGameUIController _gameUIController;
        private IResultManager _resultManager;
        private ICardItemLocator _cardItemLocator;
        private ICardHolderPositionManager _cardHolderPositionManager;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private ILevelFinishController _levelFinishController;
        private ILevelSaveDataManager _levelSaveDataManager;
        private ILevelDataCreator _levelDataCreator;
        private int _removedBoardHolderCount;
        
        [Inject]
        public PowerUpMessageController(IInitialCardAreaController initialCardAreaController, IHapticController hapticController, 
            IBoardAreaController boardAreaController, ITargetNumberCreator targetNumberCreator,
            IGameUIController gameUIController, IGuessManager guessManager, BaseButtonControllerFactory baseButtonControllerFactory,
            IFadePanelController fadePanelController, IGameInitializer gameInitializer, IPowerUpMessagePopupView view, ICardItemInfoManager cardItemInfoManager,
            ILevelTracker levelTracker, IGameSaveService gameSaveService, IResultManager resultManager,
            ICardItemLocator cardItemLocator, ICardHolderPositionManager cardHolderPositionManager, ICardItemInfoPopupController cardItemInfoPopupController,
            ILevelFinishController levelFinishController, ILevelSaveDataManager levelSaveDataManager, ILevelDataCreator levelDataCreator)
        {
            _initialCardAreaController = initialCardAreaController;
            _hapticController = hapticController;
            _boardAreaController = boardAreaController;
            _targetNumberCreator = targetNumberCreator;
            _guessManager = guessManager;
            _fadePanelController = fadePanelController;
            _gameInitializer = gameInitializer;
            _cardItemInfoManager = cardItemInfoManager;
            _levelTracker = levelTracker;
            _gameSaveService = gameSaveService;
            _gameUIController = gameUIController;
            _resultManager = resultManager;
            _cardItemLocator = cardItemLocator;
            _cardHolderPositionManager = cardHolderPositionManager;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _levelFinishController = levelFinishController;
            _levelSaveDataManager = levelSaveDataManager;
            _levelDataCreator = levelDataCreator;
            _view = view;
            gameUIController.PowerUpClickedEvent += OnPowerUpClicked;
            _closeButton = baseButtonControllerFactory.Create(_view.GetCloseButton(), null);
            _continueButton = baseButtonControllerFactory.Create(_view.GetContinueButton(), null);
            CreatePowerUps();
        }

        public void Initialize()
        {
            _removedBoardHolderCount = _levelSaveDataManager.GetLevelSaveData().RemovedBoardHolderCount;
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
            _powerUps[powerUpType].Activate(_boardAreaController, _targetNumberCreator, _initialCardAreaController, _guessManager,
                _closeButton, _continueButton, _cardItemInfoManager, OnRemoveLastWagon);
        }
        
        private void OnRemoveLastWagon()
        {
            _removedBoardHolderCount++;
            if (_gameSaveService.GetSavedLevel() != null || _levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                Debug.LogError("You shouldn't have clicked the bomb button");
                return;
            }
            _targetNumberCreator.CreateTargetNumber(_removedBoardHolderCount);
            _gameUIController.Initialize(); //check which powerup button is pressable
            _resultManager.Initialize(_removedBoardHolderCount);
            _cardItemLocator.Initialize();
            _cardHolderPositionManager.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders - _removedBoardHolderCount);
            _initialCardAreaController.DeleteOneHolderIndicator();
            _boardAreaController.DeleteOneBoardHolder();
            _cardItemInfoManager.Initialize(_levelDataCreator.GetLevelData().NumOfBoardHolders - _removedBoardHolderCount);
            _cardItemInfoManager.RemoveLastCardHolderIndicator();
            _cardItemInfoPopupController.Initialize();
            _levelFinishController.Initialize();
        }

        public int GetRemovedBoardHolderCount()
        {
            return _removedBoardHolderCount;
        }
    }

    public interface IPowerUpMessageController
    {
        void Initialize();
        int GetRemovedBoardHolderCount();
    }
}
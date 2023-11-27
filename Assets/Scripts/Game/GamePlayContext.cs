﻿using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GamePlayContext : MonoBehaviour
    {
        [SerializeField] private BoardAreaView boardAreaView;
        [SerializeField] private CardItemInfoPopupView cardItemInfoPopupView;
        [SerializeField] private ResultAreaView resultAreaView;
        [SerializeField] private LevelTracker levelTracker;
        [SerializeField] private InitialCardAreaView initialCardAreaView;
        [SerializeField] private GamePopupCreator gamePopupCreator;
        [SerializeField] private FadePanelView fadePanelView;
        [SerializeField] private TMP_Text levelIdText;
        [SerializeField] private SettingsButtonView settingsButtonView;
        [SerializeField] private CheckButtonView checkButtonView;
        [SerializeField] private ResetButtonView resetButtonView;
        [SerializeField] private StarProgressBarView starProgressBarView;
        [SerializeField] private Canvas canvas;
        
        private IBoardAreaController _boardAreaController;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private IInitialCardAreaController _initialCardAreaController;
        private ICardItemLocator _cardItemLocator;
        private ICardItemInfoManager _cardItemInfoManager;
        private IResultAreaController _resultAreaController;
        private ICardHolderModelCreator _cardHolderModelCreator;
        private IResultManager _resultManager;
        private IFadePanelController _fadePanelController;
        private ISettingsButtonController _settingsButtonController;
        private ICheckButtonController _checkButtonController;
        private IResetButtonController _resetButtonController;
        private IStarProgressBarController _starProgressBarController;
        private ILevelManager _levelManager;
        private IGameSaveService _gameSaveService;
        void Start()
        {
            _cardItemLocator = new CardItemLocator(canvas);
            _gameSaveService = new GameSaveService();
            _gameSaveService.Initialize(levelTracker);
            levelTracker.Initialize(_gameSaveService);
            _cardHolderModelCreator = new CardHolderModelCreator();
            _cardHolderModelCreator.Initialize();
            _resultManager = new ResultManager();
            _resultManager.Initialize(levelTracker);
            SetLevelId();
            CreateSettingsButton();
            CreateCheckButton();
            CreateResetButton();
            CreateLevelManager();
            CreateStarProgressBar();
            CreateBoardArea();
            CreateResultArea();
            CreateCardItemInfoPopup();
            CreateInitialCardArea();
            CreateGamePopupCreator();
            _gameSaveService.Set(_resultManager, _initialCardAreaController);
            foreach (List<int> triedCards in levelTracker.GetLevelInfo().levelSaveData.TriedCardsList)
            {
                _resultManager.CheckFinalCards(triedCards);
            }
        }
        
        private void OnApplicationQuit()
        {
            _gameSaveService.Save();
        }

        private void SetLevelId()
        {
            levelIdText.SetText("Level " + levelTracker.GetLevelId());
        }

        private void CreateSettingsButton()
        {
            _settingsButtonController = new SettingsButtonController();
            _settingsButtonController.Initialize(settingsButtonView);
        }
        
        private void CreateCheckButton()
        {
            _checkButtonController = new CheckButtonController();
            _checkButtonController.Initialize(checkButtonView);
        }
        
        private void CreateResetButton()
        {
            _resetButtonController = new ResetButtonController();
            _resetButtonController.Initialize(resetButtonView);
        }

        private void CreateLevelManager()
        {
            _levelManager = new LevelManager();
            _levelManager.Initialize(levelTracker, _resultManager, _gameSaveService);
        }

        private void CreateStarProgressBar()
        {
            _starProgressBarController = new StarProgressBarController();
            _starProgressBarController.Initialize(starProgressBarView, levelTracker, _levelManager);
        }
        
        private void CreateBoardArea()
        {
            _boardAreaController = new BoardAreaController();
            _boardAreaController.Initialize(boardAreaView, _cardItemLocator, _resultManager, levelTracker, _cardHolderModelCreator, _checkButtonController);
        }
        
        private void CreateResultArea()
        {
            _resultAreaController = new ResultAreaController();
            _resultAreaController.Initialize(resultAreaView, _resultManager);
        }
        
        private void CreateCardItemInfoPopup()
        {
            _cardItemInfoManager = new CardItemInfoManager();
            _cardItemInfoManager.Initialize(levelTracker);

            _cardItemInfoPopupController = new CardItemInfoPopupController();
            _cardItemInfoPopupController.Initialize(cardItemInfoPopupView, _cardItemInfoManager, levelTracker, _cardHolderModelCreator);
        }
        
        private void CreateInitialCardArea()
        {
            _initialCardAreaController = new InitialCardAreaController();
            _initialCardAreaController.Initialize(initialCardAreaView, _cardItemLocator, SetCardItemInfoPopupStatus, _cardItemInfoManager, levelTracker, _cardHolderModelCreator, _resetButtonController);
        }

        private void SetCardItemInfoPopupStatus(bool status, int cardIndex)
        {
            _cardItemInfoPopupController.SetCardItemInfoPopupStatus(status, cardIndex);
        }

        private void CreateGamePopupCreator()
        {
            _fadePanelController = new FadePanelController();
            _fadePanelController.Initialize(fadePanelView);
            gamePopupCreator.Initialize(_levelManager, _fadePanelController, _settingsButtonController, _gameSaveService);
        }

    }
}
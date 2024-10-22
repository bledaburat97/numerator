using System;
using Scripts;
using Zenject;

namespace Game
{
    public class LevelEndManager : ILevelEndManager
    {
        private bool _isGameOver;
        private IGameSaveService _gameSaveService;
        private ILevelStartManager _levelStartManager;
        private ICardItemInfoPopupController _cardItemInfoPopupController;
        private ILevelSuccessManager _levelSuccessManager;
        private ILevelFailManager _levelFailManager;

        [Inject]
        public LevelEndManager(IResultManager resultManager, IGameSaveService gameSaveService,
            ILevelStartManager levelStartManager, IGuessManager guessManager,
            ICardItemInfoPopupController cardItemInfoPopupController, 
            ILevelSuccessManager levelSuccessManager, ILevelFailManager levelFailManager)
        {
            _gameSaveService = gameSaveService;
            _levelStartManager = levelStartManager;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _levelStartManager.LevelStartedEvent += OnLevelStarted;
            _levelSuccessManager = levelSuccessManager;
            _levelFailManager = levelFailManager;
            resultManager.LevelSuccessEvent += OnLevelSuccess;
            guessManager.LevelFailEvent += OnLevelFail;
        }
        
        private void OnLevelStarted(object sender, EventArgs args)
        {
            _isGameOver = false;
        }
        
        private void OnLevelSuccess(object sender, EventArgs args)
        {
            _isGameOver = true;
            _gameSaveService.DeleteSave();
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            _levelSuccessManager.LevelSuccess();
        }
        
        private void OnLevelFail(object sender, EventArgs args)
        {
            _isGameOver = true;
            _gameSaveService.DeleteSave();
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            _levelFailManager.LevelFail();
        }
        
        public bool IsGameOver()
        {
            return _isGameOver;
        }
    }

    public interface ILevelEndManager
    {
        bool IsGameOver();
    }
}
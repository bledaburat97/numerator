using System;
using Scripts;
using Zenject;

namespace Game
{
    public class LevelStartManager : ILevelStartManager
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ILevelDataCreator _levelDataCreator;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private ILevelSaveDataManager _levelSaveDataManager;
        [Inject] private IBoardAreaController _boardAreaController;
        [Inject] private IResultAreaController _resultAreaController;
        [Inject] private ILifeBarController _lifeBarController;
        [Inject] private IInitialCardAreaController _initialCardAreaController;
        [Inject] private IGamePopupCreator _gamePopupCreator;
        [Inject] private IGameUIController _gameUIController;
        [Inject] private ILevelStartAnimationManager _levelStartAnimationManager;
        [Inject] private ICardItemLocator _cardItemLocator;
        [Inject] private IGuessManager _guessManager;
        [Inject] private ICardItemInfoManager _cardItemInfoManager;
        [Inject] private ICardItemInfoPopupController _cardItemInfoPopupController;
        [Inject] private ICardInteractionManager _cardInteractionManager;
        [Inject] private IResultManager _resultManager;

        public event EventHandler LevelStartedEvent;
        public void StartLevel()
        {
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _levelDataCreator.SetSinglePlayerLevelData();
                _gamePopupCreator.Initialize(); //TODO edit this class and call that with the level start event.

                if (_gameSaveService.GetSavedLevel() != null)
                {
                    _levelSaveDataManager.SetLevelSaveDataAsSaved(_gameSaveService.GetSavedLevel());
                    _boardAreaController.CreateBoard(false);
                    _resultAreaController.Initialize(false);
                    _lifeBarController.SetFade(false);
                    _initialCardAreaController.Initialize(false);
                    _gameUIController.Initialize(false);
                }
                else
                {
                    _levelSaveDataManager.CreateDefaultLevelSaveData();
                    _boardAreaController.CreateBoard(true);
                    _resultAreaController.Initialize(true);
                    _lifeBarController.SetFade(true);
                    _initialCardAreaController.Initialize(true);
                    _gameUIController.Initialize(true);
                    _levelStartAnimationManager.StartLevelStartAnimation();
                }
                
                _cardItemLocator.Initialize();
                _guessManager.Initialize();
                _cardItemInfoManager.Initialize();
                _cardItemInfoPopupController.Initialize();
                _cardInteractionManager.Initialize();
                _resultManager.TryAddTriedCards();
                LevelStartedEvent?.Invoke(this, EventArgs.Empty);
            }

            else
            {
                _lifeBarController.DisableStarProgressBar();
                _gameUIController.InitializeForMultiplayer();
            }
        }
    }

    public interface ILevelStartManager
    {
        void StartLevel();
        event EventHandler LevelStartedEvent;
    }
}
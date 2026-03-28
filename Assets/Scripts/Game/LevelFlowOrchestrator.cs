using System;
using DG.Tweening;
using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game
{
    public class LevelFlowOrchestrator : ILevelFlowOrchestrator
    {
        private readonly ILevelTracker _levelTracker;
        private readonly ILevelDataCreator _levelDataCreator;
        private readonly IGameSaveService _gameSaveService;
        private readonly ILevelSaveDataManager _levelSaveDataManager;
        private readonly IBoardAreaController _boardAreaController;
        private readonly IResultAreaController _resultAreaController;
        private readonly ILifeBarController _lifeBarController;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IGamePopupCreator _gamePopupCreator;
        private readonly IGameUIController _gameUIController;
        private readonly ILevelStartAnimationManager _levelStartAnimationManager;
        private readonly ICardItemLocator _cardItemLocator;
        private readonly IGuessManager _guessManager;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly ICardItemInfoPopupController _cardItemInfoPopupController;
        private readonly ICardInteractionManager _cardInteractionManager;
        private readonly IResultManager _resultManager;
        private readonly IRoundStateManager _roundStateManager;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private readonly ILevelSuccessAnimationManager _levelSuccessAnimationManager;
        private readonly ILevelFailAnimationManager _levelFailAnimationManager;
        private readonly IFadePanelController _fadePanelController;

        private LevelFlowState _state;
        private bool _isGameOver;

        [Inject]
        public LevelFlowOrchestrator(
            ILevelTracker levelTracker,
            ILevelDataCreator levelDataCreator,
            IGameSaveService gameSaveService,
            ILevelSaveDataManager levelSaveDataManager,
            IBoardAreaController boardAreaController,
            IResultAreaController resultAreaController,
            ILifeBarController lifeBarController,
            IInitialCardAreaController initialCardAreaController,
            IGamePopupCreator gamePopupCreator,
            IGameUIController gameUIController,
            ILevelStartAnimationManager levelStartAnimationManager,
            ICardItemLocator cardItemLocator,
            IGuessManager guessManager,
            ICardItemInfoManager cardItemInfoManager,
            ICardItemInfoPopupController cardItemInfoPopupController,
            ICardInteractionManager cardInteractionManager,
            IResultManager resultManager,
            IRoundStateManager roundStateManager,
            ILevelEndPopupController levelEndPopupController,
            ILevelSuccessAnimationManager levelSuccessAnimationManager,
            ILevelFailAnimationManager levelFailAnimationManager,
            IFadePanelController fadePanelController)
        {
            _levelTracker = levelTracker;
            _levelDataCreator = levelDataCreator;
            _gameSaveService = gameSaveService;
            _levelSaveDataManager = levelSaveDataManager;
            _boardAreaController = boardAreaController;
            _resultAreaController = resultAreaController;
            _lifeBarController = lifeBarController;
            _initialCardAreaController = initialCardAreaController;
            _gamePopupCreator = gamePopupCreator;
            _gameUIController = gameUIController;
            _levelStartAnimationManager = levelStartAnimationManager;
            _cardItemLocator = cardItemLocator;
            _guessManager = guessManager;
            _cardItemInfoManager = cardItemInfoManager;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _cardInteractionManager = cardInteractionManager;
            _resultManager = resultManager;
            _roundStateManager = roundStateManager;
            _levelEndPopupController = levelEndPopupController;
            _levelSuccessAnimationManager = levelSuccessAnimationManager;
            _levelFailAnimationManager = levelFailAnimationManager;
            _fadePanelController = fadePanelController;
            _state = LevelFlowState.Idle;

            _resultManager.LevelSuccessEvent += OnLevelSuccess;
            _guessManager.LevelFailEvent += OnLevelFail;
        }

        public void Begin(LevelEntryMode mode)
        {
            _gamePopupCreator.Initialize();
            _levelEndPopupController.SetPopupStatus(false);
            _levelEndPopupController.SetAllStatusFalse();
            _isGameOver = false;
            _state = LevelFlowState.Building;

            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                BeginMultiplayer();
                return;
            }

            BeginSinglePlayer(mode);
        }

        public void Retry()
        {
            if (_state == LevelFlowState.Transitioning) return;

            _state = LevelFlowState.Transitioning;
            float animationDuration = 1f;
            DOTween.Sequence()
                .AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .Append(_fadePanelController.AnimateFade(0f, animationDuration))
                .Join(_levelFailAnimationManager.FadeOutResultArea(animationDuration))
                .Join(_levelFailAnimationManager.SendBoardHolders(animationDuration))
                .AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .AppendCallback(() => _fadePanelController.SetFadeImageStatus(false))
                .AppendCallback(() => Begin(LevelEntryMode.Retry));
        }

        public void GoToNextLevel()
        {
            if (_state == LevelFlowState.Transitioning) return;

            _state = LevelFlowState.Transitioning;
            DOTween.Sequence()
                .AppendCallback(() => _levelEndPopupController.SetPopupStatus(false))
                .Append(_fadePanelController.AnimateFade(0f, 0.2f))
                .AppendCallback(() => _fadePanelController.SetFadeImageStatus(false))
                .AppendCallback(() => Begin(LevelEntryMode.NextLevel));
        }

        public bool IsGameOver()
        {
            return _isGameOver;
        }

        public bool IsTransitioning()
        {
            return _state != LevelFlowState.Idle && _state != LevelFlowState.Playing;
        }

        public LevelFlowState GetState()
        {
            return _state;
        }

        private void BeginSinglePlayer(LevelEntryMode mode)
        {
            _levelDataCreator.SetSinglePlayerLevelData();
            bool isResumeFlow = TryPrepareSinglePlayerSaveData(mode);

            _roundStateManager.Initialize();
            _boardAreaController.CreateBoard(!isResumeFlow);
            _resultAreaController.Initialize(!isResumeFlow);
            _lifeBarController.SetFade(!isResumeFlow);
            _initialCardAreaController.Initialize(!isResumeFlow);
            _gameUIController.Initialize(!isResumeFlow);

            InitializeGameplaySystems();

            if (isResumeFlow)
            {
                _state = LevelFlowState.Playing;
                return;
            }

            _state = LevelFlowState.IntroPlaying;
            Sequence introSequence = _levelStartAnimationManager.StartLevelStartAnimation();
            if (introSequence != null)
            {
                introSequence.OnComplete(() =>
                {
                    if (!_isGameOver)
                    {
                        _state = LevelFlowState.Playing;
                    }
                });
            }
            else
            {
                _state = LevelFlowState.Playing;
            }
        }

        private void BeginMultiplayer()
        {
            _lifeBarController.DisableStarProgressBar();
            _gameUIController.InitializeForMultiplayer();
            _state = LevelFlowState.Playing;
        }

        private bool TryPrepareSinglePlayerSaveData(LevelEntryMode mode)
        {
            if (mode == LevelEntryMode.Resume)
            {
                LevelSaveData savedLevel = _gameSaveService.GetSavedLevel();
                if (savedLevel != null)
                {
                    _levelSaveDataManager.SetLevelSaveDataAsSaved(savedLevel);
                    return true;
                }
            }

            _levelSaveDataManager.CreateDefaultLevelSaveData();
            return false;
        }

        private void InitializeGameplaySystems()
        {
            _cardItemLocator.Initialize();
            _guessManager.Initialize();
            _cardItemInfoManager.Initialize();
            _cardItemInfoPopupController.Initialize();
            _cardInteractionManager.Initialize();
            _resultManager.TryAddTriedCards();
        }

        private void OnLevelSuccess(object sender, EventArgs args)
        {
            if (_levelTracker.GetGameOption() != GameOption.SinglePlayer || _isGameOver) return;

            _isGameOver = true;
            _state = LevelFlowState.EndingSuccess;
            _gameSaveService.DeleteSave();
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            StartSuccessFlow();
        }

        private void OnLevelFail(object sender, EventArgs args)
        {
            if (_levelTracker.GetGameOption() != GameOption.SinglePlayer || _isGameOver) return;

            _isGameOver = true;
            _state = LevelFlowState.EndingFail;
            _gameSaveService.DeleteSave();
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            StartFailFlow();
        }

        private void StartSuccessFlow()
        {
            int rewardStarCount = _levelTracker.GetGiftStarCount();
            RewardType rewardType = _levelTracker.GetCurrentRewardType();
            _roundStateManager.GetActiveStarCounts(out int totalStarCount, out int newRewardStarCount);
            _levelTracker.IncrementLevelId(totalStarCount, newRewardStarCount);

            _levelEndPopupController.SetAllStatusFalse();
            _levelEndPopupController.SetPopupStatus(false);
            _levelEndPopupController.InitButton(
                LevelFinishButtonType.Game,
                "Level " + (_levelTracker.GetLevelId() + 1),
                GoToNextLevel);
            _levelEndPopupController.InitButton(LevelFinishButtonType.Menu, "Menu", OnMenuButtonClicked);
            _levelEndPopupController.InitText("Well Done");
            _levelEndPopupController.CreateRewardCircle(rewardStarCount);
            _levelEndPopupController.InitStarsAndParticles(totalStarCount, newRewardStarCount);
            _levelEndPopupController.InitRewardItem(rewardType);
            _levelSuccessAnimationManager.SuccessLevelAnimation(
                totalStarCount,
                newRewardStarCount,
                rewardStarCount);
        }

        private void StartFailFlow()
        {
            _levelEndPopupController.SetAllStatusFalse();
            _levelEndPopupController.SetPopupStatus(true);
            _levelEndPopupController.InitButton(LevelFinishButtonType.Game, "Retry", Retry);
            _levelEndPopupController.InitButton(LevelFinishButtonType.Menu, "Menu", OnMenuButtonClicked);
            _levelFailAnimationManager.FailLevelAnimation();
        }

        private void OnMenuButtonClicked()
        {
            SceneManager.LoadScene("Menu");
        }
    }

    public interface ILevelFlowOrchestrator
    {
        void Begin(LevelEntryMode mode);
        void Retry();
        void GoToNextLevel();
        bool IsGameOver();
        bool IsTransitioning();
        LevelFlowState GetState();
    }

    public enum LevelEntryMode
    {
        New,
        Resume,
        Retry,
        NextLevel
    }

    public enum LevelFlowState
    {
        Idle,
        Building,
        IntroPlaying,
        Playing,
        EndingSuccess,
        EndingFail,
        Transitioning
    }
}

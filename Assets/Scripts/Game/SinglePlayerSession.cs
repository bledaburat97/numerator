using System;
using DG.Tweening;
using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game
{
    public sealed class SinglePlayerSession : ILevelSession
    {
        private readonly ILevelTracker _levelTracker;
        private readonly ILevelDataCreator _levelDataCreator;
        private readonly IGameSaveService _gameSaveService;
        private readonly ILevelSaveDataManager _levelSaveDataManager;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IResultAreaController _resultAreaController;
        private readonly IGamePopupCreator _gamePopupCreator;
        private readonly IGameUIController _gameUIController;
        private readonly IVerticalCrystalProgressController _verticalCrystalProgressController;
        private readonly IGuessManager _guessManager;
        private readonly ICardItemInfoPopupController _cardItemInfoPopupController;
        private readonly IResultManager _resultManager;
        private readonly IRoundStateManager _roundStateManager;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private readonly ILevelSuccessAnimationManager _levelSuccessAnimationManager;
        private readonly ILevelFailAnimationManager _levelFailAnimationManager;
        private readonly LevelMechanicsController _mechanics;
        private readonly RoundBuilder _builder;
        private readonly LevelPresentation _presentation;
        private LevelFlowState _state = LevelFlowState.Idle;
        private bool _isGameOver, _disposed;
        [Inject]
        public SinglePlayerSession(
            ILevelTracker levelTracker,
            ILevelDataCreator levelDataCreator,
            IGameSaveService gameSaveService,
            ILevelSaveDataManager levelSaveDataManager,
            IBoardHolderCountManager boardHolderCountManager,
            ITargetNumberCreator targetNumberCreator,
            IResultAreaController resultAreaController,
            IGamePopupCreator gamePopupCreator,
            IGameUIController gameUIController,
            IVerticalCrystalProgressController verticalCrystalProgressController,
            IGuessManager guessManager,
            ICardItemInfoPopupController cardItemInfoPopupController,
            IResultManager resultManager,
            IRoundStateManager roundStateManager,
            ILevelEndPopupController levelEndPopupController,
            ILevelSuccessAnimationManager levelSuccessAnimationManager,
            ILevelFailAnimationManager levelFailAnimationManager,
            LevelMechanicsController mechanics,
            RoundBuilder builder,
            LevelPresentation presentation)
        {
            _levelTracker = levelTracker;
            _levelDataCreator = levelDataCreator;
            _gameSaveService = gameSaveService;
            _levelSaveDataManager = levelSaveDataManager;
            _boardHolderCountManager = boardHolderCountManager;
            _targetNumberCreator = targetNumberCreator;
            _resultAreaController = resultAreaController;
            _gamePopupCreator = gamePopupCreator;
            _gameUIController = gameUIController;
            _verticalCrystalProgressController = verticalCrystalProgressController;
            _guessManager = guessManager;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _resultManager = resultManager;
            _roundStateManager = roundStateManager;
            _levelEndPopupController = levelEndPopupController;
            _levelSuccessAnimationManager = levelSuccessAnimationManager;
            _levelFailAnimationManager = levelFailAnimationManager;
            _mechanics = mechanics;
            _builder = builder;
            _presentation = presentation;
            _resultManager.LevelSuccessEvent += OnLevelSuccess;
            _guessManager.LevelFailEvent += OnLevelFail;
        }

        public LevelFlowState State => _state;
        public bool IsGameOver => _isGameOver;
        public void Begin(LevelEntryMode mode)
        {
            if (_disposed) return;
            _presentation.PrepareRound(); _isGameOver = false; _state = LevelFlowState.Building;
            BeginSinglePlayer(mode);
        }
        public void Retry()
        {
            if (_disposed || _state == LevelFlowState.Transitioning) return;
            _state = LevelFlowState.Transitioning;
            _presentation.Retry(() => Begin(LevelEntryMode.Retry));
        }
        public void GoToNextLevel()
        {
            if (_disposed || _state == LevelFlowState.Transitioning) return;
            _state = LevelFlowState.Transitioning;
            _presentation.Next(() => Begin(LevelEntryMode.NextLevel));
        }
        private void BeginSinglePlayer(LevelEntryMode mode)
        {
            _levelDataCreator.SetSinglePlayerLevelData();
            bool isResumeFlow = TryPrepareSinglePlayerSaveData(mode);

            _builder.BuildSingle(isResumeFlow);

            _levelTracker.GrantIntroducedPowerUps();
            LevelPlaytestSchedule.PrepareTrialStock(_levelTracker, _levelSaveDataManager, _levelDataCreator.GetLevelData());
            _builder.InitializeSingleSystems(!isResumeFlow);
            GameTelemetry.BeginSingle(_levelTracker.GetLevelId() + 1, _boardHolderCountManager.GetBoardHolderCount(),
                _levelDataCreator.GetLevelData().NumOfCards, isResumeFlow ? LevelEntryMode.Resume : mode,
                _roundStateManager.GetTriedCardsList().Count, _levelDataCreator.GetLevelData().Mechanic);

            _mechanics?.Begin(_levelDataCreator.GetLevelData().Mechanic);
            if (isResumeFlow)
            {
                _state = LevelFlowState.Playing;
                _gamePopupCreator.StartTutorials();
                return;
            }

            _state = LevelFlowState.IntroPlaying;
            _presentation.PlayIntro(() =>
            {
                if (_isGameOver || _disposed) return;
                _state = LevelFlowState.Playing; _gamePopupCreator.StartTutorials();
            });
        }
        private bool TryPrepareSinglePlayerSaveData(LevelEntryMode mode)
        {
            if (mode == LevelEntryMode.Resume)
            {
                LevelSaveData savedLevel = _gameSaveService.GetSavedLevel();
                if (savedLevel?.Definition?.Mechanic == LevelMechanic.Growing) GrowingBoardState.UpgradeLegacy(savedLevel);
                if (savedLevel != null && (!_levelTracker.IsFirstLevelTutorial()
                    || FirstLevelTutorialController.CanResume(savedLevel, _levelDataCreator.GetLevelData().MaxNumOfTries)))
                {
                    if ((savedLevel.LevelId >= 0 && savedLevel.LevelId != _levelTracker.GetLevelId())
                        || !LevelPlaytestSchedule.CanResume(_levelTracker.GetLevelId(), savedLevel.Definition)
                        || (savedLevel.Definition?.Mechanic == LevelMechanic.Growing && !GrowingBoardState.IsValid(savedLevel))
                        || (savedLevel.Definition?.Mechanic == LevelMechanic.Dynamite && !DynamiteRoundState.IsValid(savedLevel)))
                    { _gameSaveService.DeleteSave(); _levelSaveDataManager.CreateDefaultLevelSaveData(); return false; }
                    if (savedLevel.Definition != null) (_levelDataCreator as LevelDataCreator)?.RestoreDefinition(savedLevel.Definition);
                    savedLevel.RevealedGuessNumbers ??= new System.Collections.Generic.List<int>();
                    _levelSaveDataManager.SetLevelSaveDataAsSaved(savedLevel);
                    return true;
                }
                if (savedLevel != null) _gameSaveService.DeleteSave();
            }

            _levelSaveDataManager.CreateDefaultLevelSaveData();
            return false;
        }
        private void OnLevelSuccess(object sender, EventArgs args)
        {
            if (_levelTracker.GetGameOption() != GameOption.SinglePlayer || _isGameOver) return;

            _isGameOver = true;
            _state = LevelFlowState.EndingSuccess;
            _gamePopupCreator.EndTutorials(true);
            _gameUIController.SetAllButtonsUnclickable();
            _resultAreaController.RevealAnswer(_targetNumberCreator.GetHistoryTargetCards());
            _gameSaveService.DeleteSave();
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            StartSuccessFlow();
        }
        private void OnLevelFail(object sender, EventArgs args)
        {
            if (_levelTracker.GetGameOption() != GameOption.SinglePlayer || _isGameOver) return;

            _isGameOver = true;
            _state = LevelFlowState.EndingFail;
            GameTelemetry.Finish("fail", _roundStateManager.GetTriedCardsList().Count);
            _gamePopupCreator.EndTutorials(false);
            _gameUIController.SetAllButtonsUnclickable();
            _resultAreaController.RevealAnswer(_targetNumberCreator.GetHistoryTargetCards());
            _gameSaveService.DeleteSave();
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            StartFailFlow();
        }
        private void StartSuccessFlow()
        {
            int previousCoinCount = _levelTracker.GetCoinCount();
            int previousCrystalProgressCount = _levelTracker.GetCrystalProgressCount();
            RewardType rewardTypeBeforeCollection = _levelTracker.GetCurrentRewardType();
            _roundStateManager.GetActiveRewardCounts(out int earnedCoinCount, out int earnedCrystalCount);
            GameTelemetry.Finish("win", _roundStateManager.GetTriedCardsList().Count, earnedCoinCount, earnedCrystalCount);
            _levelTracker.IncrementLevelIdWithRewards(earnedCoinCount, earnedCrystalCount);

            _levelEndPopupController.SetAllStatusFalse();
            if (earnedCrystalCount > 0)
                _verticalCrystalProgressController.PrepareForCollection(previousCrystalProgressCount, rewardTypeBeforeCollection);
            _levelEndPopupController.SetOutcome(true, earnedCrystalCount);
            _levelEndPopupController.SetPopupStatus(false);
            _levelEndPopupController.InitButton(
                LevelFinishButtonType.Game,
                "Bölüm " + (_levelTracker.GetLevelId() + 1),
                GoToNextLevel);
            _levelEndPopupController.InitButton(LevelFinishButtonType.Menu, "Menü", OnMenuButtonClicked);
            _levelEndPopupController.InitText("Kazandın!");
            _levelSuccessAnimationManager.SuccessLevelAnimation(
                earnedCoinCount,
                earnedCrystalCount,
                previousCoinCount,
                previousCrystalProgressCount,
                rewardTypeBeforeCollection)
                .OnComplete(() =>
                {
                    if (_state == LevelFlowState.EndingSuccess) _gamePopupCreator.ShowRewardTutorials(earnedCoinCount, earnedCrystalCount);
                });
        }
        private void StartFailFlow()
        {
            _levelEndPopupController.SetAllStatusFalse();
            _levelEndPopupController.SetOutcome(false);
            _levelEndPopupController.SetPopupStatus(true);
            _levelEndPopupController.InitText("Tekrar dene");
            _levelEndPopupController.InitButton(LevelFinishButtonType.Game, "Tekrar oyna", Retry);
            _levelEndPopupController.InitButton(LevelFinishButtonType.Menu, "Menü", OnMenuButtonClicked);
            _levelFailAnimationManager.FailLevelAnimation();
        }
        private void OnMenuButtonClicked()
        {
            SceneManager.LoadScene("Menu");
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            _resultManager.LevelSuccessEvent -= OnLevelSuccess; _guessManager.LevelFailEvent -= OnLevelFail;
            _presentation.Dispose(); GameTelemetry.Abandon("scene_exit");
        }
    }
}

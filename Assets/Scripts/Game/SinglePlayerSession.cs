using System;
using Scripts;
using Zenject;

namespace Game
{
    public sealed class SinglePlayerSession : ILevelSession, ISinglePlayerProgression
    {
        private readonly ILevelTracker _levelTracker;
        private readonly ILevelDataCreator _levelDataCreator;
        private readonly IGameSaveService _gameSaveService;
        private readonly ILevelSaveDataManager _levelSaveDataManager;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly IGamePopupCreator _gamePopupCreator;
        private readonly IGuessManager _guessManager;
        private readonly IResultManager _resultManager;
        private readonly IRoundStateManager _roundStateManager;
        private readonly SinglePlayerRoundCompletion _completion;
        private readonly SinglePlayerEndPresenter _endPresenter;
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
            IGamePopupCreator gamePopupCreator,
            IGuessManager guessManager,
            IResultManager resultManager,
            IRoundStateManager roundStateManager,
            SinglePlayerRoundCompletion completion,
            SinglePlayerEndPresenter endPresenter,
            LevelMechanicsController mechanics,
            RoundBuilder builder,
            LevelPresentation presentation)
        {
            _levelTracker = levelTracker;
            _levelDataCreator = levelDataCreator;
            _gameSaveService = gameSaveService;
            _levelSaveDataManager = levelSaveDataManager;
            _boardHolderCountManager = boardHolderCountManager;
            _gamePopupCreator = gamePopupCreator;
            _guessManager = guessManager;
            _resultManager = resultManager;
            _roundStateManager = roundStateManager;
            _completion = completion;
            _endPresenter = endPresenter;
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
            _endPresenter.PrepareRound();
            _completion.BeginRound();
            _presentation.PrepareRound(); _isGameOver = false; _state = LevelFlowState.Building;
            BeginSinglePlayer(mode);
        }
        public void Retry()
        {
            if (_disposed || _state == LevelFlowState.Transitioning) return;
            _state = LevelFlowState.Transitioning;
            _endPresenter.PrepareRound();
            _presentation.Retry(() => Begin(LevelEntryMode.Retry));
        }
        public void GoToNextLevel()
        {
            if (_disposed || _state == LevelFlowState.Transitioning) return;
            _state = LevelFlowState.Transitioning;
            _endPresenter.PrepareRound();
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
            CompleteRound(true);
        }
        private void OnLevelFail(object sender, EventArgs args)
        {
            if (_levelTracker.GetGameOption() != GameOption.SinglePlayer || _isGameOver) return;

            _isGameOver = true;
            _state = LevelFlowState.EndingFail;
            CompleteRound(false);
        }
        private void CompleteRound(bool won)
        {
            _gamePopupCreator.EndTutorials(won);
            var result = _completion.Complete(won);
            GameTelemetry.Finish(result.Won ? "win" : "fail", result.GuessCount, result.EarnedCoins, result.EarnedCrystals);
            _endPresenter.Present(result, this);
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            _resultManager.LevelSuccessEvent -= OnLevelSuccess; _guessManager.LevelFailEvent -= OnLevelFail;
            _endPresenter.Dispose(); _presentation.Dispose(); GameTelemetry.Abandon("scene_exit");
        }
    }
}

using System;
using Scripts;
using Zenject;

namespace Game
{
    public sealed class SinglePlayerSession : ILevelSession, ISinglePlayerProgression,ISinglePlayerOutcomeSink
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
        private bool _isGameOver, _disposed,_endPresented;
        private RewardSettlement<SinglePlayerRoundResult> _settlement;
        [InjectOptional] private LazyInject<IGameSaveSnapshotProvider> _snapshots;
        public RewardSaveState RewardState=>_settlement?.State??RewardSaveState.Pending;
        public void Tick(){if(!_disposed)_settlement?.Tick();}
        public void RetryReward(){if(!_disposed)_settlement?.Retry();}
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
        }

        public LevelFlowState State => _state;
        public bool IsGameOver => _isGameOver;
        public void Begin(LevelEntryMode mode)
        {
            if (_disposed) return;
            _settlement=null;_endPresented=false;
            _endPresenter.PrepareRound();
            _presentation.PrepareRound(); _isGameOver = false; _state = LevelFlowState.Building;
            BeginSinglePlayer(mode);
        }
        public void Retry()
        {
            if (_disposed || _state == LevelFlowState.Transitioning || _settlement!=null && _settlement.State!=RewardSaveState.Completed) return;
            _state = LevelFlowState.Transitioning;
            _endPresenter.PrepareRound();
            _presentation.Retry(() => Begin(LevelEntryMode.Retry));
        }
        public void GoToNextLevel()
        {
            if (_disposed || _state == LevelFlowState.Transitioning || _settlement!=null && _settlement.State!=RewardSaveState.Completed) return;
            _state = LevelFlowState.Transitioning;
            _endPresenter.PrepareRound();
            _presentation.Next(() => Begin(LevelEntryMode.NextLevel));
        }
        private void BeginSinglePlayer(LevelEntryMode mode)
        {
            _levelDataCreator.SetSinglePlayerLevelData();
            bool isResumeFlow = TryPrepareSinglePlayerSaveData(mode);

            _builder.BuildSingle(isResumeFlow);
            _completion.BeginRound();

            _levelTracker.GrantIntroducedPowerUps();
            LevelPlaytestSchedule.PrepareTrialStock(_levelTracker, _levelSaveDataManager, _levelDataCreator.GetLevelData());
            _builder.InitializeSingleSystems(!isResumeFlow);
            GameTelemetry.BeginSingle(_levelTracker.GetLevelId() + 1, _boardHolderCountManager.GetBoardHolderCount(),
                _levelDataCreator.GetLevelData().NumOfCards, isResumeFlow ? LevelEntryMode.Resume : mode,
                _roundStateManager.GetTriedCardsList().Count, _levelDataCreator.GetLevelData().Mechanic);

            _mechanics?.Begin(_levelDataCreator.GetLevelData().Mechanic);
            if(_levelSaveDataManager.GetLevelSaveData().HasPendingWin())
            {_state=LevelFlowState.Playing;OnLevelSuccess(this,EventArgs.Empty);return;}
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
                if (savedLevel != null && (savedLevel.HasPendingWin()
                    || !TutorialLessonController.IsBasics(savedLevel.Definition,_levelTracker.IsFirstLevelTutorial())
                    || FirstLevelTutorialController.CanResume(savedLevel,savedLevel.Definition?.MaxNumOfTries??_levelDataCreator.GetLevelData().MaxNumOfTries)))
                {
                    if ((savedLevel.LevelId >= 0 && savedLevel.LevelId != _levelTracker.GetLevelId())
                        || (!savedLevel.HasPendingWin() && !LevelPlaytestSchedule.CanResume(_levelTracker.GetLevelId(), savedLevel.Definition))
                        || (savedLevel.Definition?.Mechanic == LevelMechanic.Growing && !savedLevel.HasPendingWin() && !GrowingBoardState.IsValid(savedLevel))
                        || (savedLevel.Definition?.Mechanic == LevelMechanic.Dynamite && !savedLevel.HasPendingWin() && !DynamiteRoundState.IsValid(savedLevel))
                        || (savedLevel.Definition?.Mechanic == LevelMechanic.Ice && !savedLevel.HasPendingWin() && !IceRoundState.IsValid(savedLevel)))
                    { _gameSaveService.DeleteSave(); _levelSaveDataManager.CreateDefaultLevelSaveData(); return false; }
                    if (savedLevel.Definition != null) (_levelDataCreator as LevelDataCreator)?.RestoreDefinition(savedLevel.Definition);
                    if(string.IsNullOrEmpty(savedLevel.RoundId) || savedLevel.LevelId<0)
                    {
                        savedLevel.RoundId=string.IsNullOrEmpty(savedLevel.RoundId)?Guid.NewGuid().ToString("N"):savedLevel.RoundId;
                        savedLevel.LevelId=_levelTracker.GetLevelId();
                        _gameSaveService.Save(savedLevel);
                    }
                    savedLevel.RevealedGuessNumbers ??= new System.Collections.Generic.List<int>();
                    _levelSaveDataManager.SetLevelSaveDataAsSaved(savedLevel);
                    return true;
                }
                if (savedLevel != null) _gameSaveService.DeleteSave();
            }

            _levelSaveDataManager.CreateDefaultLevelSaveData();
            return false;
        }
        private void OnLevelSuccess(object sender,EventArgs args)=>FinishRound(true);
        private void OnLevelFail(object sender,EventArgs args)=>FinishRound(false);
        public void FinishRound(bool won)
        {
            if(_disposed || _levelTracker.GetGameOption()!=GameOption.SinglePlayer || _isGameOver)return;
            LevelSaveData pending=null;
            if(won && _snapshots!=null && _snapshots.Value.TryCreateSnapshot(out pending,true))pending.PendingWin=true;
            _isGameOver=true;_state=won?LevelFlowState.EndingSuccess:LevelFlowState.EndingFail;
            ObserverNotification.Run(()=>_gamePopupCreator.EndTutorials(won));
            bool saved=false;
            _settlement=new RewardSettlement<SinglePlayerRoundResult>(()=>{
                if(pending!=null && !saved){_gameSaveService.Save(pending);saved=true;}
                return _completion.Complete(won);
            });
            _settlement.Changed+=OnSettlementChanged;_settlement.Retry();
        }
        private void OnSettlementChanged()
        {
            if(_disposed)return;
            if(_settlement.State!=RewardSaveState.Completed)
            {
                if(_settlement.State==RewardSaveState.RetryRequired)_endPresenter.PresentPendingReward(_settlement.State,RetryReward,_state==LevelFlowState.EndingSuccess);
                return;
            }
            if(_endPresented)return;_endPresented=true;
            var result=_settlement.Value;
            ObserverNotification.Run(()=>GameTelemetry.Finish(result.Won?"win":"fail",result.GuessCount,result.EarnedCoins,result.EarnedCrystals));
            _endPresenter.Present(result,this);
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            if(_settlement!=null)_settlement.Changed-=OnSettlementChanged;
            _endPresenter.PrepareRound(); _presentation.Cancel(); GameTelemetry.Abandon("scene_exit");
        }
    }
}

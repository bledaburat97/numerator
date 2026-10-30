using System;
using DG.Tweening;
using Scripts;
using UnityEngine.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Zenject;

namespace Game
{
    public class LevelFlowOrchestrator : ILevelFlowOrchestrator, IDisposable
    {
        private readonly ILevelTracker _levelTracker;
        private readonly ILevelDataCreator _levelDataCreator;
        private readonly IGameSaveService _gameSaveService;
        private readonly ILevelSaveDataManager _levelSaveDataManager;
        private readonly IBoardAreaController _boardAreaController;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IResultAreaController _resultAreaController;
        private readonly ILifeBarController _lifeBarController;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IGamePopupCreator _gamePopupCreator;
        private readonly IGameUIController _gameUIController;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;
        private readonly IVerticalCrystalProgressController _verticalCrystalProgressController;
        private readonly ILevelStartAnimationManager _levelStartAnimationManager;
        private readonly ICardItemLocator _cardItemLocator;
        private readonly IGuessManager _guessManager;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly ICardItemInfoPopupController _cardItemInfoPopupController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;
        private readonly ICardInteractionManager _cardInteractionManager;
        private readonly IResultManager _resultManager;
        private readonly IRoundStateManager _roundStateManager;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private readonly ILevelSuccessAnimationManager _levelSuccessAnimationManager;
        private readonly ILevelFailAnimationManager _levelFailAnimationManager;
        private readonly IFadePanelController _fadePanelController;

        [InjectOptional] private IMultiplayerGameController _multiplayer;
        private bool _multiplayerSubscribed;
        private Sequence _multiplayerReveal;
        private LevelFlowState _state;
        private bool _isGameOver;

        [Inject]
        public LevelFlowOrchestrator(
            ILevelTracker levelTracker,
            ILevelDataCreator levelDataCreator,
            IGameSaveService gameSaveService,
            ILevelSaveDataManager levelSaveDataManager,
            IBoardAreaController boardAreaController,
            IBoardHolderCountManager boardHolderCountManager,
            ITargetNumberCreator targetNumberCreator,
            IResultAreaController resultAreaController,
            ILifeBarController lifeBarController,
            IInitialCardAreaController initialCardAreaController,
            IGamePopupCreator gamePopupCreator,
            IGameUIController gameUIController,
            IGamePowerUpAreaController gamePowerUpAreaController,
            IVerticalCrystalProgressController verticalCrystalProgressController,
            ILevelStartAnimationManager levelStartAnimationManager,
            ICardItemLocator cardItemLocator,
            IGuessManager guessManager,
            ICardItemInfoManager cardItemInfoManager,
            ICardItemInfoPopupController cardItemInfoPopupController,
            ICardPlacementCoordinator cardPlacementCoordinator,
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
            _boardHolderCountManager = boardHolderCountManager;
            _targetNumberCreator = targetNumberCreator;
            _resultAreaController = resultAreaController;
            _lifeBarController = lifeBarController;
            _initialCardAreaController = initialCardAreaController;
            _gamePopupCreator = gamePopupCreator;
            _gameUIController = gameUIController;
            _gamePowerUpAreaController = gamePowerUpAreaController;
            _verticalCrystalProgressController = verticalCrystalProgressController;
            _levelStartAnimationManager = levelStartAnimationManager;
            _cardItemLocator = cardItemLocator;
            _guessManager = guessManager;
            _cardItemInfoManager = cardItemInfoManager;
            _cardItemInfoPopupController = cardItemInfoPopupController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
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
            _gamePopupCreator.PrepareTutorials();
            _levelEndPopupController.SetPopupStatus(false);
            _levelEndPopupController.SetAllStatusFalse();
            _gamePowerUpAreaController.Refresh();
            _verticalCrystalProgressController.Initialize();
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
            _boardAreaController.CreateBoard();
            _targetNumberCreator.SetTargetNumber(_boardHolderCountManager.GetBoardHolderCount());
            _resultAreaController.Initialize(!isResumeFlow);
            _lifeBarController.SetFade(!isResumeFlow);
            _initialCardAreaController.Initialize(!isResumeFlow);
            _gameUIController.Initialize(!isResumeFlow);

            InitializeGameplaySystems(!isResumeFlow);
            GameTelemetry.BeginSingle(_levelTracker.GetLevelId() + 1, _boardHolderCountManager.GetBoardHolderCount(),
                _levelDataCreator.GetLevelData().NumOfCards, isResumeFlow ? LevelEntryMode.Resume : mode,
                _roundStateManager.GetTriedCardsList().Count);

            if (isResumeFlow)
            {
                _state = LevelFlowState.Playing;
                _gamePopupCreator.StartTutorials();
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
                        _gamePopupCreator.StartTutorials();
                    }
                });
            }
            else
            {
                _state = LevelFlowState.Playing;
                _gamePopupCreator.StartTutorials();
            }
        }

        private void BeginMultiplayer()
        {
            _lifeBarController.DisableProgressBar();
            _gameUIController.InitializeForMultiplayer();
            if (!_multiplayerSubscribed)
            {
                _multiplayer.RoundPreparing += OnMultiplayerRound;
                _multiplayer.GuessAccepted += OnMultiplayerFeedback;
                _multiplayer.StateChanged += OnMultiplayerState;
                _multiplayer.MatchEnded += OnMultiplayerEnd;
                _multiplayer.RequestRejected += OnMultiplayerError;
                _multiplayerSubscribed = true;
            }
            _gamePopupCreator.StartTutorials();
            _gameUIController.SetAllButtonsUnclickable();
            _gameUIController.SetButtonClickable(true, GameUIButtonType.Settings);
            (_gameUIController as GameUIController)?.SetMultiplayerStatus("Rakibin bağlantısı bekleniyor…", 0);
            _multiplayer.Initialize();
        }
        private void OnMultiplayerRound(object sender, int round)
        {
            _multiplayerReveal?.Kill(); _multiplayerReveal = null;
            _isGameOver = false; _state = LevelFlowState.Building;
            _levelEndPopupController.SetPopupStatus(false); _levelEndPopupController.SetAllStatusFalse();
            _cardItemInfoPopupController.ClearCardHolderIndicatorButtons();
            _initialCardAreaController.ClearInitialCardHolders();
            _resultAreaController.RemoveResultBlocks();
            _levelDataCreator.SetMultiplayerLevelData(); _levelSaveDataManager.CreateDefaultLevelSaveData();
            _roundStateManager.Initialize(); _boardAreaController.CreateBoard();
            _targetNumberCreator.SetTargetNumber(_levelDataCreator.GetLevelData().NumOfBoardHolders);
            _initialCardAreaController.Initialize(false); _resultAreaController.Initialize(false);
            _cardItemLocator.Initialize(); _cardItemInfoManager.Initialize();
            _cardPlacementCoordinator.Initialize(); _cardInteractionManager.Initialize(); _cardItemInfoPopupController.Initialize();
            _gameUIController.InitializeForMultiplayer();
            _initialCardAreaController.ChangeFadeInitialArea(0f, 1f).Complete(true);
            _resultAreaController.ChangeFade(0f, 1f).Complete(true);
            _gameUIController.ChangeFadeTopAreaButtons(0f, 1f).Complete(true);
            _gameUIController.ChangeFadeMiddleAreaButtons(0f, 1f).Complete(true);
            _fadePanelController.SetFadeImageStatus(false); _fadePanelController.SetBoardFadeImageStatus(false);
            _state = LevelFlowState.Playing;
            OnMultiplayerState(this, _multiplayer.State);
            _multiplayer.BoardReady(round);
            GameTelemetry.BeginDuel(round, MultiplayerRules.For(PlayerLobby.Instance.RoomDifficulty));
        }
        private void OnMultiplayerFeedback(object sender, MultiplayerFeedback feedback)
        {
            _roundStateManager.AddTriedCards(new System.Collections.Generic.List<int>(feedback.Cards));
            GameTelemetry.GuessAccepted(_roundStateManager.GetTriedCardsList().Count);
            _resultAreaController.AddResultBlock(new ResultBlockModel { finalNumbers = new System.Collections.Generic.List<int>(feedback.Cards),
                correctPosCount = feedback.Correct, wrongPosCount = feedback.Misplaced });
            (_resultAreaController as ResultAreaController)?.LabelLatestMultiplayerGuess(feedback.Player == NetworkManager.Singleton.LocalClientId);
            if (!feedback.Won) _gameUIController.TriggerResetNumbers();
        }
        private void OnMultiplayerState(object sender, MultiplayerPublicState state)
        {
            bool canPlay = _multiplayer.IsLocalTurn;
            _gameUIController.SetButtonClickable(canPlay, GameUIButtonType.Check);
            _gameUIController.SetButtonClickable(canPlay, GameUIButtonType.Reset);
            _gameUIController.SetButtonClickable(state.Phase != MultiplayerPhase.Finished && state.Phase != MultiplayerPhase.Disconnected, GameUIButtonType.Settings);
            for (int i = 0; i < _initialCardAreaController.GetCardCount(); i++)
                if (_initialCardAreaController.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().SetMovementLocked(!canPlay);
            string status = state.Phase == MultiplayerPhase.Preparing ? "Maç hazırlanıyor…"
                : state.Phase == MultiplayerPhase.Playing ? (canPlay ? "Sıra sende" : state.CurrentPlayer == NetworkManager.Singleton.LocalClientId ? "Tahmin gönderiliyor…" : "Sıra rakipte")
                : state.Phase == MultiplayerPhase.Disconnected ? "Rakiple bağlantı kesildi" : "Maç tamamlandı";
            if (state.Phase == MultiplayerPhase.Finished && state.RematchVotes != 0)
                status = _multiplayer.LocalRematchRequested ? "Rakibin yanıtı bekleniyor…" : "Rakibin yeni oyun teklif ediyor";
            (_gameUIController as GameUIController)?.SetMultiplayerTurn(status, state.Deadline,
                state.Phase == MultiplayerPhase.Playing && state.CurrentPlayer == NetworkManager.Singleton.LocalClientId);
            if (state.Phase == MultiplayerPhase.Finished && state.RematchVotes != 0)
            {
                _levelEndPopupController.InitButton(LevelFinishButtonType.Game, _multiplayer.LocalRematchRequested ? "Rakip bekleniyor…" : "Teklifi kabul et",
                    _multiplayer.LocalRematchRequested ? (Action)(() => { }) : _multiplayer.RequestRematch);
                _levelEndPopupController.ChangeFadeButtons(0f, 1f).Complete(true);
            }
        }
        private void OnMultiplayerError(object sender, string error) =>
            (_gameUIController as GameUIController)?.SetMultiplayerStatus(error, _multiplayer.State.Deadline);
        private void OnMultiplayerEnd(object sender, MultiplayerOutcome outcome)
        {
            GameTelemetry.Finish(outcome.Disconnected ? "disconnected" : outcome.Winner == NetworkManager.Singleton.LocalClientId ? "win" : "lose",
                _roundStateManager.GetTriedCardsList().Count);
            if (outcome.Disconnected && _isGameOver)
            {
                (_levelEndPopupController as LevelEndPopupController)?.SetMultiplayerOpponentLeft();
                return;
            }
            _multiplayerReveal?.Kill();
            _isGameOver = true;
            bool won = !outcome.Disconnected && outcome.Winner == NetworkManager.Singleton.LocalClientId;
            _state = won ? LevelFlowState.EndingSuccess : LevelFlowState.EndingFail;
            _gameUIController.SetAllButtonsUnclickable(); _gamePopupCreator.EndTutorials(false);
            _resultAreaController.EnableReview();
            _gameUIController.TriggerResetNumbers();
            _multiplayerReveal = DOTween.Sequence().AppendInterval(.35f).AppendCallback(() =>
            {
                if (_levelDataCreator.GetLevelData() != null && outcome.Answer.Length > 0 && outcome.Answer.Length == _levelDataCreator.GetLevelData().NumOfBoardHolders)
                {
                    (_targetNumberCreator as TargetNumberCreator)?.RevealMultiplayerAnswer(outcome.Answer);
                    for (int i = 0; i < outcome.Answer.Length; i++) _cardPlacementCoordinator.TryRevealAndLockCard(i, outcome.Answer[i] - 1, false);
                    _resultAreaController.RevealAnswer(outcome.Answer);
                    foreach (var card in _cardPlacementCoordinator.GetCardsOnBoard()) card.GetView().SetResultOutline(CardResultState.Correct);
                }
                _initialCardAreaController.ChangeFadeInitialArea(0f, 0f).Complete(true);
                _levelEndPopupController.SetAllStatusFalse(); _levelEndPopupController.SetOutcome(won);
                (_levelEndPopupController as LevelEndPopupController)?.SetMultiplayerResult(won, outcome.Disconnected, won ? outcome.Reward : null);
                _levelEndPopupController.InitText(outcome.Disconnected ? "Rakiple bağlantı kesildi" : won ? "Kazandın!" : "Rakibin kazandı");
                if (!outcome.Disconnected && _multiplayer.State.Phase != MultiplayerPhase.Disconnected) _levelEndPopupController.InitButton(LevelFinishButtonType.Game, "Tekrar oyna", _multiplayer.RequestRematch);
                _levelEndPopupController.InitButton(LevelFinishButtonType.Menu, "Menü", OnMenuButtonClicked);
                _levelEndPopupController.SetPopupStatus(true);
                if (_multiplayer.State.Phase == MultiplayerPhase.Disconnected) (_levelEndPopupController as LevelEndPopupController)?.SetMultiplayerOpponentLeft();
                _levelEndPopupController.ScaleUpText(.2f);
                _levelEndPopupController.ChangeFadeButtons(.2f, 1f);
            }).SetLink(_gamePopupCreator.GetCanvasRectTransform().gameObject);
        }
        public void Dispose()
        {
            GameTelemetry.Abandon("scene_exit");
            _multiplayerReveal?.Kill();
            _resultManager.LevelSuccessEvent -= OnLevelSuccess; _guessManager.LevelFailEvent -= OnLevelFail;
            if (!_multiplayerSubscribed) return;
            _multiplayer.RoundPreparing -= OnMultiplayerRound; _multiplayer.GuessAccepted -= OnMultiplayerFeedback;
            _multiplayer.StateChanged -= OnMultiplayerState; _multiplayer.MatchEnded -= OnMultiplayerEnd;
            _multiplayer.RequestRejected -= OnMultiplayerError; _multiplayer.Stop(); _multiplayerSubscribed = false;
        }

        private bool TryPrepareSinglePlayerSaveData(LevelEntryMode mode)
        {
            if (mode == LevelEntryMode.Resume)
            {
                LevelSaveData savedLevel = _gameSaveService.GetSavedLevel();
                if (savedLevel != null && (!_levelTracker.IsFirstLevelTutorial()
                    || FirstLevelTutorialController.CanResume(savedLevel, _levelDataCreator.GetLevelData().MaxNumOfTries)))
                {
                    _levelSaveDataManager.SetLevelSaveDataAsSaved(savedLevel);
                    return true;
                }
                if (savedLevel != null) _gameSaveService.DeleteSave();
            }

            _levelSaveDataManager.CreateDefaultLevelSaveData();
            return false;
        }

        private void InitializeGameplaySystems(bool deferRewardIntroAnimation)
        {
            _cardItemLocator.Initialize();
            _cardItemInfoManager.Initialize();
            _cardPlacementCoordinator.Initialize();
            _guessManager.Initialize(deferRewardIntroAnimation);
            _cardItemInfoPopupController.Initialize();
            _cardInteractionManager.Initialize();
            _resultManager.TryAddTriedCards();
            _gamePowerUpAreaController.Refresh();
        }

        private void OnLevelSuccess(object sender, EventArgs args)
        {
            if (_levelTracker.GetGameOption() != GameOption.SinglePlayer || _isGameOver) return;

            _isGameOver = true;
            _state = LevelFlowState.EndingSuccess;
            _gamePopupCreator.EndTutorials(true);
            _gameUIController.SetAllButtonsUnclickable();
            _resultAreaController.RevealAnswer(_targetNumberCreator.GetTargetCardsList());
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
            _resultAreaController.RevealAnswer(_targetNumberCreator.GetTargetCardsList());
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
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer) MultiplayerConnection.Shutdown();
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

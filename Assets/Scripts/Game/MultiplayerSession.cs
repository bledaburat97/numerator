using System;
using DG.Tweening;
using Scripts;
using Zenject;

namespace Game
{
    public sealed class MultiplayerSession : ILevelSession
    {
        private readonly ILevelDataCreator _levelDataCreator;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IResultAreaController _resultAreaController;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IGamePopupCreator _gamePopupCreator;
        private readonly IGameUIController _gameUIController;
        private readonly ICardPlacementCoordinator _cardPlacementCoordinator;
        private readonly IRoundStateManager _roundStateManager;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private readonly IFadePanelController _fadePanelController;
        private readonly IMultiplayerGameController _multiplayer;
        private readonly RoundBuilder _builder;
        private readonly LevelPresentation _presentation;
        private readonly MultiplayerRoundPresenter _hud;
        private bool _subscribed, _isGameOver, _disposed;
        private Sequence _multiplayerReveal;
        private LevelFlowState _state = LevelFlowState.Idle;
        [Inject]
        public MultiplayerSession(
            ILevelDataCreator levelDataCreator,
            ITargetNumberCreator targetNumberCreator,
            IResultAreaController resultAreaController,
            IInitialCardAreaController initialCardAreaController,
            IGamePopupCreator gamePopupCreator,
            IGameUIController gameUIController,
            ICardPlacementCoordinator cardPlacementCoordinator,
            IRoundStateManager roundStateManager,
            ILevelEndPopupController levelEndPopupController,
            IFadePanelController fadePanelController,
            IMultiplayerGameController multiplayer,
            RoundBuilder builder,
            LevelPresentation presentation,
            MultiplayerRoundPresenter hud)
        {
            _levelDataCreator = levelDataCreator;
            _targetNumberCreator = targetNumberCreator;
            _resultAreaController = resultAreaController;
            _initialCardAreaController = initialCardAreaController;
            _gamePopupCreator = gamePopupCreator;
            _gameUIController = gameUIController;
            _cardPlacementCoordinator = cardPlacementCoordinator;
            _roundStateManager = roundStateManager;
            _levelEndPopupController = levelEndPopupController;
            _fadePanelController = fadePanelController;
            _multiplayer = multiplayer;
            _builder = builder;
            _presentation = presentation;
            _hud = hud;
        }

        public LevelFlowState State => _state;
        public bool IsGameOver => _isGameOver;
        public void Begin(LevelEntryMode mode)
        {
            if (_disposed) return;
            _presentation.PrepareRound(); _isGameOver = false; _state = LevelFlowState.Building;
            _hud.BeginWaiting();
            if (!_subscribed)
            {
                _multiplayer.RoundPreparing += OnMultiplayerRound; _multiplayer.GuessAccepted += OnMultiplayerFeedback;
                _multiplayer.StateChanged += OnMultiplayerState; _multiplayer.MatchEnded += OnMultiplayerEnd;
                _multiplayer.RequestRejected += OnMultiplayerError; _subscribed = true;
            }
            _gamePopupCreator.StartTutorials(); _multiplayer.Initialize();
        }
        private void OnMultiplayerRound(object sender, int round)
        {
            _multiplayerReveal?.Kill(); _multiplayerReveal = null;
            _isGameOver = false; _state = LevelFlowState.Building;
            _levelEndPopupController.SetPopupStatus(false); _levelEndPopupController.SetAllStatusFalse();
            _builder.BuildMultiplayer();
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
            bool isLocalGuess = feedback.Player == _multiplayer.LocalPlayerId;
            _roundStateManager.AddTriedCards(new System.Collections.Generic.List<int>(feedback.Cards));
            GameTelemetry.GuessAccepted(_roundStateManager.GetTriedCardsList().Count,isLocalGuess);
            _resultAreaController.AddResultBlock(new ResultBlockModel { finalNumbers = new System.Collections.Generic.List<int>(feedback.Cards),
                correctPosCount = feedback.Correct, wrongPosCount = feedback.Misplaced,
                isLocalGuess = isLocalGuess, playerName = _multiplayer.GetPlayerName(feedback.Player) });
            if (!feedback.Won && isLocalGuess) _gameUIController.TriggerResetNumbers();
        }
        private void OnMultiplayerState(object sender, MultiplayerPublicState state) => _hud.RefreshState(state);
        private void OnMultiplayerError(object sender, string error) => _hud.ShowError(error);
        private void OnMultiplayerEnd(object sender, MultiplayerOutcome outcome)
        {
            GameTelemetry.Finish(outcome.Disconnected ? "disconnected" : outcome.Winner == _multiplayer.LocalPlayerId ? "win" : "lose",
                _roundStateManager.GetTriedCardsList().Count);
            if (outcome.Disconnected && _isGameOver)
            {
                _hud.ShowDisconnected();
                return;
            }
            _multiplayerReveal?.Kill();
            _isGameOver = true;
            bool won = !outcome.Disconnected && outcome.Winner == _multiplayer.LocalPlayerId;
            _state = won ? LevelFlowState.EndingSuccess : LevelFlowState.EndingFail;
            _gameUIController.SetAllButtonsUnclickable(); _gamePopupCreator.EndTutorials(false);
            _gameUIController.SetButtonClickable(true, GameUIButtonType.Settings);
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
                _hud.ShowOutcome(outcome, won);
            }).SetLink(_gamePopupCreator.GetCanvasRectTransform().gameObject);
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            _multiplayerReveal?.Kill(); _presentation.Dispose(); GameTelemetry.Abandon("scene_exit");
            if (!_subscribed) return;
            _multiplayer.RoundPreparing -= OnMultiplayerRound; _multiplayer.GuessAccepted -= OnMultiplayerFeedback;
            _multiplayer.StateChanged -= OnMultiplayerState; _multiplayer.MatchEnded -= OnMultiplayerEnd;
            _multiplayer.RequestRejected -= OnMultiplayerError; _multiplayer.Stop(); _subscribed = false;
        }
    }
}

using System;
using DG.Tweening;
using Scripts;
using Zenject;

namespace Game
{
    public sealed class MultiplayerRoundPresenter
    {
        private readonly ILifeBarController _lifeBarController;
        private readonly IInitialCardAreaController _initialCardAreaController;
        private readonly IGameUIController _gameUIController;
        private readonly ILevelEndPopupController _levelEndPopupController;
        private readonly IMultiplayerGameController _multiplayer;

        [Inject]
        public MultiplayerRoundPresenter(
            ILifeBarController lifeBarController,
            IInitialCardAreaController initialCardAreaController,
            IGameUIController gameUIController,
            ILevelEndPopupController levelEndPopupController,
            IMultiplayerGameController multiplayer)
        {
            _lifeBarController = lifeBarController;
            _initialCardAreaController = initialCardAreaController;
            _gameUIController = gameUIController;
            _levelEndPopupController = levelEndPopupController;
            _multiplayer = multiplayer;
        }

        public void BeginWaiting()
        {
            _lifeBarController.DisableProgressBar(); _gameUIController.InitializeForMultiplayer();
            _gameUIController.SetAllButtonsUnclickable(); _gameUIController.SetButtonClickable(true, GameUIButtonType.Settings);
            (_gameUIController as GameUIController)?.SetMultiplayerStatus("Rakibin bağlantısı bekleniyor…", 0);
        }
        public void ShowError(string error) => (_gameUIController as GameUIController)?.SetMultiplayerStatus(error, _multiplayer.State.Deadline);
        public void RefreshState(MultiplayerPublicState state)
        {
            bool canPlay = _multiplayer.IsLocalTurn;
            bool canArrangeCards = state.Phase == MultiplayerPhase.Playing;
            _gameUIController.SetButtonClickable(canPlay, GameUIButtonType.Check);
            _gameUIController.SetButtonClickable(canArrangeCards, GameUIButtonType.Reset);
            _gameUIController.SetButtonClickable(true, GameUIButtonType.Settings);
            for (int i = 0; i < _initialCardAreaController.GetCardCount(); i++)
                if (_initialCardAreaController.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().SetMovementLocked(!canArrangeCards);
            string status = state.Phase == MultiplayerPhase.Preparing ? "Maç hazırlanıyor…"
                : state.Phase == MultiplayerPhase.Playing ? (canPlay ? "Sıra sende" : state.CurrentPlayer == _multiplayer.LocalPlayerId ? "Tahmin gönderiliyor…" : "Sıra: " + _multiplayer.GetPlayerName(state.CurrentPlayer))
                : state.Phase == MultiplayerPhase.Disconnected ? "Maç sona erdi" : "Maç tamamlandı";
            if (state.Phase == MultiplayerPhase.Finished && state.RematchVotes != 0)
                status = _multiplayer.LocalRematchRequested ? "Diğer oyuncular bekleniyor…" : "Yeni oyun teklifi var";
            if (!string.IsNullOrEmpty(_multiplayer.StatusNotice)) status = _multiplayer.StatusNotice;
            (_gameUIController as GameUIController)?.SetMultiplayerTurn(status, state.Deadline,
                state.Phase == MultiplayerPhase.Playing && state.CurrentPlayer == _multiplayer.LocalPlayerId);
            if (state.Phase == MultiplayerPhase.Finished && state.RematchVotes != 0)
            {
                _levelEndPopupController.InitButton(LevelFinishButtonType.Game, _multiplayer.LocalRematchRequested ? "Oyuncular bekleniyor…" : "Teklifi kabul et",
                    _multiplayer.LocalRematchRequested ? (Action)(() => { }) : _multiplayer.RequestRematch);
                _levelEndPopupController.ChangeFadeButtons(0f, 1f).Complete(true);
            }
                }
        public void ShowOutcome(MultiplayerOutcome outcome, bool won)
        {
            _levelEndPopupController.SetAllStatusFalse(); _levelEndPopupController.SetOutcome(won);
            (_levelEndPopupController as LevelEndPopupController)?.SetMultiplayerResult(won, outcome.Disconnected, won ? outcome.Reward : null);
            _levelEndPopupController.InitText(outcome.Disconnected ? "Maç sona erdi" : won ? "Kazandın!" : _multiplayer.GetPlayerName(outcome.Winner) + " kazandı");
            if (!outcome.Disconnected && _multiplayer.State.Phase != MultiplayerPhase.Disconnected)
                _levelEndPopupController.InitButton(LevelFinishButtonType.Game, "Tekrar oyna", _multiplayer.RequestRematch);
            _levelEndPopupController.SetPopupStatus(true);
            if (_multiplayer.State.Phase == MultiplayerPhase.Disconnected) ShowDisconnected();
            _levelEndPopupController.ScaleUpText(.2f); _levelEndPopupController.ChangeFadeButtons(.2f, 1);
        }
        public void ShowDisconnected() => (_levelEndPopupController as LevelEndPopupController)?.SetMultiplayerOpponentLeft(_multiplayer.DisconnectMessage);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Scripts;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Game
{
    // This component transports commands and public snapshots; the answer never lives in a NetworkVariable.
    public class MultiplayerGameController : NetworkBehaviour, IMultiplayerGameController
    {
        [Inject] private ILevelTracker _tracker;
        private readonly HashSet<ulong> _joined = new HashSet<ulong>();
        private MultiplayerMatchState _match;
        private bool _initialized, _registered, _helloSent, _pending;
        private int _announcedRound, _builtRound, _lastFeedbackTurn;
        private ulong _host, _guest;
        private double _waitingSince;
        private Difficulty _difficulty;
        private int _lastOutcomeRound;
        private RewardType _roundReward;
        public MultiplayerPublicState State { get; private set; }
        public bool IsLocalTurn => _initialized && !_pending && State.Phase == MultiplayerPhase.Playing
            && NetworkManager != null && State.CurrentPlayer == NetworkManager.LocalClientId;
        public event EventHandler<int> RoundPreparing;
        public event EventHandler<MultiplayerFeedback> GuessAccepted;
        public event EventHandler<MultiplayerPublicState> StateChanged;
        public event EventHandler<MultiplayerOutcome> MatchEnded;
        public event EventHandler<string> RequestRejected;

        public void Initialize()
        {
            if (_initialized || _tracker.GetGameOption() != GameOption.MultiPlayer) return;
            _initialized = true; _waitingSince = Time.realtimeSinceStartupAsDouble;
            TryHello();
        }
        public override void OnNetworkSpawn() => TryHello();
        private void TryHello()
        {
            if (!_initialized || !IsSpawned || _helloSent) return;
            if (!_registered) { NetworkManager.OnClientDisconnectCallback += OnDisconnected; _registered = true; }
            _helloSent = true; JoinedGameServerRpc();
            if (_announcedRound > 0) BuildAnnouncedRound();
        }
        [ServerRpc(RequireOwnership = false)]
        private void JoinedGameServerRpc(ServerRpcParams rpc = default)
        {
            ulong sender = rpc.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClientsIds.Contains(sender)) return;
            _joined.Add(sender);
            if (_match == null && _joined.Count == 2) StartRound(1);
        }
        private void StartRound(int number)
        {
            if (!IsServer || NetworkManager.ConnectedClientsIds.Count != 2) return;
            _host = Unity.Netcode.NetworkManager.ServerClientId;
            _guest = NetworkManager.ConnectedClientsIds.First(id => id != _host);
            if (_match == null) _difficulty = PlayerLobby.Instance.RoomDifficulty;
            var settings = MultiplayerRules.For(_difficulty);
            var fruits = Enumerable.Range(1, settings.FruitCount).ToArray();
            for (int i = fruits.Length - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); int old = fruits[i]; fruits[i] = fruits[j]; fruits[j] = old; }
            int[] answer = fruits.Take(settings.BoardCount).ToArray();
            double seconds = MultiplayerRules.TurnSeconds;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) { answer = MultiplayerNetworkProbe.Answer(number); seconds = 10; }
#endif
            _match = new MultiplayerMatchState(_host, _guest, number, answer, _difficulty, seconds);
            BeginRoundClientRpc(number, _host, _guest, (byte)_difficulty);
        }
        [ClientRpc]
        private void BeginRoundClientRpc(int number, ulong host, ulong guest, byte difficulty)
        {
            if (number <= _announcedRound) return;
            _difficulty = MultiplayerRules.For((Difficulty)difficulty).Difficulty;
            _host = host; _guest = guest; _announcedRound = number; _lastFeedbackTurn = 0; _pending = false;
            State = new MultiplayerPublicState(number, 0, MultiplayerPhase.Preparing, MultiplayerRules.NoPlayer, MultiplayerRules.NoPlayer, 0, 0);
            _waitingSince = Time.realtimeSinceStartupAsDouble;
            BuildAnnouncedRound();
        }
        private void BuildAnnouncedRound()
        {
            if (!_initialized || _builtRound >= _announcedRound) return;
            _tracker.SetMultiplayerLevelDifficulty(_difficulty);
            _builtRound = _announcedRound;
            RoundPreparing?.Invoke(this, _announcedRound);
        }
        public void BoardReady(int round) { if (_initialized && IsSpawned) BoardReadyServerRpc(round); }
        [ServerRpc(RequireOwnership = false)]
        private void BoardReadyServerRpc(int round, ServerRpcParams rpc = default)
        {
            if (_match == null) return;
            _match.MarkBoardReady(rpc.Receive.SenderClientId, round, NetworkManager.ServerTime.Time);
            BroadcastState();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SendProbeCommand(int round, int turn, int[] cards)
        { if (MultiplayerNetworkProbe.IsRunning) SubmitGuessServerRpc(round, turn, cards); }
#endif
        public void SubmitGuess(int[] cards)
        {
            if (!IsLocalTurn || cards == null) return;
            _pending = true; StateChanged?.Invoke(this, State);
            SubmitGuessServerRpc(State.Round, State.Turn, cards);
        }
        [ServerRpc(RequireOwnership = false)]
        private void SubmitGuessServerRpc(int round, int turn, int[] cards, ServerRpcParams rpc = default)
        {
            if (_match == null) return;
            ulong sender = rpc.Receive.SenderClientId;
            if (!_match.TrySubmit(sender, round, turn, cards, NetworkManager.ServerTime.Time, out var feedback, out string error))
            {
                BroadcastState();
                RejectClientRpc(error, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } });
                return;
            }
            FeedbackClientRpc(feedback.Round, feedback.Turn, feedback.Player, feedback.Cards, feedback.Correct, feedback.Misplaced);
            BroadcastState();
            if (feedback.Won)
            {
                _roundReward = PowerUpTypeUtility.OrderedRewardTypes[UnityEngine.Random.Range(0, PowerUpTypeUtility.OrderedRewardTypes.Length)];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (MultiplayerNetworkProbe.IsRunning) _roundReward = (RewardType)((round - 1) % 3);
#endif
                OutcomeClientRpc(round, sender, _match.RevealAnswer(), (byte)_roundReward);
            }
        }
        [ClientRpc]
        private void FeedbackClientRpc(int round, int turn, ulong player, int[] cards, int correct, int misplaced)
        {
            if (!_initialized || round != State.Round || turn <= _lastFeedbackTurn) return;
            _lastFeedbackTurn = turn; _pending = false;
            GuessAccepted?.Invoke(this, new MultiplayerFeedback(round, turn, player, cards, correct, misplaced));
        }
        [ClientRpc]
        private void RejectClientRpc(string error, ClientRpcParams rpc = default)
        { if (!_initialized) return; _pending = false; StateChanged?.Invoke(this, State); RequestRejected?.Invoke(this, error); }
        private void BroadcastState()
        {
            var s = _match.Snapshot;
            StateClientRpc(s.Round, s.Turn, (byte)s.Phase, s.CurrentPlayer, s.Winner, s.Deadline, s.RematchVotes);
        }
        [ClientRpc]
        private void StateClientRpc(int round, int turn, byte phase, ulong player, ulong winner, double deadline, byte votes)
        {
            if (!_initialized || round < State.Round) return;
            if (round != State.Round || turn != State.Turn || (MultiplayerPhase)phase != MultiplayerPhase.Playing) _pending = false;
            State = new MultiplayerPublicState(round, turn, (MultiplayerPhase)phase, player, winner, deadline, votes);
            StateChanged?.Invoke(this, State);
        }
        [ClientRpc]
        private void OutcomeClientRpc(int round, ulong winner, int[] answer, byte reward)
        {
            if (!_initialized || round != State.Round || round <= _lastOutcomeRound || State.Phase != MultiplayerPhase.Finished
                || State.Winner != winner || reward > (byte)RewardType.Bomb) return;
            _lastOutcomeRound = round;
            var type = (RewardType)reward;
            if (winner == NetworkManager.LocalClientId)
            {
                switch (type)
                {
                    case RewardType.Revealing: _tracker.IncreaseRevealingPowerUpCount(); break;
                    case RewardType.Life: _tracker.IncreaseLifePowerUpCount(); break;
                    case RewardType.Bomb: _tracker.IncreaseBombPowerUpCount(); break;
                }
            }
            _pending = false; MatchEnded?.Invoke(this, new MultiplayerOutcome(round, winner, answer, false, type));
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void RepeatProbeOutcome()
        {
            if (MultiplayerNetworkProbe.IsRunning && IsServer && _match != null && _match.Phase == MultiplayerPhase.Finished)
                OutcomeClientRpc(_match.Round, _match.Winner, _match.RevealAnswer(), (byte)_roundReward);
        }
#endif
        public void RequestRematch()
        { if (_initialized && IsSpawned && State.Phase == MultiplayerPhase.Finished) RematchServerRpc(State.Round); }
        [ServerRpc(RequireOwnership = false)]
        private void RematchServerRpc(int round, ServerRpcParams rpc = default)
        {
            if (_match == null || !NetworkManager.ConnectedClientsIds.Contains(rpc.Receive.SenderClientId)) return;
            bool both = _match.VoteForRematch(rpc.Receive.SenderClientId, round);
            BroadcastState(); if (both) StartRound(_match.Round + 1);
        }
        public bool LocalRematchRequested => NetworkManager != null && (State.RematchVotes & (NetworkManager.LocalClientId == _host ? 1 : 2)) != 0;
        private void Update()
        {
            if (!_initialized) return;
            TryHello();
            if (IsServer && _match != null && _match.Tick(NetworkManager.ServerTime.Time)) BroadcastState();
            if (State.Phase == MultiplayerPhase.Preparing && Time.realtimeSinceStartupAsDouble - _waitingSince > 35)
                EndDisconnected();
        }
        private void OnDisconnected(ulong id)
        {
            if (!_initialized || MultiplayerConnection.IsLeaving) return;
            if (!IsServer || id != NetworkManager.LocalClientId) EndDisconnected();
        }
        private void EndDisconnected()
        {
            if (State.Phase == MultiplayerPhase.Disconnected) return;
            _match?.Disconnect();
            State = new MultiplayerPublicState(State.Round, State.Turn, MultiplayerPhase.Disconnected, MultiplayerRules.NoPlayer, MultiplayerRules.NoPlayer, 0, 0);
            _pending = false; StateChanged?.Invoke(this, State);
            MatchEnded?.Invoke(this, new MultiplayerOutcome(State.Round, MultiplayerRules.NoPlayer, IsServer ? _match?.RevealAnswer() : null, true));
        }
        public void Stop()
        {
            _initialized = false;
            if (_registered && NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnDisconnected;
            _registered = false;
        }
        public override void OnNetworkDespawn() => Stop();
        public override void OnDestroy() { Stop(); base.OnDestroy(); }
    }

    public interface IMultiplayerGameController
    {
        MultiplayerPublicState State { get; }
        bool IsLocalTurn { get; }
        bool LocalRematchRequested { get; }
        void Initialize();
        void BoardReady(int round);
        void SubmitGuess(int[] cards);
        void RequestRematch();
        void Stop();
        event EventHandler<int> RoundPreparing;
        event EventHandler<MultiplayerFeedback> GuessAccepted;
        event EventHandler<MultiplayerPublicState> StateChanged;
        event EventHandler<MultiplayerOutcome> MatchEnded;
        event EventHandler<string> RequestRejected;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    public enum MultiplayerPhase : byte { Preparing, Playing, Finished, Disconnected }

    public static class MultiplayerRules
    {
        public const double TurnSeconds = 60;
        public const ulong NoPlayer = ulong.MaxValue;
        public static MultiplayerSettings For(Difficulty difficulty) => new MultiplayerSettings(difficulty);
        public static MultiplayerSettings ForBoardCount(int count) => For((Difficulty)(count - 3));
    }

    public readonly struct MultiplayerSettings
    {
        public readonly Difficulty Difficulty;
        public int BoardCount => 3 + (int)Difficulty;
        public int FruitCount => 7 + (int)Difficulty;
        public string Name => Difficulty == Difficulty.Easy ? "Kolay" : Difficulty == Difficulty.Medium ? "Orta" : "Zor";
        public string Description => $"{Name} · {BoardCount} alan · {FruitCount} meyve";
        public MultiplayerSettings(Difficulty difficulty)
        {
            if (difficulty < Difficulty.Easy || difficulty > Difficulty.Hard) throw new ArgumentOutOfRangeException(nameof(difficulty));
            Difficulty = difficulty;
        }
    }

    public readonly struct MultiplayerFeedback
    {
        public readonly int Round, Turn, Correct, Misplaced;
        public readonly ulong Player;
        public readonly int[] Cards;
        public bool Won => Cards != null && Cards.Length > 0 && Correct == Cards.Length;
        public MultiplayerFeedback(int round, int turn, ulong player, int[] cards, int correct, int misplaced)
        { Round = round; Turn = turn; Player = player; Cards = (int[])cards.Clone(); Correct = correct; Misplaced = misplaced; }
    }

    public readonly struct MultiplayerPublicState
    {
        public readonly int Round, Turn;
        public readonly MultiplayerPhase Phase;
        public readonly ulong CurrentPlayer, Winner;
        public readonly double Deadline;
        public readonly byte RematchVotes;
        public MultiplayerPublicState(int round, int turn, MultiplayerPhase phase, ulong player, ulong winner, double deadline, byte votes)
        { Round = round; Turn = turn; Phase = phase; CurrentPlayer = player; Winner = winner; Deadline = deadline; RematchVotes = votes; }
    }

    public readonly struct MultiplayerOutcome
    {
        public readonly int Round;
        public readonly ulong Winner;
        public readonly int[] Answer;
        public readonly bool Disconnected;
        public readonly Game.RewardType? Reward;
        public MultiplayerOutcome(int round, ulong winner, int[] answer, bool disconnected, Game.RewardType? reward = null)
        { Round = round; Winner = winner; Answer = answer == null ? Array.Empty<int>() : (int[])answer.Clone(); Disconnected = disconnected; Reward = reward; }
    }

    // Only the host owns this state. The public snapshot contains no hidden answer.
    public sealed class MultiplayerMatchState
    {
        private readonly ulong _host, _guest;
        private readonly int[] _answer;
        private readonly double _turnSeconds;
        public MultiplayerSettings Settings { get; }
        private readonly HashSet<ulong> _ready = new HashSet<ulong>();
        private byte _votes;
        public int Round { get; }
        public int Turn { get; private set; }
        public MultiplayerPhase Phase { get; private set; } = MultiplayerPhase.Preparing;
        public ulong CurrentPlayer { get; private set; }
        public ulong Winner { get; private set; } = MultiplayerRules.NoPlayer;
        public double Deadline { get; private set; }
        public MultiplayerPublicState Snapshot => new MultiplayerPublicState(Round, Turn, Phase, CurrentPlayer, Winner, Deadline, _votes);

        public MultiplayerMatchState(ulong host, ulong guest, int round, IReadOnlyList<int> answer, Difficulty difficulty = Difficulty.Easy, double turnSeconds = MultiplayerRules.TurnSeconds)
        {
            Settings = MultiplayerRules.For(difficulty);
            if (host == guest || round < 1 || !ValidCards(answer) || turnSeconds <= 0 || double.IsNaN(turnSeconds) || double.IsInfinity(turnSeconds)) throw new ArgumentException("Invalid two-player match setup");
            _turnSeconds = turnSeconds;
            _host = host; _guest = guest; Round = round; _answer = answer.ToArray();
            CurrentPlayer = round % 2 == 1 ? host : guest;
        }
        public bool Contains(ulong player) => player == _host || player == _guest;
        public bool MarkBoardReady(ulong player, int round, double now)
        {
            if (Phase != MultiplayerPhase.Preparing || Round != round || !Contains(player)) return false;
            _ready.Add(player);
            if (_ready.Count != 2) return false;
            Phase = MultiplayerPhase.Playing; Turn = 1; Deadline = now + _turnSeconds;
            return true;
        }
        public bool Tick(double now)
        {
            if (Phase != MultiplayerPhase.Playing || now < Deadline) return false;
            Advance(now); return true;
        }
        public bool TrySubmit(ulong player, int round, int turn, IReadOnlyList<int> cards, double now,
            out MultiplayerFeedback feedback, out string error)
        {
            feedback = default;
            if (Phase != MultiplayerPhase.Playing) { error = "Maç henüz hazır değil."; return false; }
            if (round != Round || turn != Turn) { error = "Bu tahminin sırası geçti."; return false; }
            if (!Contains(player) || player != CurrentPlayer) { error = "Şu an rakibinin sırası."; return false; }
            if (now >= Deadline) { Advance(now); error = "Süren doldu. Sıra rakibinde."; return false; }
            if (!ValidCards(cards)) { error = $"{Settings.BoardCount} farklı meyveyi alanlara yerleştir."; return false; }
            int correct = 0, present = 0;
            for (int i = 0; i < _answer.Length; i++)
            {
                if (cards[i] == _answer[i]) correct++;
                if (Array.IndexOf(_answer, cards[i]) >= 0) present++;
            }
            feedback = new MultiplayerFeedback(Round, Turn, player, cards.ToArray(), correct, present - correct);
            if (feedback.Won) { Winner = player; Phase = MultiplayerPhase.Finished; Deadline = 0; }
            else Advance(now);
            error = null; return true;
        }
        public bool VoteForRematch(ulong player, int round)
        {
            if (Phase != MultiplayerPhase.Finished || round != Round || !Contains(player)) return false;
            _votes |= (byte)(player == _host ? 1 : 2);
            return _votes == 3;
        }
        public void Disconnect() { Phase = MultiplayerPhase.Disconnected; Winner = MultiplayerRules.NoPlayer; Deadline = 0; }
        public int[] RevealAnswer()
        {
            if (Phase != MultiplayerPhase.Finished && Phase != MultiplayerPhase.Disconnected)
                throw new InvalidOperationException("The answer remains private until the match ends");
            return (int[])_answer.Clone();
        }
        private void Advance(double now)
        { CurrentPlayer = CurrentPlayer == _host ? _guest : _host; Turn++; Deadline = now + _turnSeconds; }
        private bool ValidCards(IReadOnlyList<int> cards)
        {
            if (cards == null || cards.Count != Settings.BoardCount) return false;
            var seen = new HashSet<int>();
            foreach (int fruit in cards) if (fruit < 1 || fruit > Settings.FruitCount || !seen.Add(fruit)) return false;
            return true;
        }
    }
}

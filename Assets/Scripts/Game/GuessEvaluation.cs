using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    // Value snapshot: subsequent board edits never change a submitted guess.
    public sealed class GuessEvaluation : EventArgs
    {
        public int Number { get; }
        public IReadOnlyList<int> Cards { get; }
        public IReadOnlyList<CardResultState> States { get; }
        public int Correct { get; }
        public int Misplaced { get; }
        public IReadOnlyList<GuessGroupFeedback> Groups { get; }
        public bool Won => Correct == Cards.Count;

        public GuessEvaluation(int number, IReadOnlyList<int> cards, IReadOnlyList<int> answer, BoardPartition partition = null)
        {
            // Earlier Growing rows keep the answer prefix that existed when submitted.
            if (cards.Count > answer.Count || cards.Count == 0) throw new ArgumentException("A guess needs a nonempty answer prefix of matching size.");
            if (partition != null && partition.BoardCount != cards.Count) throw new ArgumentException("Partition must match the board.");
            Number = number;
            Cards = Array.AsReadOnly(cards.ToArray());
            var states = new CardResultState[cards.Count];
            var total = EvaluateRange(cards,answer,0,cards.Count,states);
            Correct = total.Correct; Misplaced = total.Misplaced;
            Groups = partition == null ? Array.Empty<GuessGroupFeedback>() : Array.AsReadOnly(new[] {
                EvaluateRange(cards,answer,0,partition.FirstCount,states),
                EvaluateRange(cards,answer,partition.FirstCount,partition.SecondCount,states)
            });
            // Detail hints follow the same active partition as the visible counters.
            // End-of-level answer review uses an unpartitioned evaluation as before.
            States = Array.AsReadOnly(states);
        }

        private static GuessGroupFeedback EvaluateRange(IReadOnlyList<int> cards,IReadOnlyList<int> answer,int start,int count,CardResultState[] states)
        {
            int correct = 0, misplaced = 0, end = start+count;
            var remaining = new Dictionary<int, int>();
            for (int i = start; i < end; i++)
            {
                if (cards[i] == answer[i]) { if (states != null) states[i] = CardResultState.Correct; correct++; }
                else remaining[answer[i]] = remaining.TryGetValue(answer[i], out int n) ? n + 1 : 1;
            }
            for (int i = start; i < end; i++)
            {
                if (cards[i] == answer[i]) continue;
                bool exists = remaining.TryGetValue(cards[i], out int n) && n > 0;
                if (states != null) states[i] = exists ? CardResultState.Misplaced : CardResultState.Absent;
                if (exists) { remaining[cards[i]]--; misplaced++; }
            }
            return new GuessGroupFeedback(start,count,correct,misplaced);
        }
    }

    public enum LevelMechanic { Normal = 0, LegacyElimination = 1, Helper = 2, Pair = 3, Split = 4, Growing = 5, Dynamite = 6 }

    public static class LevelMechanicRules
    {
        public static bool ShouldTrigger(LevelMechanic mechanic, GuessEvaluation guess, int remaining) =>
            mechanic == LevelMechanic.Helper && !guess.Won && remaining > 0 && guess.Number % 3 == 0;

        public static LevelMechanic Normalize(LevelMechanic mechanic) => mechanic == LevelMechanic.Helper || mechanic == LevelMechanic.Pair || mechanic == LevelMechanic.Split || mechanic == LevelMechanic.Growing || mechanic == LevelMechanic.Dynamite ? mechanic : LevelMechanic.Normal;

        public static string Description(LevelMechanic mechanic) => mechanic == LevelMechanic.Helper
            ? "Bonus İpucu"
            : mechanic == LevelMechanic.Pair ? "Ardışık Bağlı Meyveler"
            : mechanic == LevelMechanic.Dynamite ? "Dinamitli Tahta"
            : mechanic == LevelMechanic.Growing ? "Büyüyen Tahta"
            : mechanic == LevelMechanic.Split ? "İki grubun sonucu ayrı hesaplanır" : "";
    }
}

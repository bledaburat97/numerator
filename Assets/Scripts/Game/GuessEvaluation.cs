using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    // Value snapshot: subsequent board edits never change a submitted guess.
    public sealed class GuessEvaluation : EventArgs
    {
        public int Number { get; }
        public GuessSubmission Submission { get; }
        public IReadOnlyList<int> Cards { get; }
        public IReadOnlyList<CardResultState> States { get; }
        public int Correct { get; }
        public int Misplaced { get; }
        public IReadOnlyList<GuessGroupFeedback> Groups { get; }
        public bool Won => Submission.IsComplete && Correct == Submission.AnswerScope;

        public GuessEvaluation(int number, IReadOnlyList<int> cards, IReadOnlyList<int> answer, BoardPartition partition = null)
            : this(number,GuessSubmission.FullBoard(cards,answer.Count),answer,partition) { }

        public GuessEvaluation(int number,GuessSubmission submission,IReadOnlyList<int> answer,BoardPartition partition=null)
        {
            if(submission==null || answer==null || submission.AnswerScope>answer.Count)
                throw new ArgumentException("The answer must contain the submitted scope.");
            if(partition!=null && partition.BoardCount!=submission.AnswerScope)throw new ArgumentException("Partition must cover the answer scope.");
            if(answer.Take(submission.AnswerScope).Any(fruit=>fruit<1))throw new ArgumentException("An answer contains real fruit in every evaluated slot.");
            Number=number;Submission=submission;Cards=submission.Cards;
            var states=new CardResultState[submission.AnswerScope];
            var total=EvaluateRange(submission,answer,0,submission.AnswerScope,states);
            Correct=total.Correct;Misplaced=total.Misplaced;
            Groups=partition==null?Array.Empty<GuessGroupFeedback>():Array.AsReadOnly(new[]{
                EvaluateRange(submission,answer,0,partition.FirstCount,states),
                EvaluateRange(submission,answer,partition.FirstCount,partition.SecondCount,states)
            });
            States=Array.AsReadOnly(states);
        }

        private static GuessGroupFeedback EvaluateRange(GuessSubmission submission,IReadOnlyList<int> answer,int start,int count,CardResultState[] states)
        {
            var cards=submission.Cards;
            int correct = 0, misplaced = 0, end = start+count;
            var remaining = new Dictionary<int, int>();
            for (int i = start; i < end; i++)
            {
                if (cards[i] == answer[i]) { if (states != null) states[i] = CardResultState.Correct; correct++; }
                else remaining[answer[i]] = remaining.TryGetValue(answer[i], out int n) ? n + 1 : 1;
            }
            for (int i = start; i < end; i++)
            {
                if (submission.Slots[i].Kind==GuessSlotKind.Frozen)
                { if(states!=null)states[i]=CardResultState.Frozen;continue; }
                if (cards[i] == answer[i]) continue;
                bool exists = remaining.TryGetValue(cards[i], out int n) && n > 0;
                if (states != null) states[i] = exists ? CardResultState.Misplaced : CardResultState.Absent;
                if (exists) { remaining[cards[i]]--; misplaced++; }
            }
            return new GuessGroupFeedback(start,count,correct,misplaced);
        }
    }

    public enum LevelMechanic { Normal = 0, LegacyElimination = 1, Helper = 2, Pair = 3, Split = 4, Growing = 5, Dynamite = 6, Ice = 7 }

    public static class LevelMechanicRules
    {
        public static bool ShouldTrigger(LevelMechanic mechanic, GuessEvaluation guess, int remaining) =>
            mechanic == LevelMechanic.Helper && !guess.Won && remaining > 0 && guess.Number % 3 == 0;

        public static LevelMechanic Normalize(LevelMechanic mechanic) => mechanic == LevelMechanic.Helper || mechanic == LevelMechanic.Pair || mechanic == LevelMechanic.Split || mechanic == LevelMechanic.Growing || mechanic == LevelMechanic.Dynamite || mechanic == LevelMechanic.Ice ? mechanic : LevelMechanic.Normal;

        public static string Description(LevelMechanic mechanic) => mechanic == LevelMechanic.Helper
            ? "Bonus İpucu"
            : mechanic == LevelMechanic.Pair ? "Ardışık Bağlı Meyveler"
            : mechanic == LevelMechanic.Ice ? "Buzlu Tahta"
            : mechanic == LevelMechanic.Dynamite ? "Dinamitli Tahta"
            : mechanic == LevelMechanic.Growing ? "Büyüyen Tahta"
            : mechanic == LevelMechanic.Split ? "İki grubun sonucu ayrı hesaplanır" : "";
    }
}

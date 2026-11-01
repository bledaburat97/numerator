using System;
using Game;
using Zenject;

namespace Scripts
{
    // Immutable partition shared by level rules and a future inventory activation.
    public sealed class BoardPartition
    {
        public int BoardCount { get; }
        public int FirstCount => 2;
        public int SecondCount => BoardCount - FirstCount;
        private BoardPartition(int count) { BoardCount = count; }
        // Three is supported when Bomb removes a slot from a four-slot split board.
        public static BoardPartition ForCount(int count) => count >= 3 && count <= 5 ? new BoardPartition(count) : null;
    }

    public readonly struct GuessGroupFeedback
    {
        public int Start { get; }
        public int Count { get; }
        public int Correct { get; }
        public int Misplaced { get; }
        public GuessGroupFeedback(int start,int count,int correct,int misplaced)
        { Start = start; Count = count; Correct = correct; Misplaced = misplaced; }
    }

    public sealed class BoardSplitService
    {
        [InjectOptional] private ILevelSaveDataManager _save;
        [InjectOptional] private ITargetNumberCreator _target;
        [InjectOptional] private IRoundStateManager _round;
        private readonly ILevelDataCreator _levels;
        private readonly ILevelTracker _tracker;
        private BoardPartition _cached;
        public BoardSplitService(ILevelDataCreator levels,ILevelTracker tracker) { _levels = levels; _tracker = tracker; }
        public event EventHandler Applied;
        public bool IsActive => _tracker.GetGameOption() == GameOption.SinglePlayer
            && (_levels.GetLevelData()?.Mechanic == LevelMechanic.Split || (_save?.GetLevelSaveData().BoardSplitApplied ?? false));
        public bool CanApply
        {
            get
            {
                if (_tracker.GetGameOption() != GameOption.SinglePlayer || IsActive || _save == null || _target == null || _round == null
                    || _round.GetRemainingGuessCount() <= 0) return false;
                var answer = _target.GetTargetCardsList();
                if (answer == null || answer.Count < 4 || answer.Count > 5) return false;
                var history = _round.GetTriedCardsList();
                return history.Count == 0 || !new GuessEvaluation(history.Count,history[history.Count-1],_target.GetHistoryTargetCards()).Won;
            }
        }

        public bool TryApply(Func<bool> consumeInventory)
        {
            if (!CanApply || (consumeInventory != null && !consumeInventory())) return false;
            _save.GetLevelSaveData().BoardSplitApplied = true;
            Applied?.Invoke(this,EventArgs.Empty);
            return true;
        }

        public BoardPartition PartitionFor(int count)
        {
            if (!IsActive) return null;
            if (_cached == null || _cached.BoardCount != count) _cached = BoardPartition.ForCount(count);
            return _cached;
        }

        public GuessEvaluation Evaluate(int number,System.Collections.Generic.IReadOnlyList<int> cards,System.Collections.Generic.IReadOnlyList<int> answer)
            => new GuessEvaluation(number,cards,answer,PartitionFor(cards.Count));
    }
}

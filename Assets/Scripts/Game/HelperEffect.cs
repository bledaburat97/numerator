using System;
using Game;
using Zenject;

namespace Scripts
{
    // Both the inventory item and the level rule call this service.
    public sealed class HelperEffect
    {
        [InjectOptional] private BoardSplitService _split;
        private readonly IRoundStateManager _round;
        private readonly ITargetNumberCreator _target;
        private readonly ILevelSaveDataManager _save;
        public event EventHandler<GuessEvaluation> Applied;

        public HelperEffect(IRoundStateManager round, ITargetNumberCreator target, ILevelSaveDataManager save)
        { _round = round; _target = target; _save = save; }

        public bool CanApply => _round.GetRemainingGuessCount() > 0 && _round.GetTriedCardsList().Count > 0
            && !_save.GetLevelSaveData().RevealedGuessNumbers.Contains(_round.GetTriedCardsList().Count)
            && !Latest().Won;

        private GuessEvaluation Latest()
            => EvaluateGuess(_round.GetTriedCardsList().Count);

        public GuessEvaluation EvaluateGuess(int number)
        {
            var history = _round.GetTriedCardsList();
            return _split != null ? _split.Evaluate(number,history[number-1],_target.GetHistoryTargetCards())
                : new GuessEvaluation(number,GuessSubmission.FromHistory(history[number-1]),_target.GetHistoryTargetCards());
        }

        public bool TryApply(System.Func<bool> consumeInventory=null)
        {
            if (!CanApply) return false;
            var guess = Latest();
            if(consumeInventory!=null && !consumeInventory())return false;
            _save.GetLevelSaveData().RevealedGuessNumbers.Add(guess.Number);
            Applied?.Invoke(this, guess);
            return true;
        }
    }
}

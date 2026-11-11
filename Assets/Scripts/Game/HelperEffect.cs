using System;
using System.Linq;
using System.Collections.Generic;
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

        private GuessEvaluation Prepare()=>Plan(_round.GetRemainingGuessCount(),_round.GetTriedCardsList(),
            _save.GetLevelSaveData().RevealedGuessNumbers,_target.GetHistoryTargetCards(),_split?.IsActive??false);
        public bool CanApply=>Prepare()!=null;
        public static GuessEvaluation Plan(int remaining,IReadOnlyList<List<int>> history,IReadOnlyCollection<int> revealed,IReadOnlyList<int> answer,bool split)
        {
            if(remaining<=0 || history==null || history.Count==0 || revealed.Contains(history.Count))return null;
            var submission=GuessSubmission.FromHistory(history[history.Count-1]);
            var guess=new GuessEvaluation(history.Count,submission,answer,split?BoardPartition.ForCount(submission.AnswerScope):null);
            return guess.Won?null:guess;
        }
        public static bool Record(LevelSaveData state,int number)
        {
            if(state.RevealedGuessNumbers.Contains(number))return false;
            state.RevealedGuessNumbers.Add(number);return true;
        }
        public bool ApplyCommitted(GuessEvaluation inspection)
        {
            if(inspection==null)return false;
            if(Record(_save.GetLevelSaveData(),inspection.Number))Applied?.Invoke(this,inspection);
            return true;
        }

        public GuessEvaluation EvaluateGuess(int number)
        {
            var history = _round.GetTriedCardsList();
            return _split != null ? _split.Evaluate(number,history[number-1],_target.GetHistoryTargetCards())
                : new GuessEvaluation(number,GuessSubmission.FromHistory(history[number-1]),_target.GetHistoryTargetCards());
        }

        public bool TryApply()
        {
            var prepared=Prepare();return prepared!=null && ApplyCommitted(prepared);
        }
    }
}

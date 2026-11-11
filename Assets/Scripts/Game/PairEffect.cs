using System;
using System.Collections.Generic;
using System.Linq;
using Game;

namespace Scripts
{
    // Contains an unordered adjacency relationship, never an answer direction or position.
    public sealed class PairClue : EventArgs
    {
        public int First { get; }
        public int Second { get; }

        // Canonical card IDs reveal no answer direction, including when old saves are loaded.
        public PairClue(int first, int second) { First = Math.Min(first, second); Second = Math.Max(first, second); }

        public bool Matches(IReadOnlyList<int> answer)
        {
            for (int i = 1; i < answer.Count; i++)
                if ((answer[i - 1] == First && answer[i] == Second) || (answer[i - 1] == Second && answer[i] == First)) return true;
            return false;
        }
    }

    // One application per round. The level rule and inventory item share this effect.
    public sealed class PairEffect
    {
        private readonly IRoundStateManager _round;
        private readonly ITargetNumberCreator _target;
        private readonly ILevelSaveDataManager _save;
        private readonly ICardItemInfoManager _info;
        private readonly ILevelTracker _tracker;
        public event EventHandler<PairClue> Changed;
        public PairClue Current => _save.GetLevelSaveData().PairClue;

        public PairEffect(IRoundStateManager round, ITargetNumberCreator target, ILevelSaveDataManager save,
            ICardItemInfoManager info, ILevelTracker tracker)
        { _round = round; _target = target; _save = save; _info = info; _tracker = tracker; }

        public bool CanApply => IsPlayable && !_save.GetLevelSaveData().PairWasApplied && Current == null
            && _target.GetTargetCardsList().Count >= 2 && Candidates().Count > 0;

        private bool IsPlayable
        {
            get
            {
                if (_tracker.GetGameOption() != GameOption.SinglePlayer || _round.GetRemainingGuessCount() <= 0) return false;
                var answer = _target.GetTargetCardsList();
                if (answer == null) return false;
                var history = _round.GetTriedCardsList();
                return history.Count == 0 || !history[history.Count - 1].SequenceEqual(answer);
            }
        }

        private List<PairClue> Candidates()
        {
            var answer = _target.GetTargetCardsList();
            return Candidates(answer,_info.GetCardItemInfoList());
        }

        public static List<PairClue> Candidates(IReadOnlyList<int> answer,IReadOnlyList<CardItemInfo> info)
        {
            var pairs = new List<PairClue>();
            for (int i = 1; i < answer.Count; i++)
            {
                bool Locked(int fruit) => info != null && fruit > 0 && fruit <= info.Count && info[fruit - 1].isLocked;
                if (!Locked(answer[i - 1]) || !Locked(answer[i])) pairs.Add(new PairClue(answer[i - 1], answer[i]));
            }
            return pairs;
        }

        public static PairClue SelectClue(IReadOnlyList<int> answer,IReadOnlyList<CardItemInfo> info)
        {
            var candidates=Candidates(answer,info);
            return candidates.Count>0?candidates[UnityEngine.Random.Range(0,candidates.Count)]:null;
        }

        public bool ApplyClue(PairClue selected)
        {
            if(!CanApply || selected==null || !selected.Matches(_target.GetTargetCardsList()))return false;
            var state=_save.GetLevelSaveData();state.PairWasApplied=true;state.PairClue=selected;
            Changed?.Invoke(this,selected);return true;
        }

        public bool TryApply()
        {
            if (!CanApply) return false;
            var candidates = Candidates();
            var selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return ApplyClue(selected);
        }

        public bool Prepare(bool pairLevel)
        {
            Reconcile();
            return pairLevel && TryApply();
        }

        public void RestoreClue(PairClue clue)
        {
            var state=_save.GetLevelSaveData();state.PairClue=clue;
            if(clue!=null)state.PairWasApplied=true;
            Changed?.Invoke(this,clue);
        }

        // Bomb changes the answer before the first guess. Never leave a stale fruit in the clue.
        public void Reconcile()
        {
            if (_tracker.GetGameOption() != GameOption.SinglePlayer) return;
            var state = _save.GetLevelSaveData();
            if (state.PairClue != null)
            {
                state.PairWasApplied = true;
                var answer = _target.GetTargetCardsList();
                if (!state.PairClue.Matches(answer))
                {
                    var candidates = Candidates();
                    // An already-earned clue remains valid even if Bomb left only two slots.
                    state.PairClue = candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
                }
            }
            Changed?.Invoke(this, state.PairClue);
        }
    }
}

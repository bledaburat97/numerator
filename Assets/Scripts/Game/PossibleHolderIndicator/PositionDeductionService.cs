using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    public enum FruitPresence { Unknown, Present, Absent }
    public enum PositionDeductionKind
    {
        NoCorrect, NoMisplaced, NoMatches, CompleteSet, AllPositionsEliminated,
        OnlyPosition, OnlyCandidate, Occupied, FixedElsewhere, RevealedPosition, EliminatedFruit,
        DetailedCorrect, DetailedMisplaced, DetailedAbsent, Manual
    }

    public sealed class PositionDeductionReason
    {
        public PositionDeductionKind Kind { get; }
        public int Slot { get; }
        public IReadOnlyList<int> Guesses { get; }
        public PositionDeductionReason(PositionDeductionKind kind, int slot = -1, int guess = 0,
            IEnumerable<PositionDeductionReason> dependencies = null)
        {
            Kind = kind; Slot = slot;
            var sources = new SortedSet<int>();
            if (guess > 0) sources.Add(guess);
            if (dependencies != null)
                foreach (var reason in dependencies)
                    if (reason != null) foreach (int number in reason.Guesses) sources.Add(number);
            Guesses = Array.AsReadOnly(sources.ToArray());
        }
    }

    // Public feedback only: this class never receives an answer, Unity object or hidden per-card evaluation.
    // Fruit IDs are 1-based, slot indexes are 0-based. Inferences are notes, never placement locks.
    public sealed class PositionDeductionService
    {
        private readonly bool[,] _possible;
        private readonly PositionDeductionReason[,] _exclusions;
        private readonly FruitPresence[] _presence;
        private readonly PositionDeductionReason[] _presenceReasons, _fixedReasons;
        private readonly int[] _fixedSlots;
        public int FruitCount { get; }
        public int SlotCount { get; }
        public bool IsConsistent { get; private set; } = true;
        public bool HasManualChanges { get; private set; }

        public PositionDeductionService(int fruits, int slots)
        {
            if (slots < 1 || slots > 5 || fruits < slots || fruits > 9) throw new ArgumentOutOfRangeException();
            FruitCount = fruits; SlotCount = slots;
            _possible = new bool[fruits, slots]; _exclusions = new PositionDeductionReason[fruits, slots];
            _presence = new FruitPresence[fruits]; _presenceReasons = new PositionDeductionReason[fruits];
            _fixedReasons = new PositionDeductionReason[fruits]; _fixedSlots = new int[fruits];
            for (int fruit = 0; fruit < fruits; fruit++)
            {
                _fixedSlots[fruit] = -1;
                for (int slot = 0; slot < slots; slot++) _possible[fruit, slot] = true;
            }
        }

        public bool IsPossible(int fruit, int slot) => _possible[fruit - 1, slot];
        public FruitPresence Presence(int fruit) => _presence[fruit - 1];
        public int FixedSlot(int fruit) => _fixedSlots[fruit - 1];
        public PositionDeductionReason Reason(int fruit, int slot) => _fixedSlots[fruit - 1] == slot
            ? _fixedReasons[fruit - 1] : _exclusions[fruit - 1, slot];

        public bool ApplyGuess(int number, IReadOnlyList<int> cards, int correct, int misplaced)
        {
            if ((!IsConsistent && !HasManualChanges) || number < 1 || cards == null || cards.Count != SlotCount || correct < 0 || misplaced < 0
                || correct + misplaced > SlotCount || cards.Distinct().Count() != cards.Count
                || cards.Any(fruit => fruit < 1 || fruit > FruitCount)) return false;
            if (correct == 0 && misplaced == 0)
            {
                var reason = new PositionDeductionReason(PositionDeductionKind.NoMatches, guess: number);
                foreach (int fruit in cards) EliminateFruit(fruit - 1, reason);
            }
            else
            {
                if (correct == 0)
                {
                    var reason = new PositionDeductionReason(PositionDeductionKind.NoCorrect, guess: number);
                    for (int slot = 0; slot < SlotCount; slot++) Exclude(cards[slot] - 1, slot, reason);
                }
                if (misplaced == 0)
                {
                    var reason = new PositionDeductionReason(PositionDeductionKind.NoMisplaced, guess: number);
                    for (int slot = 0; slot < SlotCount; slot++)
                        for (int other = 0; other < SlotCount; other++)
                            if (other != slot) Exclude(cards[slot] - 1, other, reason);
                }
            }
            if (correct + misplaced == SlotCount)
            {
                var reason = new PositionDeductionReason(PositionDeductionKind.CompleteSet, guess: number);
                for (int fruit = 0; fruit < FruitCount; fruit++)
                    if (cards.Contains(fruit + 1)) MarkPresent(fruit, reason); else EliminateFruit(fruit, reason);
            }
            Propagate(); return true;
        }

        public void TogglePosition(int fruit, int slot)
        {
            if (fruit < 1 || fruit > FruitCount || slot < 0 || slot >= SlotCount) return;
            HasManualChanges = true;
            int index = fruit - 1;
            _possible[index,slot] = !_possible[index,slot];
            _exclusions[index,slot] = _possible[index,slot] ? null : new PositionDeductionReason(PositionDeductionKind.Manual,slot);
            _fixedSlots[index] = -1; _fixedReasons[index] = null;
            RefreshEditedPresence(index);
            // Editing changes only the requested note. Previously derived notes stay put.
        }

        public void SetManualPresence(int fruit, FruitPresence presence)
        {
            if (fruit < 1 || fruit > FruitCount) return;
            HasManualChanges = true; int index = fruit - 1;
            var reason = new PositionDeductionReason(PositionDeductionKind.Manual);
            bool reopen = presence == FruitPresence.Unknown || _presence[index] == FruitPresence.Absent;
            for (int slot = 0; slot < SlotCount; slot++)
                if (presence == FruitPresence.Absent || reopen)
                { _possible[index,slot] = presence != FruitPresence.Absent; _exclusions[index,slot] = presence == FruitPresence.Absent ? reason : null; }
            _presence[index] = presence; _presenceReasons[index] = reason;
            _fixedSlots[index] = -1; _fixedReasons[index] = null;
        }

        private void RefreshEditedPresence(int fruit)
        {
            bool any = false;
            for (int slot = 0; slot < SlotCount; slot++) any |= _possible[fruit,slot];
            if (!any) _presence[fruit] = FruitPresence.Absent;
            else if (_presence[fruit] == FruitPresence.Absent) _presence[fruit] = FruitPresence.Unknown;
            _presenceReasons[fruit] = new PositionDeductionReason(PositionDeductionKind.Manual);
        }

        public PositionNotesSnapshot Capture(int historyCount, bool automaticEnabled)
        {
            var snapshot = new PositionNotesSnapshot {
                Fruits=FruitCount, Slots=SlotCount, HistoryCount=historyCount, AutomaticEnabled=automaticEnabled,
                Manual=HasManualChanges, Consistent=IsConsistent, Possible=new bool[FruitCount*SlotCount],
                Presence=(FruitPresence[])_presence.Clone(), Fixed=(int[])_fixedSlots.Clone(),
                Exclusions=new PositionNoteReason[FruitCount*SlotCount],
                PresenceReasons=_presenceReasons.Select(PositionNoteReason.From).ToArray(), FixedReasons=_fixedReasons.Select(PositionNoteReason.From).ToArray()
            };
            for(int fruit=0;fruit<FruitCount;fruit++)for(int slot=0;slot<SlotCount;slot++)
            {int i=fruit*SlotCount+slot;snapshot.Possible[i]=_possible[fruit,slot];snapshot.Exclusions[i]=PositionNoteReason.From(_exclusions[fruit,slot]);}
            return snapshot;
        }

        public static PositionDeductionService Restore(PositionNotesSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsValid()) return null;
            var state=new PositionDeductionService(snapshot.Fruits,snapshot.Slots) {HasManualChanges=snapshot.Manual,IsConsistent=snapshot.Consistent};
            for(int fruit=0;fruit<state.FruitCount;fruit++)
            {
                state._presence[fruit]=snapshot.Presence[fruit]; state._fixedSlots[fruit]=snapshot.Fixed[fruit];
                state._presenceReasons[fruit]=snapshot.PresenceReasons?[fruit]?.ToReason();state._fixedReasons[fruit]=snapshot.FixedReasons?[fruit]?.ToReason();
                for(int slot=0;slot<state.SlotCount;slot++)
                {int i=fruit*state.SlotCount+slot;state._possible[fruit,slot]=snapshot.Possible[i];state._exclusions[fruit,slot]=snapshot.Exclusions?[i]?.ToReason();}
            }
            return state;
        }

        public PositionDeductionService Resize(int slots)
        {
            var resized=new PositionDeductionService(FruitCount,slots) {HasManualChanges=HasManualChanges};
            for(int fruit=0;fruit<FruitCount;fruit++)
            {
                resized._presence[fruit]=_presence[fruit];resized._presenceReasons[fruit]=_presenceReasons[fruit];
                for(int slot=0;slot<Math.Min(slots,SlotCount);slot++)
                {resized._possible[fruit,slot]=_possible[fruit,slot];resized._exclusions[fruit,slot]=_exclusions[fruit,slot];}
                if(_fixedSlots[fruit]>=0 && _fixedSlots[fruit]<slots)
                {resized._fixedSlots[fruit]=_fixedSlots[fruit];resized._fixedReasons[fruit]=_fixedReasons[fruit];}
                else resized.RefreshEditedPresence(fruit);
                if(slots>SlotCount && resized._presence[fruit]==FruitPresence.Absent)resized._presence[fruit]=FruitPresence.Unknown;
            }
            return resized;
        }

        public void RevealPosition(int fruit, int slot)
        {
            if (fruit < 1 || fruit > FruitCount || slot < 0 || slot >= SlotCount) return;
            AssignVisiblePosition(fruit - 1, slot, new PositionDeductionReason(PositionDeductionKind.RevealedPosition, slot)); Propagate();
        }
        public void RevealAbsent(int fruit)
        {
            if (fruit < 1 || fruit > FruitCount) return;
            EliminateFruit(fruit - 1, new PositionDeductionReason(PositionDeductionKind.EliminatedFruit)); Propagate();
        }
        public void ApplyVisibleDetail(int number, int fruit, int slot, CardResultState detail)
        {
            if (fruit < 1 || fruit > FruitCount || slot < 0 || slot >= SlotCount) return;
            if (detail == CardResultState.Correct)
                AssignVisiblePosition(fruit - 1, slot, new PositionDeductionReason(PositionDeductionKind.DetailedCorrect, slot, number));
            else if (detail == CardResultState.Misplaced)
            {
                var reason = new PositionDeductionReason(PositionDeductionKind.DetailedMisplaced, slot, number);
                MarkPresent(fruit - 1, reason); Exclude(fruit - 1, slot, reason);
            }
            else if (detail == CardResultState.Absent)
                EliminateFruit(fruit - 1, new PositionDeductionReason(PositionDeductionKind.DetailedAbsent, slot, number));
            Propagate();
        }

        private void AssignVisiblePosition(int fruit,int slot,PositionDeductionReason reason)
        {
            if(HasManualChanges)
            {
                // A genuinely revealed position supersedes a conflicting pencil mark.
                _possible[fruit,slot]=true;_exclusions[fruit,slot]=null;
                if(_presence[fruit]==FruitPresence.Absent)_presence[fruit]=FruitPresence.Unknown;
                _fixedSlots[fruit]=-1;_fixedReasons[fruit]=null;
            }
            Assign(fruit,slot,reason);
        }

        private bool Exclude(int fruit, int slot, PositionDeductionReason reason)
        {
            if (!_possible[fruit, slot]) return false;
            if (_fixedSlots[fruit] == slot) IsConsistent = false;
            if (HasManualChanges && _fixedSlots[fruit] == slot) { _fixedSlots[fruit] = -1; _fixedReasons[fruit] = null; }
            _possible[fruit, slot] = false; _exclusions[fruit, slot] = reason; return true;
        }
        private bool MarkPresent(int fruit, PositionDeductionReason reason)
        {
            if (_presence[fruit] == FruitPresence.Present) return false;
            if (_presence[fruit] == FruitPresence.Absent) { IsConsistent = false; if (HasManualChanges) return false; }
            _presence[fruit] = FruitPresence.Present; _presenceReasons[fruit] = reason; return true;
        }
        private bool EliminateFruit(int fruit, PositionDeductionReason reason)
        {
            bool changed = false;
            for (int slot = 0; slot < SlotCount; slot++) changed |= Exclude(fruit, slot, reason);
            if (_presence[fruit] == FruitPresence.Present) IsConsistent = false;
            if (HasManualChanges) { _fixedSlots[fruit] = -1; _fixedReasons[fruit] = null; }
            if (_presence[fruit] != FruitPresence.Absent)
            { _presence[fruit] = FruitPresence.Absent; _presenceReasons[fruit] = reason; changed = true; }
            return changed;
        }
        private bool Assign(int fruit, int slot, PositionDeductionReason reason)
        {
            if (!_possible[fruit, slot] || (_fixedSlots[fruit] >= 0 && _fixedSlots[fruit] != slot)) { IsConsistent = false; return false; }
            bool changed = MarkPresent(fruit, reason);
            if (_fixedSlots[fruit] < 0) { _fixedSlots[fruit] = slot; _fixedReasons[fruit] = reason; changed = true; }
            var elsewhere = new PositionDeductionReason(PositionDeductionKind.FixedElsewhere, slot, dependencies: new[] { reason });
            var occupied = new PositionDeductionReason(PositionDeductionKind.Occupied, slot, dependencies: new[] { reason });
            for (int other = 0; other < SlotCount; other++) if (other != slot) changed |= Exclude(fruit, other, elsewhere);
            for (int other = 0; other < FruitCount; other++) if (other != fruit) changed |= Exclude(other, slot, occupied);
            return changed;
        }
        private void Propagate()
        {
            bool changed;
            do
            {
                changed = false;
                for (int fruit = 0; fruit < FruitCount && (IsConsistent || HasManualChanges); fruit++)
                {
                    int count = 0, remaining = -1;
                    var reasons = new List<PositionDeductionReason> { _presenceReasons[fruit] };
                    for (int slot = 0; slot < SlotCount; slot++)
                        if (_possible[fruit, slot]) { count++; remaining = slot; } else reasons.Add(_exclusions[fruit, slot]);
                    if (count == 0) changed |= EliminateFruit(fruit, new PositionDeductionReason(PositionDeductionKind.AllPositionsEliminated, dependencies: reasons));
                    else if (count == 1 && _presence[fruit] == FruitPresence.Present && _fixedSlots[fruit] < 0)
                        changed |= Assign(fruit, remaining, new PositionDeductionReason(PositionDeductionKind.OnlyPosition, remaining, dependencies: reasons));
                }
                for (int slot = 0; slot < SlotCount && (IsConsistent || HasManualChanges); slot++)
                {
                    int count = 0, remaining = -1;
                    var reasons = new List<PositionDeductionReason>();
                    for (int fruit = 0; fruit < FruitCount; fruit++)
                        if (_possible[fruit, slot]) { count++; remaining = fruit; } else reasons.Add(_exclusions[fruit, slot]);
                    if (count == 0) IsConsistent = false;
                    else if (count == 1 && _fixedSlots[remaining] < 0)
                        changed |= Assign(remaining, slot, new PositionDeductionReason(PositionDeductionKind.OnlyCandidate, slot, dependencies: reasons));
                }
            } while (changed && (IsConsistent || HasManualChanges));
        }
    }

    // A single visible notebook, including sticky consequences of manual changes.
    public sealed class PositionNotesSnapshot
    {
        public int Fruits,Slots,HistoryCount;
        public bool AutomaticEnabled,Manual,Edited,IndicatorsVisible,Consistent=true;
        public bool[] Possible;
        public FruitPresence[] Presence;
        public int[] Fixed;
        public PositionNoteReason[] Exclusions,PresenceReasons,FixedReasons;
        public bool IsValid() => Slots>=1 && Slots<=5 && Fruits>=Slots && Fruits<=9 && HistoryCount>=0
            && Possible?.Length==Fruits*Slots && Presence?.Length==Fruits && Fixed?.Length==Fruits
            && (Exclusions==null || Exclusions.Length==Fruits*Slots) && (PresenceReasons==null || PresenceReasons.Length==Fruits)
            && (FixedReasons==null || FixedReasons.Length==Fruits) && Fixed.All(slot=>slot>=-1 && slot<Slots);
        public PositionNotesSnapshot Copy()
        {
            var copy=PositionDeductionService.Restore(this)?.Capture(HistoryCount,AutomaticEnabled);
            if(copy!=null){copy.Edited=Edited;copy.IndicatorsVisible=IndicatorsVisible;}
            return copy;
        }
    }
    public sealed class PositionNoteReason
    {
        public PositionDeductionKind Kind; public int Slot; public int[] Guesses;
        public static PositionNoteReason From(PositionDeductionReason reason)=>reason==null?null:new PositionNoteReason {Kind=reason.Kind,Slot=reason.Slot,Guesses=reason.Guesses.ToArray()};
        public PositionDeductionReason ToReason()=>new PositionDeductionReason(Kind,Slot,dependencies:Guesses?.Select(guess=>new PositionDeductionReason(Kind,guess:guess)));
    }
}

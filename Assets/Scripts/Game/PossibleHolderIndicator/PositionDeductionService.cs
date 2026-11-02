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
        DetailedCorrect, DetailedMisplaced, DetailedAbsent
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
            if (!IsConsistent || number < 1 || cards == null || cards.Count != SlotCount || correct < 0 || misplaced < 0
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

        public void RevealPosition(int fruit, int slot)
        {
            if (fruit < 1 || fruit > FruitCount || slot < 0 || slot >= SlotCount) return;
            Assign(fruit - 1, slot, new PositionDeductionReason(PositionDeductionKind.RevealedPosition, slot)); Propagate();
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
                Assign(fruit - 1, slot, new PositionDeductionReason(PositionDeductionKind.DetailedCorrect, slot, number));
            else if (detail == CardResultState.Misplaced)
            {
                var reason = new PositionDeductionReason(PositionDeductionKind.DetailedMisplaced, slot, number);
                MarkPresent(fruit - 1, reason); Exclude(fruit - 1, slot, reason);
            }
            else if (detail == CardResultState.Absent)
                EliminateFruit(fruit - 1, new PositionDeductionReason(PositionDeductionKind.DetailedAbsent, slot, number));
            Propagate();
        }

        private bool Exclude(int fruit, int slot, PositionDeductionReason reason)
        {
            if (!_possible[fruit, slot]) return false;
            if (_fixedSlots[fruit] == slot) IsConsistent = false;
            _possible[fruit, slot] = false; _exclusions[fruit, slot] = reason; return true;
        }
        private bool MarkPresent(int fruit, PositionDeductionReason reason)
        {
            if (_presence[fruit] == FruitPresence.Present) return false;
            if (_presence[fruit] == FruitPresence.Absent) IsConsistent = false;
            _presence[fruit] = FruitPresence.Present; _presenceReasons[fruit] = reason; return true;
        }
        private bool EliminateFruit(int fruit, PositionDeductionReason reason)
        {
            bool changed = false;
            for (int slot = 0; slot < SlotCount; slot++) changed |= Exclude(fruit, slot, reason);
            if (_presence[fruit] == FruitPresence.Present) IsConsistent = false;
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
                for (int fruit = 0; fruit < FruitCount && IsConsistent; fruit++)
                {
                    int count = 0, remaining = -1;
                    var reasons = new List<PositionDeductionReason> { _presenceReasons[fruit] };
                    for (int slot = 0; slot < SlotCount; slot++)
                        if (_possible[fruit, slot]) { count++; remaining = slot; } else reasons.Add(_exclusions[fruit, slot]);
                    if (count == 0) changed |= EliminateFruit(fruit, new PositionDeductionReason(PositionDeductionKind.AllPositionsEliminated, dependencies: reasons));
                    else if (count == 1 && _presence[fruit] == FruitPresence.Present && _fixedSlots[fruit] < 0)
                        changed |= Assign(fruit, remaining, new PositionDeductionReason(PositionDeductionKind.OnlyPosition, remaining, dependencies: reasons));
                }
                for (int slot = 0; slot < SlotCount && IsConsistent; slot++)
                {
                    int count = 0, remaining = -1;
                    var reasons = new List<PositionDeductionReason>();
                    for (int fruit = 0; fruit < FruitCount; fruit++)
                        if (_possible[fruit, slot]) { count++; remaining = fruit; } else reasons.Add(_exclusions[fruit, slot]);
                    if (count == 0) IsConsistent = false;
                    else if (count == 1 && _fixedSlots[remaining] < 0)
                        changed |= Assign(remaining, slot, new PositionDeductionReason(PositionDeductionKind.OnlyCandidate, slot, dependencies: reasons));
                }
            } while (changed && IsConsistent);
        }
    }
}

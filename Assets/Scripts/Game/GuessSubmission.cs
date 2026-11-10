using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    public enum GuessSlotKind { Fruit, Frozen }

    public readonly struct GuessSlot
    {
        public GuessSlotKind Kind { get; }
        public int? Fruit { get; }
        private GuessSlot(GuessSlotKind kind,int? fruit){Kind=kind;Fruit=fruit;}
        public static GuessSlot Filled(int fruit)
        {if(fruit<1)throw new ArgumentOutOfRangeException(nameof(fruit));return new GuessSlot(GuessSlotKind.Fruit,fruit);}
        public static GuessSlot Frozen => new GuessSlot(GuessSlotKind.Frozen,null);
    }

    // The answer scope and a non-enterable slot are different concepts. A Growing
    // history row has a shorter scope. An iced row retains its whole answer scope.
    public sealed class GuessSubmission
    {
        public const int FrozenMarker=0; // Compact on-disk history encoding only.
        public int AnswerScope { get; }
        public IReadOnlyList<GuessSlot> Slots { get; }
        public IReadOnlyList<int> Cards { get; }
        public int EnteredCount { get; }
        public bool IsComplete => EnteredCount==AnswerScope;
        private GuessSubmission(IEnumerable<GuessSlot> slots,int scope)
        {
            var items=slots?.ToArray() ?? throw new ArgumentNullException(nameof(slots));
            if(scope<1 || scope>5 || items.Length!=scope)throw new ArgumentException("Guess slots must cover the explicit answer scope.");
            AnswerScope=scope;Slots=Array.AsReadOnly(items);
            Cards=Array.AsReadOnly(items.Select(slot=>slot.Fruit??FrozenMarker).ToArray());
            EnteredCount=items.Count(slot=>slot.Kind==GuessSlotKind.Fruit);
        }
        public static GuessSubmission FullBoard(IReadOnlyList<int> cards,int answerCount)
            =>new GuessSubmission(cards.Select(GuessSlot.Filled),answerCount);
        public static GuessSubmission FrozenBoard(IReadOnlyList<int> cards,int answerCount,int frozenIndex)
        {
            if(cards==null || cards.Count!=answerCount || frozenIndex<0 || frozenIndex>=answerCount || cards[frozenIndex]!=FrozenMarker)
                throw new ArgumentException("A frozen guess retains the complete board width and an explicit frozen slot.");
            return new GuessSubmission(cards.Select((fruit,index)=>index==frozenIndex?GuessSlot.Frozen:GuessSlot.Filled(fruit)),answerCount);
        }
        // Each persisted row is its own immutable scope snapshot. Prefix rows
        // therefore stay short when a Growing board expands later.
        public static GuessSubmission FromHistory(IReadOnlyList<int> cards)
        {
            if(cards==null)throw new ArgumentNullException(nameof(cards));
            return new GuessSubmission(cards.Select(fruit=>fruit==FrozenMarker?GuessSlot.Frozen:GuessSlot.Filled(fruit)),cards.Count);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Game;

namespace Scripts
{
    public readonly struct PowerUpUseRequest
    {
        public RewardType Type { get; }
        public int BoardIndex { get; }
        public PowerUpUseRequest(RewardType type,int boardIndex=-1){Type=type;BoardIndex=boardIndex;}
    }

    // The effect prepares a checkpoint; it never spends stock or changes scene objects.
    public interface IPowerUpEffect
    {
        RewardType Type { get; }
        bool CanUse(PowerUpContext context,int boardIndex=-1);
        bool TryApply(PowerUpContext context,LevelSaveData checkpoint,int boardIndex=-1);
    }

    public sealed class PowerUpContext
    {
        public LevelSaveData State;
        public IReadOnlyList<int> Answer;
        public IReadOnlyList<List<int>> History;
        public IReadOnlyList<CardItemInfo> Cards;
        public int Remaining,Capacity,Slots,Removed,FrozenIndex=-1;
        public bool CanShrink;
        public LevelMechanic Mechanic;
        public bool SplitActive=>Mechanic==LevelMechanic.Split || State.BoardSplitApplied;
    }

    public static class PowerUpEffects
    {
        private static readonly IPowerUpEffect[] All={new LifeEffect(),new BombEffect(),new RevealEffect(),new InspectEffect(),new AdjacencyEffect(),new PartitionEffect(),new AutomaticNotesEffect()};
        public static IPowerUpEffect Get(RewardType type)=>All.FirstOrDefault(effect=>effect.Type==type);

        private abstract class Effect : IPowerUpEffect
        {
            public abstract RewardType Type {get;}
            public abstract bool CanUse(PowerUpContext context,int boardIndex=-1);
            public bool TryApply(PowerUpContext context,LevelSaveData checkpoint,int boardIndex=-1)
            {if(!CanUse(context,boardIndex))return false;Apply(context,checkpoint,boardIndex);return true;}
            protected abstract void Apply(PowerUpContext context,LevelSaveData checkpoint,int boardIndex);
        }
        private sealed class LifeEffect : Effect
        {
            public override RewardType Type=>RewardType.Life;
            public override bool CanUse(PowerUpContext c,int slot=-1)=>c.Remaining>0 && c.Capacity-c.Remaining>=PowerUpDefinition.Get(Type).ExtraGuesses;
            protected override void Apply(PowerUpContext c,LevelSaveData data,int slot)=>data.RemainingGuessCount+=PowerUpDefinition.Get(Type).ExtraGuesses;
        }
        private sealed class BombEffect : Effect
        {
            public override RewardType Type=>RewardType.Bomb;
            public override bool CanUse(PowerUpContext c,int slot=-1)=>c.CanShrink && c.Slots>1 && c.Removed==0 && c.History.Count==0
                && c.Answer?.Count==c.Slots && c.Cards.All(card=>!card.isLocked);
            protected override void Apply(PowerUpContext c,LevelSaveData data,int slot)
            {
                data.TargetCards.RemoveAt(data.TargetCards.Count-1);data.RemovedBoardHolderCount++;
                foreach(var card in data.CardItemInfoList)card.possibleCardHolderIndicatorIndexes.RemoveAll(index=>index>=data.TargetCards.Count);
                if(data.Dynamite!=null && (data.Dynamite.BoardIndex<0 || data.Dynamite.BoardIndex>=data.TargetCards.Count))
                    data.Dynamite.BoardIndex=UnityEngine.Random.Range(0,data.TargetCards.Count);
                if(data.PairClue!=null && !data.PairClue.Matches(data.TargetCards))
                    data.PairClue=PairEffect.SelectClue(data.TargetCards,data.CardItemInfoList);
            }
        }
        private sealed class RevealEffect : Effect
        {
            public override RewardType Type=>RewardType.Revealing;
            public override bool CanUse(PowerUpContext c,int slot=-1)
            {
                if(c.Answer==null)return false;
                if(slot<0)return Enumerable.Range(0,c.Answer.Count).Any(index=>CanUse(c,index));
                if(slot>=c.Answer.Count || slot==c.FrozenIndex)return false;
                int fruit=c.Answer[slot]-1;
                return fruit>=0 && fruit<c.Cards.Count && c.Cards[fruit].isExisted && !c.Cards[fruit].isLocked;
            }
            protected override void Apply(PowerUpContext c,LevelSaveData data,int slot)
            {
                var card=data.CardItemInfoList[data.TargetCards[slot]-1];
                card.isLocked=true;card.probabilityType=ProbabilityType.Certain;card.possibleCardHolderIndicatorIndexes=new List<int>{slot};
                if(data.Dynamite?.BoardIndex==slot)data.Dynamite.Disarmed=true;
            }
        }
        private sealed class InspectEffect : Effect
        {
            public override RewardType Type=>RewardType.Helper;
            public override bool CanUse(PowerUpContext c,int slot=-1)=>c.History.Count>0 && !c.State.RevealedGuessNumbers.Contains(c.History.Count);
            protected override void Apply(PowerUpContext c,LevelSaveData data,int slot)=>data.RevealedGuessNumbers.Add(c.History.Count);
        }
        private sealed class AdjacencyEffect : Effect
        {
            public override RewardType Type=>RewardType.Pair;
            public override bool CanUse(PowerUpContext c,int slot=-1)=>c.Mechanic!=LevelMechanic.Pair && !c.State.PairWasApplied && c.State.PairClue==null
                && c.Answer!=null && PairEffect.Candidates(c.Answer,c.Cards).Count>0;
            protected override void Apply(PowerUpContext c,LevelSaveData data,int slot)
            {data.PairWasApplied=true;data.PairClue=PairEffect.SelectClue(c.Answer,c.Cards);}
        }
        private sealed class PartitionEffect : Effect
        {
            public override RewardType Type=>RewardType.Split;
            public override bool CanUse(PowerUpContext c,int slot=-1)=>!c.SplitActive && !c.State.UltraActive && c.History.Count==0 && c.Slots>=4 && c.Slots<=5;
            protected override void Apply(PowerUpContext c,LevelSaveData data,int slot)=>data.BoardSplitApplied=true;
        }
        private sealed class AutomaticNotesEffect : Effect
        {
            public override RewardType Type=>RewardType.Ultra;
            public override bool CanUse(PowerUpContext c,int slot=-1)=>!c.State.UltraActive && !c.SplitActive && c.Mechanic!=LevelMechanic.Growing;
            protected override void Apply(PowerUpContext c,LevelSaveData data,int slot){data.UltraActive=true;data.PositionNotes=null;}
        }
    }
}

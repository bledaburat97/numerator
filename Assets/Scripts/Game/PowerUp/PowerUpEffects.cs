using System;
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
    public interface IPowerUpEffect
    {
        RewardType Type { get; }
        bool CanUse(PowerUpContext context,int boardIndex=-1);
        bool IsAlreadyActive(PowerUpContext context);
        bool TryPrepare(PowerUpContext context,LevelSaveData checkpoint,int boardIndex,out PreparedPowerUp prepared);
    }
    public sealed class PowerUpContext
    {
        public LevelSaveData State;
        public IReadOnlyList<int> Answer,HistoryAnswer;
        public IReadOnlyList<List<int>> History;
        public IReadOnlyList<CardItemInfo> Cards;
        public int Remaining,Capacity,Slots,Removed,FrozenIndex=-1;
        public bool CanShrink;
        public LevelMechanic Mechanic;
        public bool SplitActive=>Mechanic==LevelMechanic.Split || State.BoardSplitApplied;
    }
    public abstract class PowerUpEffect : IPowerUpEffect
    {
        public abstract RewardType Type {get;}
        public abstract bool CanUse(PowerUpContext context,int boardIndex=-1);
        public virtual bool IsAlreadyActive(PowerUpContext context)=>false;
        public bool TryPrepare(PowerUpContext context,LevelSaveData checkpoint,int boardIndex,out PreparedPowerUp prepared)
        {
            prepared=null;if(!CanUse(context,boardIndex))return false;
            prepared=Prepare(context,checkpoint,boardIndex);return prepared!=null;
        }
        protected abstract PreparedPowerUp Prepare(PowerUpContext context,LevelSaveData checkpoint,int boardIndex);
    }
    public sealed class LifePowerUpEffect : PowerUpEffect
    {
        private readonly Func<IGuessManager> _live;
        public LifePowerUpEffect(Func<IGuessManager> live){_live=live;}
        public override RewardType Type=>RewardType.Life;
        public override bool CanUse(PowerUpContext c,int slot=-1)=>c.Remaining>0 && c.Capacity-c.Remaining>=PowerUpDefinition.Get(Type).ExtraGuesses;
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData data,int slot)
        {
            int before=c.Remaining,after=before+PowerUpDefinition.Get(Type).ExtraGuesses;data.RemainingGuessCount=after;
            return new PreparedPowerUp(()=>_live().ApplyCommittedLives(before,after));
        }
    }
    public sealed class BombPowerUpEffect : PowerUpEffect
    {
        private readonly Func<IBombPowerUpController> _live;
        public BombPowerUpEffect(Func<IBombPowerUpController> live){_live=live;}
        public override RewardType Type=>RewardType.Bomb;
        public override bool CanUse(PowerUpContext c,int slot=-1)=>c.CanShrink && c.Slots>1 && c.Removed==0 && c.History.Count==0
            && c.Answer?.Count==c.Slots && c.Cards.All(card=>!card.isLocked);
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData data,int slot)
        {
            int before=c.Slots;
            data.TargetCards.RemoveAt(data.TargetCards.Count-1);data.RemovedBoardHolderCount++;
            foreach(var card in data.CardItemInfoList)card.possibleCardHolderIndicatorIndexes.RemoveAll(index=>index>=data.TargetCards.Count);
            if(data.Dynamite!=null && (data.Dynamite.BoardIndex<0 || data.Dynamite.BoardIndex>=data.TargetCards.Count))
                data.Dynamite.BoardIndex=UnityEngine.Random.Range(0,data.TargetCards.Count);
            if(data.PairClue!=null && !data.PairClue.Matches(data.TargetCards))data.PairClue=PairEffect.SelectClue(data.TargetCards,data.CardItemInfoList);
            return new PreparedPowerUp(()=>_live().ApplyCommitted(before,data));
        }
    }
    public sealed class RevealPowerUpEffect : PowerUpEffect
    {
        private readonly Func<ICardPlacementCoordinator> _live;
        public RevealPowerUpEffect(Func<ICardPlacementCoordinator> live){_live=live;}
        public override RewardType Type=>RewardType.Revealing;
        public override bool CanUse(PowerUpContext c,int slot=-1)
        {
            if(c.Answer==null)return false;
            if(slot<0)return Enumerable.Range(0,c.Answer.Count).Any(index=>CanUse(c,index));
            if(slot>=c.Answer.Count || slot==c.FrozenIndex)return false;
            int fruit=c.Answer[slot]-1;
            return fruit>=0 && fruit<c.Cards.Count && c.Cards[fruit].isExisted && !c.Cards[fruit].isLocked;
        }
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData data,int slot)
        {
            if(slot<0)return null;
            int fruit=data.TargetCards[slot]-1;
            CardItemInfoManager.SetCertain(data.CardItemInfoList[fruit],new List<int>{slot});
            if(data.Dynamite?.BoardIndex==slot)data.Dynamite.Disarmed=true;
            return new PreparedPowerUp(()=>_live().TryRevealAndLockCard(slot,fruit));
        }
    }
    public sealed class InspectPowerUpEffect : PowerUpEffect
    {
        private readonly Func<HelperEffect> _live;
        public InspectPowerUpEffect(Func<HelperEffect> live){_live=live;}
        public override RewardType Type=>RewardType.Helper;
        private static GuessEvaluation Plan(PowerUpContext c)=>HelperEffect.Plan(c.Remaining,c.History,c.State.RevealedGuessNumbers,
            c.HistoryAnswer??c.Answer,c.SplitActive);
        public override bool CanUse(PowerUpContext c,int slot=-1)=>Plan(c)!=null;
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData data,int slot)
        {
            var inspection=Plan(c);if(inspection==null)return null;
            HelperEffect.Record(data,inspection.Number);
            return new PreparedPowerUp(()=>_live().ApplyCommitted(inspection));
        }
    }
    public sealed class PairPowerUpEffect : PowerUpEffect
    {
        private readonly Func<PairEffect> _live;
        public PairPowerUpEffect(Func<PairEffect> live){_live=live;}
        public override RewardType Type=>RewardType.Pair;
        public override bool CanUse(PowerUpContext c,int slot=-1)=>c.Mechanic!=LevelMechanic.Pair
            && PairEffect.CanPlan(true,c.Remaining,c.History,c.Answer,c.Cards,c.State);
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData data,int slot)
        {
            var clue=PairEffect.SelectClue(c.Answer,c.Cards);data.PairWasApplied=true;data.PairClue=clue;
            return new PreparedPowerUp(()=>_live().ApplyCommitted(clue));
        }
    }
    public sealed class SplitPowerUpEffect : PowerUpEffect
    {
        private readonly Func<BoardSplitService> _live;
        public SplitPowerUpEffect(Func<BoardSplitService> live){_live=live;}
        public override RewardType Type=>RewardType.Split;
        public override bool CanUse(PowerUpContext c,int slot=-1)=>BoardSplitService.CanActivate(true,c.SplitActive,c.State.UltraActive,c.Remaining,c.Slots,c.History.Count);
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData data,int slot)
        {data.BoardSplitApplied=true;return new PreparedPowerUp(()=>_live().ApplyCommitted(true));}
    }
    public sealed class UltraPowerUpEffect : PowerUpEffect
    {
        private readonly Func<UltraPowerUpController> _live;
        public UltraPowerUpEffect(Func<UltraPowerUpController> live){_live=live;}
        public override RewardType Type=>RewardType.Ultra;
        public override bool IsAlreadyActive(PowerUpContext c)=>c.State.UltraActive;
        public override bool CanUse(PowerUpContext c,int slot=-1)=>!c.State.UltraActive && PositionAssistanceController.Supports(c.Mechanic,c.SplitActive);
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData data,int slot)
        {data.UltraActive=true;data.PositionNotes=null;return new PreparedPowerUp(()=>_live().ApplyCommitted());}
    }
}

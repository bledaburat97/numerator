using System;
using System.Collections.Generic;
using System.Linq;
using Game;

namespace Scripts
{
    // Turn decisions are checkpointed. In particular, adding Life after a final
    // turn has thawed must not suddenly freeze the board the player is editing.
    public sealed class IceRoundState
    {
        public int TurnNumber=1;
        public bool Frozen,FinalThaw;
        public IceRoundState Copy()=>(IceRoundState)MemberwiseClone();
        public static bool IsValid(LevelSaveData save)
        {
            var state=save.Ice;var definition=save.Definition;
            int width=definition?.NumOfBoardHolders??0;
            if(state==null || definition==null || definition.Mechanic!=LevelMechanic.Ice || width<2 || width>5
                || definition.MaxNumOfTries%2!=1 || save.RemainingGuessCount<=0 || save.RemainingGuessCount>definition.MaxNumOfTries
                || save.RemovedBoardHolderCount!=0 || save.TargetCards?.Count!=width || save.TriedCardsList==null
                || save.CardItemInfoList?.Count!=definition.NumOfCards
                || save.CardItemInfoList.Any(info=>info==null || info.possibleCardHolderIndicatorIndexes==null)
                || state.TurnNumber!=save.TriedCardsList.Count+1
                || state.Frozen!=(state.TurnNumber%2==0 && !state.FinalThaw) || (state.FinalThaw && state.TurnNumber%2!=0)
                || (state.Frozen && save.RemainingGuessCount<=1))return false;
            for(int i=0;i<save.TriedCardsList.Count;i++)
            {
                var cards=save.TriedCardsList[i];
                if(cards==null || cards.Count!=width || cards.Take(width-1).Any(fruit=>fruit<1 || fruit>definition.NumOfCards)
                    || cards.Last()<0 || cards.Last()>definition.NumOfCards
                    || (cards.Last()==GuessSubmission.FrozenMarker && (i+1)%2!=0)
                    || cards.Where(fruit=>fruit>0).Distinct().Count()!=cards.Count(fruit=>fruit>0))return false;
            }
            return save.TargetCards.All(fruit=>fruit>=1 && fruit<=definition.NumOfCards)
                && save.TargetCards.Distinct().Count()==width
                && save.TargetCards.All(fruit=>save.CardItemInfoList[fruit-1].isExisted);
        }
    }

    // No scene dependencies: all input paths consult the same frozen-slot rule.
    public sealed class IceBoardService
    {
        private readonly ILevelDataCreator _levels;
        private readonly ILevelTracker _tracker;
        private readonly ILevelSaveDataManager _save;
        private readonly IRoundStateManager _round;
        private LevelSaveData _finishedRound;
        public event Action Changed;
        public IceBoardService(ILevelDataCreator levels,ILevelTracker tracker,ILevelSaveDataManager save,IRoundStateManager round)
        {_levels=levels;_tracker=tracker;_save=save;_round=round;}
        public bool IsActive=>_tracker.GetGameOption()==GameOption.SinglePlayer && _levels.GetLevelData()?.Mechanic==LevelMechanic.Ice;
        public int FrozenIndex=>IsFrozen?_levels.GetLevelData().NumOfBoardHolders-1:-1;
        public bool IsFrozen
        {
            get
            {
                if(!IsActive || ReferenceEquals(_finishedRound,_save.GetLevelSaveData()) || _round.GetRemainingGuessCount()<=1)return false;
                int number=_round.GetTriedCardsList().Count+1;
                var state=_save.GetLevelSaveData().Ice;
                // During WrongGuess callbacks, history/life have already advanced
                // but presentation has not. Crystal hints must target the NEXT turn.
                return state!=null && state.TurnNumber==number?state.Frozen:number%2==0;
            }
        }
        public bool IsBlocked(int slot)=>slot>=0 && slot==FrozenIndex;
        public void PrepareTurn()
        {
            if(!IsActive){Changed?.Invoke();return;}
            var data=_save.GetLevelSaveData();int number=_round.GetTriedCardsList().Count+1;
            if(data.Ice==null || data.Ice.TurnNumber!=number)
                data.Ice=new IceRoundState {TurnNumber=number,Frozen=number%2==0 && _round.GetRemainingGuessCount()>1,
                    FinalThaw=number%2==0 && _round.GetRemainingGuessCount()<=1};
            Changed?.Invoke();
        }
        public void AfterGuess(GuessEvaluation guess)
        {
            if(!IsActive)return;
            if(guess.Won){_finishedRound=_save.GetLevelSaveData();Changed?.Invoke();return;}
            PrepareTurn();
        }
        public GuessSubmission Capture(IReadOnlyList<int> cards)
        {
            int width=_levels.GetLevelData().NumOfBoardHolders;
            return IsFrozen?GuessSubmission.FrozenBoard(cards,width,FrozenIndex):GuessSubmission.FullBoard(cards,width);
        }
    }
}

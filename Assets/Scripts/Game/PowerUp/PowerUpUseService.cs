using System;
using System.Linq;
using Game;
using Newtonsoft.Json;
using Scripts.Economy;
using UnityEngine;

namespace Scripts
{
    // Isolates observers; it neither routes commands nor owns subscriptions.
    public static class ObserverNotification
    {
        [ThreadStatic] private static Scope _scope;
        public sealed class Scope : IDisposable
        {
            private readonly Scope _parent;private readonly System.Collections.Generic.List<Action> _notifications=new System.Collections.Generic.List<Action>();
            private bool _closed;
            internal Scope(){_parent=_scope;_scope=this;}
            internal void Add(Action action)=>_notifications.Add(action);
            public void Complete()
            {
                if(_closed)return;_closed=true;_scope=_parent;
                foreach(var action in _notifications)Present(action);
            }
            public void Dispose(){if(!_closed){_scope=_parent;_closed=true;}}
        }
        public static Scope Defer()=>new Scope();
        public static void Present(Action action){if(_scope!=null)_scope.Add(action);else Run(action);}
        public static void Run(Action action)
        {try{action?.Invoke();}catch(Exception error){Debug.LogException(error);}}
        public static void Send(Action handlers)
        {if(handlers!=null)foreach(Action handler in handlers.GetInvocationList())Present(handler);}
        public static void SendImmediately(Action handlers)
        {if(handlers!=null)foreach(Action handler in handlers.GetInvocationList())Run(handler);}
        public static void Send<T>(Action<T> handlers,T value)
        {if(handlers!=null)foreach(Action<T> handler in handlers.GetInvocationList())Present(()=>handler(value));}
        public static void Send(EventHandler handlers,object sender,EventArgs args)
        {if(handlers!=null)foreach(EventHandler handler in handlers.GetInvocationList())Present(()=>handler(sender,args));}
        public static void Send<T>(EventHandler<T> handlers,object sender,T value)
        {if(handlers!=null)foreach(EventHandler<T> handler in handlers.GetInvocationList())Present(()=>handler(sender,value));}
    }
    public enum PowerUpUseResult { Unavailable, Applied, AlreadyActive, StorageFailed, ResumeRequired }

    public sealed class PowerUpUseService
    {
        private readonly ILevelTracker _tracker;
        private readonly ILevelDataCreator _levels;
        private readonly ILevelSaveDataManager _state;
        private readonly IRoundStateManager _round;
        private readonly ICardItemInfoManager _cards;
        private readonly IBoardHolderCountManager _board;
        private readonly ITargetNumberCreator _target;
        private readonly PowerUpUseJournal _journal;
        private readonly IGameSaveService _saves;
        private readonly Func<IGameSaveSnapshotProvider> _snapshots;
        private readonly IPowerUpCatalog _effects;
        private readonly Func<IceBoardService> _ice;
        private bool _busy;
        private LevelSaveData _failedState;
        public event Action Changed;
        public event Action RecoveryRequired;

        public PowerUpUseService(ILevelTracker tracker,ILevelDataCreator levels,ILevelSaveDataManager state,IRoundStateManager round,
            ICardItemInfoManager cards,IBoardHolderCountManager board,ITargetNumberCreator target,PowerUpUseJournal journal,IGameSaveService saves,
            Func<IGameSaveSnapshotProvider> snapshots,IPowerUpCatalog effects,Func<IceBoardService> ice)
        {_tracker=tracker;_levels=levels;_state=state;_round=round;_cards=cards;_board=board;_target=target;_journal=journal;_saves=saves;_snapshots=snapshots;_effects=effects;_ice=ice;}

        private PowerUpContext Context()=>new PowerUpContext {State=_state.GetLevelSaveData(),Answer=_target.GetTargetCardsList(),
            History=_round.GetTriedCardsList(),HistoryAnswer=_target.GetHistoryTargetCards(),Cards=_cards.GetCardItemInfoList(),Remaining=_round.GetRemainingGuessCount(),Capacity=_round.GetMaxGuessCount(),
            Slots=_board.GetBoardHolderCount(),Removed=_board.GetRemovedBoardHolderCount(),CanShrink=_board.CanShrink,
            Mechanic=_levels.GetLevelData().Mechanic,FrozenIndex=_ice?.Invoke()?.FrozenIndex??-1};

        public bool CanUse(RewardType type,int boardIndex=-1)
        {
            if(_busy || _tracker.GetGameOption()!=GameOption.SinglePlayer || _round.GetRemainingGuessCount()<=0
                || ReferenceEquals(_state.GetLevelSaveData(),_failedState) || !PowerUpTypeUtility.IsUnlocked(type,_tracker.GetLevelId())
                || _tracker.GetPowerUpCount(type)<=0)return false;
            var c=Context();
            if(c.History.Count>0 && c.History[c.History.Count-1].SequenceEqual(c.Answer))return false;
            return _effects.Get(type)?.CanUse(c,boardIndex)==true;
        }

        public PowerUpUseResult TryApply(RewardType type,int boardIndex=-1)
        {
            if(_busy || _tracker.GetGameOption()!=GameOption.SinglePlayer)return PowerUpUseResult.Unavailable;
            if(_effects.Get(type)?.IsAlreadyActive(Context())==true)return PowerUpUseResult.AlreadyActive;
            if(!CanUse(type,boardIndex) || (PowerUpDefinition.Get(type).NeedsBoardTarget && boardIndex<0))return PowerUpUseResult.Unavailable;
            _busy=true;bool committed=false;
            try
            {
                if(!_snapshots().TryCreateSnapshot(out var before,true))return PowerUpUseResult.Unavailable;
                var after=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(before));
                if(!_effects.Get(type).TryPrepare(Context(),after,boardIndex,out var prepared))return PowerUpUseResult.Unavailable;
                after.PowerUpUseRevision=checked(before.PowerUpUseRevision+1);
                // A resumable base must exist before the durable profile debit.
                _saves.Save(before);
                var request=new PowerUpUseRequest(type,boardIndex);
                var result=_journal.Commit(before,after,request);
                if(result==ProfileCommitResult.Rejected)return PowerUpUseResult.Unavailable;
                committed=true;
                if(result==ProfileCommitResult.AlreadyApplied || !prepared.ApplyCommitted())
                {_failedState=_state.GetLevelSaveData();return PowerUpUseResult.ResumeRequired;}
                _state.GetLevelSaveData().PowerUpUseRevision=after.PowerUpUseRevision;
                _saves.Save(after);
                GameTelemetry.Powerup(type,false,"single");
                return PowerUpUseResult.Applied;
            }
            catch(Exception error)
            {
                if(committed)_failedState=_state.GetLevelSaveData();
                Debug.LogWarning("Powerup use could not finish: "+error.Message);
                return committed?PowerUpUseResult.ResumeRequired:PowerUpUseResult.StorageFailed;
            }
            finally
            {
                _busy=false;
                if(ReferenceEquals(_failedState,_state.GetLevelSaveData()))ObserverNotification.SendImmediately(RecoveryRequired);
                ObserverNotification.Send(Changed);
            }
        }
    }
}

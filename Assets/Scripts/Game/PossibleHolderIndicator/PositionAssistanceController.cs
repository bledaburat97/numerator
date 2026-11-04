using System;
using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Scripts
{
    public interface IPositionAssistanceView : IDisposable
    {
        void SetEnabled(bool enabled);
        void Render(PositionDeductionService state, Action<PositionDeductionReason> explain);
        void Explain(PositionDeductionReason reason);
        void CloseReason();
        void Tick();
    }
    public interface IPositionNotesSettings
    {
        bool AssistanceEnabled { get; }
        void SetAssistanceEnabled(bool enabled);
        void ResetNotes();
    }
    public sealed class PositionAssistancePreferences
    {
        private const string Key="normal_assistance_enabled";
        private readonly bool _persist;
        public bool Enabled { get; private set; }
        public event Action<bool> Changed;
        [Zenject.Inject] public PositionAssistancePreferences() : this(PlayerPrefs.GetInt(Key,1)!=0,true) { }
        public PositionAssistancePreferences(bool enabled,bool persist=false) {Enabled=enabled;_persist=persist;}
        public void Set(bool enabled)
        {
            if(Enabled==enabled)return;
            Enabled=enabled;
            if(_persist){PlayerPrefs.SetInt(Key,enabled?1:0);PlayerPrefs.Save();}
            Changed?.Invoke(enabled);
        }
    }

    public sealed class PositionAssistanceController : IDisposable,IPositionNotesSettings
    {
        private readonly IResultHistorySource _history;
        private readonly IInitialCardAreaController _cards;
        private readonly IBoardHolderCountManager _counts;
        private readonly ILevelDataCreator _levels;
        private readonly ICardItemInfoManager _info;
        private readonly BoardSplitService _split;
        private readonly IPositionAssistanceView _view;
        private readonly IInitialCardAreaEvents _actors;
        private readonly ICardKnowledgeEvents _knowledge;
        private readonly PositionAssistancePreferences _preferences;
        private readonly ILevelSaveDataManager _save;
        private readonly HashSet<int> _details=new HashSet<int>();
        private int _generation=-1,_processed;
        private bool _dirty=true,_disposed;
        private bool[] _knownAbsent;
        private int[] _knownLocks;
        private LevelSaveData _roundData;
        public PositionDeductionService State { get; private set; }
        public bool AssistanceEnabled=>_preferences.Enabled;
        public event Action Changed;
        private bool CanDeduce=>AssistanceEnabled && _levels.GetLevelData()?.Mechanic!=LevelMechanic.Growing
            && _levels.GetLevelData()?.Mechanic!=LevelMechanic.Split && !(_split?.IsActive??false);

        public PositionAssistanceController(IResultHistorySource history,IInitialCardAreaController cards,
            IBoardHolderCountManager counts,ILevelDataCreator levels,ICardItemInfoManager info,
            BoardSplitService split,IPositionAssistanceView view,PositionAssistancePreferences preferences=null,ILevelSaveDataManager save=null)
        {
            _history=history;_cards=cards;_counts=counts;_levels=levels;_info=info;_split=split;_view=view;
            _preferences=preferences??new PositionAssistancePreferences(true);_save=save;
            _actors=cards as IInitialCardAreaEvents;_knowledge=info as ICardKnowledgeEvents;
            _history.Changed+=Invalidate;_history.DetailsChanged+=OnDetails;_preferences.Changed+=OnPreferenceChanged;
            if(_actors!=null)_actors.Rebuilt+=Invalidate;
            if(_knowledge!=null)_knowledge.Changed+=Invalidate;
            if(_split!=null)_split.Applied+=OnSplit;
        }
        private void Invalidate()=>_dirty=true;
        private void OnDetails(int index){_details.Add(index);Invalidate();}
        private void OnSplit(object sender,EventArgs args)=>Invalidate();
        private void OnPreferenceChanged(bool enabled){if(enabled)ResetNotes();else {Refresh();}}
        public void SetAssistanceEnabled(bool enabled)=>_preferences.Set(enabled);
        public void Tick(){if(_disposed)return;_view.Tick();if(_dirty)Refresh();}

        public void Refresh()
        {
            if(_disposed)return;
            _dirty=false;
            int slots=_counts.GetBoardHolderCount(),fruits=_cards.GetCardCount();
            if(slots<1 || slots>5 || fruits<slots || fruits>9)
            {State=null;_view.SetEnabled(false);_view.Render(null,null);return;}
            var data=_save?.GetLevelSaveData();
            bool newRound=State==null || _generation!=_history.Generation || (_save!=null && !ReferenceEquals(data,_roundData)) || _processed>_history.HistoryCount;
            if(newRound)
            {
                _roundData=data;_generation=_history.Generation;_processed=0;_details.Clear();_view.CloseReason();
                var snapshot=data?.PositionNotes;
                bool restore=snapshot!=null && snapshot.Fruits==fruits && snapshot.Slots==slots && snapshot.HistoryCount<=_history.HistoryCount
                    && !(AssistanceEnabled && !snapshot.AutomaticEnabled);
                State=restore?PositionDeductionService.Restore(snapshot):null;
                if(State!=null)_processed=snapshot.HistoryCount;
                else State=new PositionDeductionService(fruits,slots);
                ResetKnowledge();
                if(restore && _processed>0)AcknowledgeKnowledge();
            }
            else if(State.SlotCount!=slots || State.FruitCount!=fruits)
            {
                State=State.FruitCount==fruits?State.Resize(slots):new PositionDeductionService(fruits,slots);
                ResetKnowledge();
            }
            if(CanDeduce)
            {
                while(_processed<_history.HistoryCount)
                {
                    int index=_processed++;var row=_history.GetHistoryEntry(index);
                    if(row.groups!=null && row.groups.Count>0)continue;
                    // Growing/split history is never interpreted as whole-board evidence.
                    if(row.finalNumbers.Count==State.SlotCount)State.ApplyGuess(index+1,row.finalNumbers,row.correctPosCount,row.wrongPosCount);
                    if(row.hintStates!=null)_details.Add(index);
                }
                foreach(int index in _details)
                {
                    if(index<0 || index>=_history.HistoryCount)continue;
                    var row=_history.GetHistoryEntry(index);
                    if(row.hintStates==null)continue;
                    for(int slot=0;slot<row.finalNumbers.Count && slot<row.hintStates.Count && slot<State.SlotCount;slot++)
                        State.ApplyVisibleDetail(index+1,row.finalNumbers[slot],slot,row.hintStates[slot]);
                }
                ApplyKnowledge();
            }
            else {_processed=_history.HistoryCount;AcknowledgeKnowledge();}
            _details.Clear();Publish();
        }
        private void ResetKnowledge()
        {
            _knownAbsent=new bool[State.FruitCount];_knownLocks=new int[State.FruitCount];
            for(int i=0;i<_knownLocks.Length;i++)_knownLocks[i]=-1;
        }
        private void AcknowledgeKnowledge()=>ReadKnowledge(false);
        private void ApplyKnowledge()=>ReadKnowledge(true);
        private void ReadKnowledge(bool apply)
        {
            var known=_info.GetCardItemInfoList();
            for(int fruit=0;known!=null && fruit<State.FruitCount && fruit<known.Count;fruit++)
            {
                var fact=known[fruit];
                if(!fact.isExisted && !_knownAbsent[fruit]){_knownAbsent[fruit]=true;if(apply)State.RevealAbsent(fruit+1);}
                if(fact.isLocked && fact.possibleCardHolderIndicatorIndexes.Count==1)
                {
                    int slot=fact.possibleCardHolderIndicatorIndexes[0];
                    if(_knownLocks[fruit]!=slot){_knownLocks[fruit]=slot;if(apply)State.RevealPosition(fruit+1,slot);}
                }
            }
        }
        private bool CanEdit(int fruit)
        {
            if(_disposed || State==null || fruit<1 || fruit>State.FruitCount)return false;
            var info=_info.GetCardItemInfoList();
            return info!=null && fruit<=info.Count && info[fruit-1].isExisted && !info[fruit-1].isLocked;
        }
        public void TogglePosition(int fruit,int slot)
        {
            if(_dirty)Refresh();
            if(!CanEdit(fruit) || slot<0 || slot>=State.SlotCount)return;
            State.TogglePosition(fruit,slot);Publish();
        }
        public void SetPresence(int fruit,FruitPresence presence)
        {
            if(_dirty)Refresh();if(!CanEdit(fruit))return;
            State.SetManualPresence(fruit,presence);Publish();
        }
        public void ResetNotes()
        {
            if(_disposed)return;
            if(_save!=null)_save.GetLevelSaveData().PositionNotes=null;
            State=null;Refresh();
            if(_save!=null && _save.GetLevelSaveData().PositionNotes!=null)_save.GetLevelSaveData().PositionNotes.Edited=true;
        }
        private void Publish()
        {
            // Conflicting player notes remain visible and editable; they never invalidate a guess.
            _view.SetEnabled(true);_view.Render(State,_view.Explain);
            if(_save!=null)
            {
                bool edited=State.HasManualChanges || (_save.GetLevelSaveData().PositionNotes?.Edited??false);
                _save.GetLevelSaveData().PositionNotes=State.Capture(_processed,AssistanceEnabled);
                _save.GetLevelSaveData().PositionNotes.Edited=edited;
            }
            Changed?.Invoke();
        }
        public void Suspend()=>_view.CloseReason();
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            _history.Changed-=Invalidate;_history.DetailsChanged-=OnDetails;_preferences.Changed-=OnPreferenceChanged;
            if(_actors!=null)_actors.Rebuilt-=Invalidate;
            if(_knowledge!=null)_knowledge.Changed-=Invalidate;
            if(_split!=null)_split.Applied-=OnSplit;
            _view.Dispose();
        }
    }
}

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
        bool UltraActive { get; }
        bool CanResetNotes { get; }
        void ResetNotes();
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
        private readonly ILevelSaveDataManager _save;
        private readonly HashSet<int> _details=new HashSet<int>();
        private int _generation=-1,_processed;
        private bool _dirty=true,_disposed;
        private bool[] _knownAbsent;
        private int[] _knownLocks;
        private LevelSaveData _roundData;
        public PositionDeductionService State { get; private set; }
        private bool _ultraActive,_indicatorsVisible;
        public bool UltraActive=>_ultraActive;
        public bool IndicatorsVisible=>_indicatorsVisible || UltraActive;
        public bool CanResetNotes=>!UltraActive;
        public bool CanEditNotes=>!UltraActive;
        public bool SupportsAutomatic=>_levels.GetLevelData()?.Mechanic!=LevelMechanic.Growing
            && _levels.GetLevelData()?.Mechanic!=LevelMechanic.Split && !(_split?.IsActive??false);
        private bool SupportsSplit=>_levels.GetLevelData()?.Mechanic==LevelMechanic.Ice;
        public event Action Changed;
        private bool CanDeduce=>UltraActive && SupportsAutomatic;

        public PositionAssistanceController(IResultHistorySource history,IInitialCardAreaController cards,
            IBoardHolderCountManager counts,ILevelDataCreator levels,ICardItemInfoManager info,
            BoardSplitService split,IPositionAssistanceView view,ILevelSaveDataManager save=null)
        {
            _history=history;_cards=cards;_counts=counts;_levels=levels;_info=info;_split=split;_view=view;
            _save=save;
            _actors=cards as IInitialCardAreaEvents;_knowledge=info as ICardKnowledgeEvents;
            _history.Changed+=Invalidate;_history.DetailsChanged+=OnDetails;
            if(_actors!=null)_actors.Rebuilt+=Invalidate;
            if(_knowledge!=null)_knowledge.Changed+=Invalidate;
            if(_split!=null)_split.Applied+=OnSplit;
        }
        private void Invalidate()=>_dirty=true;
        private void OnDetails(int index){_details.Add(index);Invalidate();}
        private void OnSplit(object sender,EventArgs args)=>Invalidate();
        // Called only after the powerup transaction succeeds. Rebuild from public evidence;
        // player notes never seed Ultra's calculation.
        public void ActivateUltra()
        {
            if(_dirty || State==null)Refresh();
            if(_disposed || UltraActive || !SupportsAutomatic || State==null)return;
            _ultraActive=true;
            if(_save!=null)_save.GetLevelSaveData().UltraActive=true;
            RebuildEvidence();
        }
        private void RebuildEvidence()
        {
            State=new PositionDeductionService(State.FruitCount,State.SlotCount);
            _processed=0;_details.Clear();ResetKnowledge();_dirty=true;Refresh();
        }
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
                _ultraActive=data?.UltraActive??false;
                var snapshot=data?.PositionNotes;
                // Pre-Ultra automatic notes do not enable a paid power or seed a manual notebook.
                bool restore=!_ultraActive && snapshot!=null && !snapshot.AutomaticEnabled
                    && snapshot.Fruits==fruits && snapshot.Slots==slots && snapshot.HistoryCount<=_history.HistoryCount;
                _indicatorsVisible=restore && snapshot.IndicatorsVisible;
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
                    if(row.groups!=null && row.groups.Count>0 && !SupportsSplit)continue;
                    // Growing/split history is never interpreted as whole-board evidence.
                    if(row.finalNumbers.Count==State.SlotCount)State.ApplyGuess(index+1,row.finalNumbers,row.correctPosCount,row.wrongPosCount,row.groups);
                    if(row.hintStates!=null)_details.Add(index);
                }
                for(int index=0;index<_history.HistoryCount;index++)
                    if(_history.GetHistoryEntry(index).hintStates!=null)_details.Add(index);
                foreach(int index in _details)
                {
                    if(index<0 || index>=_history.HistoryCount)continue;
                    var row=_history.GetHistoryEntry(index);
                    if(row.hintStates==null)continue;
                    for(int slot=0;slot<row.finalNumbers.Count && slot<row.hintStates.Count && slot<State.SlotCount;slot++)
                    {
                        if(row.groups!=null && row.groups.Count>0)
                        {
                            foreach(var group in row.groups)if(slot>=group.Start && slot<group.Start+group.Count)
                                State.ApplyVisibleDetail(index+1,row.finalNumbers[slot],slot,row.hintStates[slot],group.Start,group.Count);
                        }
                        else State.ApplyVisibleDetail(index+1,row.finalNumbers[slot],slot,row.hintStates[slot]);
                    }
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
        public bool CanEditFruit(int fruit){if(_dirty || State==null)Refresh();return CanEdit(fruit);}
        private bool CanEdit(int fruit)
        {
            if(_disposed || UltraActive || State==null || fruit<1 || fruit>State.FruitCount)return false;
            for(int index=0;index<_history.HistoryCount;index++)
            {
                var row=_history.GetHistoryEntry(index);
                if(row.hintStates==null)continue;
                for(int slot=0;slot<row.finalNumbers.Count && slot<row.hintStates.Count;slot++)
                    if(row.finalNumbers[slot]==fruit && ((row.hintStates[slot]==CardResultState.Absent && (row.groups==null || row.groups.Count==0)) || row.hintStates[slot]==CardResultState.Correct))return false;
            }
            var info=_info.GetCardItemInfoList();
            return info!=null && fruit<=info.Count && info[fruit-1].isExisted && !info[fruit-1].isLocked;
        }
        public void TogglePosition(int fruit,int slot)
        {
            if(_dirty)Refresh();
            if(!CanEdit(fruit) || slot<0 || slot>=State.SlotCount)return;
            if(State.IsPossible(fruit,slot))_indicatorsVisible=true;
            State.TogglePosition(fruit,slot);Publish();
        }
        public void SetPresence(int fruit,FruitPresence presence)
        {
            if(_dirty)Refresh();if(!CanEdit(fruit))return;
            State.SetManualPresence(fruit,presence);Publish();
        }
        public void ResetNotes()
        {
            if(_disposed || UltraActive)return;
            if(_dirty || State==null)Refresh();
            bool visible=_indicatorsVisible;
            State=new PositionDeductionService(State.FruitCount,State.SlotCount);
            _processed=_history.HistoryCount;_indicatorsVisible=visible;ResetKnowledge();Publish();
            if(_save!=null && _save.GetLevelSaveData().PositionNotes!=null)_save.GetLevelSaveData().PositionNotes.Edited=true;
        }
        private void Publish()
        {
            // Conflicting player notes remain visible and editable; they never invalidate a guess.
            _view.SetEnabled(IndicatorsVisible);_view.Render(State,_view.Explain);
            if(_save!=null)
            {
                bool edited=State.HasManualChanges || (_save.GetLevelSaveData().PositionNotes?.Edited??false);
                _save.GetLevelSaveData().PositionNotes=State.Capture(_processed,UltraActive);
                _save.GetLevelSaveData().PositionNotes.Edited=edited;
                _save.GetLevelSaveData().PositionNotes.IndicatorsVisible=_indicatorsVisible;
            }
            Changed?.Invoke();
        }
        public void Suspend()=>_view.CloseReason();
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            _history.Changed-=Invalidate;_history.DetailsChanged-=OnDetails;
            if(_actors!=null)_actors.Rebuilt-=Invalidate;
            if(_knowledge!=null)_knowledge.Changed-=Invalidate;
            if(_split!=null)_split.Applied-=OnSplit;
            _view.Dispose();
        }
    }
}

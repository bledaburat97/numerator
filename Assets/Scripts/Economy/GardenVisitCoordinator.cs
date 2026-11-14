using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Economy
{
    // Application-session lifetime. Scene/menu navigation never calls ProcessVisit.
    public sealed class GardenVisitCoordinator : IDisposable
    {
        private readonly IGardenService _garden;
        private bool _started, _paused, _focused=true;
        private DateTime _nextGrowth=DateTime.MinValue;
        private readonly IPlayerProfileRepository _profiles;
        private GardenVisitNotice _pending;
        private readonly Dictionary<string,GardenBatchChange> _visualChanges=new Dictionary<string,GardenBatchChange>();
        public event Action NoticeAvailable;
        public event Action<Exception> Failed;

        public GardenVisitCoordinator(IGardenService garden,IPlayerProfileRepository profiles=null)
        { _garden=garden;_profiles=profiles;if(_profiles!=null)_profiles.Updated+=OnProfileChanged; }
        private void OnProfileChanged(ProfileChange change){if(change.Includes(ProfileArea.Garden))_nextGrowth=DateTime.MinValue;}
        public void Tick(){if(_started && !_paused && _focused && _garden.UtcNow>=_nextGrowth)Visit();}
        public void Dispose(){if(_profiles!=null)_profiles.Updated-=OnProfileChanged;}
        private void ScheduleNext()
        {
            _nextGrowth=DateTime.MaxValue;
            foreach(var batch in _garden.GetBatches().Values)
                if(batch.Stage!=GardenGrowthStage.Fruit && GardenGrowth.NextStageAt(batch)<_nextGrowth)_nextGrowth=GardenGrowth.NextStageAt(batch);
        }
        public void Initialize()
        {
            if(_started)return;
            _started=true;if(!_paused && _focused)Visit();
        }
        public void SetPaused(bool paused)
        {
            bool wasForeground=!_paused && _focused;
            _paused=paused;
            if(_started && !wasForeground && !_paused && _focused)Visit();
        }
        public void SetFocused(bool focused)
        {
            bool wasForeground=!_paused && _focused;
            _focused=focused;
            if(_started && !wasForeground && !_paused && _focused)Visit();
        }
        public GardenVisitNotice TakeNotice()
        {
            var pending=_pending;_pending=null;
            if(pending!=null)
            {
                // A player may already have collected a crop before opening this notice.
                var current=_garden.GetBatches();
                foreach(var fruit in new List<FruitId>(pending.Ready.Keys))
                    if(!System.Linq.Enumerable.Any(current.Values,batch=>batch.Fruit==fruit && batch.Stage==GardenGrowthStage.Fruit && !batch.IsLegacyOverflow))pending.Ready.Remove(fruit);
            }
            return pending;
        }
        public IReadOnlyList<GardenBatchChange> TakeVisualChanges()
        {
            var result=_visualChanges.Values.ToArray();_visualChanges.Clear();return result;
        }
        private void Visit()
        {
            try
            {
                var result=_garden.ProcessVisit();
                ScheduleNext();
                if(!result.HasProgress)return;
                // Only the 9 x 4 on-screen cells need animation history. Legacy
                // overflow still grows, without an unbounded presentation queue.
                var batches=_garden.GetBatches();
                foreach(var change in result.Changes)
                {
                    if(!batches.TryGetValue(change.BatchId,out var batch) || batch.IsLegacyOverflow)continue;
                    _visualChanges.TryGetValue(change.BatchId,out var earlier);
                    _visualChanges[change.BatchId]=new GardenBatchChange {BatchId=change.BatchId,Fruit=change.Fruit,Quantity=change.Quantity,
                        PreviousStage=earlier?.PreviousStage??change.PreviousStage,CurrentStage=change.CurrentStage};
                }
                if(_pending==null)_pending=new GardenVisitNotice();
                _pending.AdvancedBatches+=result.Changes.Count;
                if(result.Changes.Count>0)_pending.ExampleGrowth=result.Changes[0];
                foreach(var fruit in result.Ready)
                {
                    _pending.Ready.TryGetValue(fruit.Key,out long previous);
                    _pending.Ready[fruit.Key]=checked(previous+fruit.Value);
                }
            }
            catch(Exception error) { _nextGrowth=_garden.UtcNow.AddSeconds(5);Failed?.Invoke(error);return; }
            NoticeAvailable?.Invoke();
        }
    }

    public sealed class GardenVisitNotice
    {
        public int AdvancedBatches;
        public GardenBatchChange ExampleGrowth;
        public readonly Dictionary<FruitId,long> Ready=new Dictionary<FruitId,long>();
    }
}

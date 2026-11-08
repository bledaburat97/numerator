using System;
using System.Collections.Generic;

namespace Scripts.Economy
{
    // Application-session lifetime. Scene/menu navigation never calls ProcessVisit.
    public sealed class GardenVisitCoordinator
    {
        private readonly IGardenService _garden;
        private bool _started, _paused, _focused=true;
        private GardenVisitNotice _pending;
        public event Action NoticeAvailable;
        public event Action<Exception> Failed;

        public GardenVisitCoordinator(IGardenService garden) { _garden=garden; }
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
            var pending=_pending;_pending=null;return pending;
        }
        private void Visit()
        {
            try
            {
                var result=_garden.ProcessVisit();
                if(!result.HasProgress)return;
                if(_pending==null)_pending=new GardenVisitNotice();
                _pending.AdvancedBatches+=result.Changes.Count;
                foreach(var fruit in result.Harvested)
                {
                    _pending.Harvested.TryGetValue(fruit.Key,out long previous);
                    _pending.Harvested[fruit.Key]=checked(previous+fruit.Value);
                }
            }
            catch(Exception error) { Failed?.Invoke(error);return; }
            NoticeAvailable?.Invoke();
        }
    }

    public sealed class GardenVisitNotice
    {
        public int AdvancedBatches;
        public readonly Dictionary<FruitId,long> Harvested=new Dictionary<FruitId,long>();
    }
}

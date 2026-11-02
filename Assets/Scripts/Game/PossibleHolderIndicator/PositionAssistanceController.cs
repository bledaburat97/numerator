using System;
using System.Collections.Generic;
using Game;

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

    public sealed class PositionAssistanceController : IDisposable
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
        private readonly HashSet<int> _details = new HashSet<int>();
        private int _generation = -1, _processed;
        private bool _dirty = true, _disposed;
        private bool[] _knownAbsent;
        private int[] _knownLocks;
        public PositionDeductionService State { get; private set; }

        public PositionAssistanceController(IResultHistorySource history, IInitialCardAreaController cards,
            IBoardHolderCountManager counts, ILevelDataCreator levels, ICardItemInfoManager info,
            BoardSplitService split, IPositionAssistanceView view)
        {
            _history = history; _cards = cards; _counts = counts; _levels = levels; _info = info; _split = split; _view = view;
            _actors = cards as IInitialCardAreaEvents; _knowledge = info as ICardKnowledgeEvents;
            _history.Changed += Invalidate; _history.DetailsChanged += OnDetails;
            if (_actors != null) _actors.Rebuilt += Invalidate;
            if (_knowledge != null) _knowledge.Changed += Invalidate;
            if (_split != null) _split.Applied += OnSplit;
        }
        private void Invalidate() => _dirty = true;
        private void OnDetails(int index) { _details.Add(index); Invalidate(); }
        private void OnSplit(object sender, EventArgs args) => Invalidate();
        public void Tick()
        {
            if (_disposed) return;
            _view.Tick();
            if (_dirty) Refresh();
        }
        public void Refresh()
        {
            if (_disposed) return;
            _dirty = false;
            var definition = _levels.GetLevelData();
            int slots = _counts.GetBoardHolderCount(), fruits = _cards.GetCardCount();
            bool enabled = definition != null && definition.Mechanic != LevelMechanic.Growing && definition.Mechanic != LevelMechanic.Split
                && !(_split?.IsActive ?? false) && slots > 0 && slots <= 5 && fruits >= slots && fruits <= 9;
            if (!enabled)
            {
                State = null; _view.SetEnabled(false); _view.Render(null, null); return;
            }
            if (State == null || State.SlotCount != slots || State.FruitCount != fruits || _generation != _history.Generation || _processed > _history.HistoryCount)
            {
                State = new PositionDeductionService(fruits, slots); _generation = _history.Generation; _processed = 0;
                _knownAbsent = new bool[fruits]; _knownLocks = new int[fruits];
                for (int i = 0; i < fruits; i++) _knownLocks[i] = -1;
                _details.Clear(); _view.CloseReason();
            }
            while (_processed < _history.HistoryCount)
            {
                int index = _processed++;
                var row = _history.GetHistoryEntry(index);
                // Group-local feedback cannot be used as whole-board evidence.
                if (row.groups != null && row.groups.Count > 0) { State = null; _view.SetEnabled(false); _view.Render(null, null); return; }
                if (!State.ApplyGuess(index + 1, row.finalNumbers, row.correctPosCount, row.wrongPosCount))
                { State = null; _view.SetEnabled(false); _view.Render(null, null); return; }
                if (row.hintStates != null) _details.Add(index);
            }
            foreach (int index in _details)
            {
                if (index < 0 || index >= _history.HistoryCount) continue;
                var row = _history.GetHistoryEntry(index);
                if (row.hintStates == null) continue;
                for (int slot = 0; slot < row.finalNumbers.Count && slot < row.hintStates.Count; slot++)
                    State.ApplyVisibleDetail(index + 1, row.finalNumbers[slot], slot, row.hintStates[slot]);
            }
            _details.Clear();
            var known = _info.GetCardItemInfoList();
            for (int fruit = 0; known != null && fruit < fruits && fruit < known.Count; fruit++)
            {
                var fact = known[fruit];
                if (!fact.isExisted && !_knownAbsent[fruit]) { _knownAbsent[fruit] = true; State.RevealAbsent(fruit + 1); }
                if (fact.isLocked && fact.possibleCardHolderIndicatorIndexes.Count == 1)
                {
                    int slot = fact.possibleCardHolderIndicatorIndexes[0];
                    if (_knownLocks[fruit] != slot) { _knownLocks[fruit] = slot; State.RevealPosition(fruit + 1, slot); }
                }
            }
            _view.SetEnabled(State.IsConsistent);
            _view.Render(State.IsConsistent ? State : null, _view.Explain);
        }
        public void Suspend() => _view.CloseReason();

        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            _history.Changed -= Invalidate; _history.DetailsChanged -= OnDetails;
            if (_actors != null) _actors.Rebuilt -= Invalidate;
            if (_knowledge != null) _knowledge.Changed -= Invalidate;
            if (_split != null) _split.Applied -= OnSplit;
            _view.Dispose();
        }
    }
}

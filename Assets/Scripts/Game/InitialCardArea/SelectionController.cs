using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    public class SelectionController : ISelectionController
    {
        private Dictionary<int, bool> _selectionStates;
        public event EventHandler<List<int>> DeselectCards;
        public SelectionController(int numOfDefaultCards)
        {
            _selectionStates = new Dictionary<int, bool>();
            for (int i = 0; i < numOfDefaultCards; i++)
            {
                _selectionStates.Add(i, false);
            }
        }

        public void SetSelectionState(int index, bool status)
        {
            _selectionStates[index] = status;
        }

        public bool GetSelectionState(int index)
        {
            return _selectionStates[index];
        }
        
        public void DeselectAll()
        {
            DeselectCards.Invoke(this,
                _selectionStates.Where(pair => pair.Value == true).Select(pair => pair.Key).ToList());
            
            for (int i = 0; i < _selectionStates.Count; i++)
            {
                SetSelectionState(i, false);
            }
        }

        public int GetSelectedCardIndex()
        {
            for (int i = 0; i < _selectionStates.Count; i++)
            {
                if (_selectionStates[i]) return i;
            }

            return -1;
        }
    }
    
    public interface ISelectionController
    {
        void SetSelectionState(int index, bool status);
        bool GetSelectionState(int index);
        void DeselectAll();
        int GetSelectedCardIndex();
        event EventHandler<List<int>> DeselectCards;
    }
    
}
using System;
using System.Collections.Generic;

namespace Views
{
    public class SelectionController : ISelectionController
    {
        private Dictionary<int, bool> _selectionStates;
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
            for (int i = 0; i < _selectionStates.Count; i++)
            {
                SetSelectionState(i, false);
            }

            EventHandler handler = deselectCards;
            if (handler != null)
            {
                handler(this,null);
            }
        }

        public event EventHandler deselectCards;

    }
    
    public interface ISelectionController
    {
        void SetSelectionState(int index, bool status);
        bool GetSelectionState(int index);
        void DeselectAll();
        event EventHandler deselectCards;
    }
    
}
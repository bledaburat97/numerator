using System;
using System.Collections.Generic;
using System.Linq;
namespace Scripts
{
    public class BoardAreaManager : IBoardAreaManager
    {
        private int[] _numbersList;
        public event EventHandler<List<int>> ResultAdded;

        public BoardAreaManager(int finalNumberSize)
        {
            _numbersList = new int[finalNumberSize];
        }

        public void SetNumberOfCard(int index, int numberOfCard)
        {
            _numbersList[index] = numberOfCard;
            if (CheckAllNumbersPlaced()) ResultAdded?.Invoke(this, _numbersList.ToList());
        }

        private bool CheckAllNumbersPlaced()
        {
            for (int i = 0; i < _numbersList.Length; i++)
            {
                if (_numbersList[i] == 0) return false;
            }

            return true;
        }
    }
    
    public interface IBoardAreaManager
    {
        void SetNumberOfCard(int index, int numberOfCard);
        event EventHandler<List<int>> ResultAdded;
    }
}
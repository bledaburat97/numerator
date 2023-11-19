using System;
using System.Collections.Generic;
using System.Linq;
namespace Scripts
{
    public class BoardAreaManager : IBoardAreaManager
    {
        private int[] _numbersList;
        private IResultManager _resultManager;
        public BoardAreaManager(ILevelTracker levelTracker, IResultManager resultManager)
        {
            _numbersList = new int[levelTracker.GetLevelData().NumOfBoardHolders];
            _resultManager = resultManager;
        }

        public void SetNumberOfCard(int index, int numberOfCard)
        {
            _numbersList[index] = numberOfCard;
            if (CheckAllNumbersPlaced()) _resultManager.CheckFinalCardList(_numbersList.ToList());
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
    }
}
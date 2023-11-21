using System;
using System.Collections.Generic;
using System.Linq;
namespace Scripts
{
    public class BoardAreaManager : IBoardAreaManager
    {
        private int[] _numbersList;
        private IResultManager _resultManager;
        public BoardAreaManager(ILevelTracker levelTracker, IResultManager resultManager, ICheckButtonController checkButtonController)
        {
            _numbersList = new int[levelTracker.GetLevelData().NumOfBoardHolders];
            _resultManager = resultManager;
            checkButtonController.CheckFinalNumbers += CheckFinalNumbers;
        }

        public void SetNumberOfCard(int index, int numberOfCard)
        {
            _numbersList[index] = numberOfCard;
            
        }

        private void CheckFinalNumbers(object sender, EventArgs args)
        {
            if (CheckAllNumbersPlaced())
            {
                _resultManager.CheckFinalCardList(_numbersList.ToList());
            }
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
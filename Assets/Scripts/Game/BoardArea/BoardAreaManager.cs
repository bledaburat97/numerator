using System;
using System.Linq;
namespace Scripts
{
    public class BoardAreaManager : IBoardAreaManager
    {
        private int[] _numbersList;
        private IResultManager _resultManager;
        public BoardAreaManager(ILevelDataCreator levelDataCreator, IResultManager resultManager, IGameUIController gameUIController)
        {
            _numbersList = new int[levelDataCreator.GetLevelData().NumOfBoardHolders];
            _resultManager = resultManager;
            gameUIController.CheckFinalNumbers += CheckFinalNumbers;
        }

        public void SetCardNumber(int index, int numberOfCard)
        {
            _numbersList[index] = numberOfCard;
        }

        public int SetWildCardOnBoard(int index)
        {
            int targetNumber = _resultManager.GetTargetCardAtIndex(index);
            _numbersList[index] = targetNumber;
            return targetNumber - 1;
        }

        private void CheckFinalNumbers(object sender, EventArgs args)
        {
            if (CheckAllNumbersPlaced())
            {
                _resultManager.CheckFinalCards(_numbersList.ToList());
            }
        }

        private bool CheckAllNumbersPlaced()
        {
            return _numbersList.All(number => number != 0);
        }

        public int GetEmptyBoardHolderIndex()
        {
            for (int i = 0; i < _numbersList.Length; i++)
            {
                if (_numbersList[i] == 0) return i;
            }

            return -1;
        }
    }
    
    public interface IBoardAreaManager
    {
        void SetCardNumber(int index, int numberOfCard);
        int SetWildCardOnBoard(int index);
        int GetEmptyBoardHolderIndex();
    }
}
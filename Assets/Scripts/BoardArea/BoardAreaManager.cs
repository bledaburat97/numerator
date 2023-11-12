using UnityEngine;

namespace Scripts
{
    public class BoardAreaManager : IBoardAreaManager
    {
        private int[] _numbersList;
        private IResultChecker _resultChecker;

        public BoardAreaManager(IResultChecker resultChecker, int finalNumberSize)
        {
            _resultChecker = resultChecker;
            _numbersList = new int[finalNumberSize];
        }

        public void SetNumberOfCard(int index, int numberOfCard)
        {
            _numbersList[index] = numberOfCard;
            TryCheckFinalNumber();
        }

        private bool CheckAllNumbersPlaced()
        {
            for (int i = 0; i < _numbersList.Length; i++)
            {
                if (_numbersList[i] == 0) return false;
            }

            return true;
        }

        private void TryCheckFinalNumber()
        {
            if (CheckAllNumbersPlaced())
            {
                Result result = _resultChecker.CheckTargetAchieved(_numbersList);
                
                if (result.CorrectPos == _numbersList.Length)
                {
                    Debug.Log("WELL DONE");
                }
                else
                {
                    Debug.Log("Correct: " + result.CorrectPos + ", Wrong: " + result.WrongPos + ", NonExistence: " + result.NoExistence);
                }
            }
        }
    }
    
    public interface IBoardAreaManager
    {
        void SetNumberOfCard(int index, int numberOfCard);
    }
}
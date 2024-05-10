using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultManager : IResultManager
    {
        [Inject] private GamePopupCreator _gamePopupCreator;
        
        private List<int> _targetCards = new List<int>();
        private List<List<int>> _triedCardsList = new List<List<int>>();
        private int _numOfBoardHolders;
        public event EventHandler<ResultBlockModel> ResultBlockAddition;
        public event EventHandler<NumberGuessedEventArgs> NumberGuessed;
        public void Initialize(ILevelTracker levelTracker, ITargetNumberCreator targetNumberCreator, ILevelDataCreator levelDataCreator)
        {
            _numOfBoardHolders = levelDataCreator.GetLevelData().NumOfBoardHolders;
            _targetCards = targetNumberCreator.GetTargetCardsList();
            _triedCardsList = levelTracker.GetLevelSaveData().TriedCardsList;
            foreach (List<int> triedCards in _triedCardsList)
            {
                CalculatePositionCounts(triedCards, out int numOfCorrectPos, out int numOfWrongPos);
                ResultBlockAddition?.Invoke(this, new ResultBlockModel()
                {
                    finalNumbers = triedCards,
                    correctPosCount = numOfCorrectPos,
                    wrongPosCount = numOfWrongPos
                });
            }
        }

        public int GetTargetCardAtIndex(int index)
        {
            return _targetCards[index];
        }

        private void CalculatePositionCounts(List<int> finalCards, out int numOfCorrectPos, out int numOfWrongPos)
        {
            numOfCorrectPos = 0;
            numOfWrongPos = 0;
            
            for (int i = 0; i < finalCards.Count; i++)
            {
                for (int j = 0; j < _targetCards.Count; j++)
                {
                    if (finalCards[i] == _targetCards[j])
                    {
                        if (i == j) numOfCorrectPos++;
                        else numOfWrongPos++;
                    }
                }
            }
        }

        public void CheckFinalCards(List<int> finalCards)
        {
            if (finalCards.Count != _targetCards.Count)
            {
                Debug.LogError("Final number size and target number size are not equal.");
                return;
            }
            
            //TODO: check _triedCardList contains finalCardList
            _triedCardsList.Add(finalCards);
            CalculatePositionCounts(finalCards, out int numOfCorrectPos, out int numOfWrongPos);
            DetermineAction(finalCards, numOfCorrectPos, numOfWrongPos);
        }

        private void DetermineAction(List<int> finalCards, int numOfCorrectPos, int numOfWrongPos)
        {
            if (numOfCorrectPos == _numOfBoardHolders)
            {
                ResultBlockAddition?.Invoke(this, new ResultBlockModel()
                {
                    finalNumbers = finalCards,
                    correctPosCount = numOfCorrectPos,
                    wrongPosCount = numOfWrongPos
                });
                NumberGuessed.Invoke(this, new NumberGuessedEventArgs()
                {
                    finalCardNumbers = finalCards,
                    targetCardNumbers = _targetCards,
                    isGuessRight = true,
                });
            }
            else
            {
                ResultBlockAddition?.Invoke(this, new ResultBlockModel()
                {
                    finalNumbers = finalCards,
                    correctPosCount = numOfCorrectPos,
                    wrongPosCount = numOfWrongPos
                });
                NumberGuessed.Invoke(this, new NumberGuessedEventArgs()
                {
                    finalCardNumbers = finalCards,
                    targetCardNumbers = _targetCards,
                    isGuessRight = false,
                });
            }
        }
        
        public List<List<int>> GetTriedCardsList()
        {
            return _triedCardsList;
        }

        public List<int> GetTargetCards()
        {
            return _targetCards;
        }
    }
    
    public class NumberGuessedEventArgs : EventArgs
    {
        public List<int> finalCardNumbers;
        public List<int> targetCardNumbers;
        public bool isGuessRight;
    }

    public interface IResultManager
    {
        void Initialize(ILevelTracker levelTracker, ITargetNumberCreator targetNumberCreator, ILevelDataCreator levelDataCreator);
        int GetTargetCardAtIndex(int index);
        void CheckFinalCards(List<int> finalCards);
        event EventHandler<ResultBlockModel> ResultBlockAddition;
        event EventHandler<NumberGuessedEventArgs> NumberGuessed;
        List<List<int>> GetTriedCardsList();
        List<int> GetTargetCards();
    }
}
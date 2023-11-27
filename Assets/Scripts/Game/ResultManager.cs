using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Scripts
{
    public class ResultManager : IResultManager
    {
        private List<int> _targetCards = new List<int>();
        private List<List<int>> _triedCardsList = new List<List<int>>();
        private ILevelTracker _levelTracker;
        public event EventHandler<ResultBlockModel> ResultBlockAddition;
        public event EventHandler<NumberGuessedEventArgs> NumberGuessed;
        
        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            _targetCards = _levelTracker.GetLevelInfo().levelSaveData.TargetCards;
        }

        public int GetTargetCardAtIndex(int index)
        {
            return _targetCards[index];
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
            
            int numOfCorrectPos = 0;
            int numOfWrongPos = 0;
            
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
            DetermineAction(finalCards, numOfCorrectPos, numOfWrongPos);
        }

        private void DetermineAction(List<int> finalCards, int numOfCorrectPos, int numOfWrongPos)
        {
            if (numOfCorrectPos == _levelTracker.GetLevelInfo().levelData.NumOfBoardHolders)
            {
                NumberGuessed.Invoke(this, new NumberGuessedEventArgs()
                {
                    isGuessRight = true,
                });
            }
            else
            {
                ResultBlockAddition?.Invoke(this, new ResultBlockModel()
                {
                    finalNumbers = finalCards,
                    resultModels = CreateResultModelList(numOfCorrectPos, numOfWrongPos)
                });
                NumberGuessed.Invoke(this, new NumberGuessedEventArgs()
                {
                    isGuessRight = false,
                });
            }
        }

        private List<ResultModel> CreateResultModelList(int numOfCorrectPos, int numOfWrongPos)
        {
            List<ResultModel> resultModels = new List<ResultModel>();
            if (numOfCorrectPos > 0)
            {
                resultModels.Add(new ResultModel()
                {
                    number = numOfCorrectPos,
                    color = ConstantValues.GetCardPositionToColorMapping()[CardPositionCorrectness.Correct]
                });
            }

            if (numOfWrongPos > 0)
            {
                resultModels.Add(new ResultModel()
                {
                    number = numOfWrongPos,
                    color = ConstantValues.GetCardPositionToColorMapping()[CardPositionCorrectness.Wrong]
                });
            }

            int numOfNonExistentCards = _levelTracker.GetLevelInfo().levelData.NumOfBoardHolders - numOfCorrectPos - numOfWrongPos;
            
            if (numOfNonExistentCards > 0)
            {
                resultModels.Add(new ResultModel()
                {
                    number = numOfNonExistentCards,
                    color = ConstantValues.GetCardPositionToColorMapping()[CardPositionCorrectness.NotExisted]
                });
            }

            return resultModels;
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
        public bool isGuessRight;
    }

    public interface IResultManager
    {
        void Initialize(ILevelTracker levelTracker);
        int GetTargetCardAtIndex(int index);
        void CheckFinalCards(List<int> finalCards);
        event EventHandler<ResultBlockModel> ResultBlockAddition;
        event EventHandler<NumberGuessedEventArgs> NumberGuessed;
        List<List<int>> GetTriedCardsList();
        List<int> GetTargetCards();
    }
}
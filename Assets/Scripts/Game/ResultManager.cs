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
        public event EventHandler<BackFlipCorrectCardsEventArgs> BackFlipCorrectCards;
        
        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            _targetCards = _levelTracker.GetLevelInfo().levelSaveData.TargetCards;
            _triedCardsList = _levelTracker.GetLevelInfo().levelSaveData.TriedCardsList;
            foreach (List<int> triedCards in _triedCardsList)
            {
                GetNumOfPositions(triedCards, out int numOfCorrectPos, out int numOfWrongPos);
                ResultBlockAddition?.Invoke(this, new ResultBlockModel()
                {
                    finalNumbers = triedCards,
                    resultModels = CreateResultModelList(numOfCorrectPos, numOfWrongPos)
                });
            }
        }

        public int GetTargetCardAtIndex(int index)
        {
            return _targetCards[index];
        }

        private void GetNumOfPositions(List<int> finalCards, out int numOfCorrectPos, out int numOfWrongPos)
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
            GetNumOfPositions(finalCards, out int numOfCorrectPos, out int numOfWrongPos);
            DetermineAction(finalCards, numOfCorrectPos, numOfWrongPos);
        }

        private void DetermineAction(List<int> finalCards, int numOfCorrectPos, int numOfWrongPos)
        {
            if (numOfCorrectPos == _levelTracker.GetLevelInfo().levelData.NumOfBoardHolders)
            {
                BackFlipCorrectCards.Invoke(this, new BackFlipCorrectCardsEventArgs()
                {
                    finalCardNumbers = finalCards,
                    onComplete = () => NumberGuessed.Invoke(this, new NumberGuessedEventArgs()
                    {
                        isGuessRight = true,
                    })
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
                    cardPositionCorrectness = CardPositionCorrectness.Correct
                });
            }

            if (numOfWrongPos > 0)
            {
                resultModels.Add(new ResultModel()
                {
                    number = numOfWrongPos,
                    cardPositionCorrectness = CardPositionCorrectness.Wrong
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
    
    public class BackFlipCorrectCardsEventArgs : EventArgs
    {
        public List<int> finalCardNumbers;
        public Action onComplete;
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
        event EventHandler<BackFlipCorrectCardsEventArgs> BackFlipCorrectCards;
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Scripts
{
    public class ResultManager : IResultManager
    {
        private List<int> _targetCardList = new List<int>();
        private ILevelTracker _levelTracker;
        public event EventHandler<ResultBlockModel> ResultBlockAddition;
        public event EventHandler<NumberGuessedEventArgs> NumberGuessed;
        
        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            _targetCardList = GetTargetCardList(_levelTracker.GetLevelData().NumOfCards,
                _levelTracker.GetLevelData().NumOfBoardHolders);
        }
        
        private List<int> GetTargetCardList(int numOfCards, int numOfBoardHolders)
        {
            List<int> cardList = Enumerable.Range(1, numOfCards).ToList();
            Random random = new Random();
            for (int i = numOfCards - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                (cardList[i], cardList[j]) = (cardList[j], cardList[i]);
            }

            return cardList.Take(numOfBoardHolders).ToList();
        }

        public void CheckFinalCardList(List<int> finalCardList)
        {
            if (finalCardList.Count != _targetCardList.Count)
            {
                Debug.LogError("Final number size and target number size are not equal.");
                return;
            }
            
            int numOfCorrectPos = 0;
            int numOfWrongPos = 0;
            
            for (int i = 0; i < finalCardList.Count; i++)
            {
                for (int j = 0; j < _targetCardList.Count; j++)
                {
                    if (finalCardList[i] == _targetCardList[j])
                    {
                        if (i == j) numOfCorrectPos++;
                        else numOfWrongPos++;
                    }
                }
            }
            DetermineAction(finalCardList, numOfCorrectPos, numOfWrongPos);
        }

        private void DetermineAction(List<int> finalCardList, int numOfCorrectPos, int numOfWrongPos)
        {
            if (numOfCorrectPos == _levelTracker.GetLevelData().NumOfBoardHolders)
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
                    finalNumbers = finalCardList,
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

            int numOfNonExistentCards = _levelTracker.GetLevelData().NumOfBoardHolders - numOfCorrectPos - numOfWrongPos;
            
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
    }
    
    public class NumberGuessedEventArgs : EventArgs
    {
        public bool isGuessRight;
    }

    public interface IResultManager
    {
        void Initialize(ILevelTracker levelTracker);
        void CheckFinalCardList(List<int> finalCardList);
        event EventHandler<ResultBlockModel> ResultBlockAddition;
        event EventHandler<NumberGuessedEventArgs> NumberGuessed;
    }
}
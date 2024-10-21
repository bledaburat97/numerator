using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultManager : IResultManager
    {
        private IResultAreaController _resultAreaController;
        private ILevelSaveDataManager _levelSaveDataManager;
        private ITargetNumberCreator _targetNumberCreator;
        private IBoardCardIndexManager _boardCardIndexManager;
        
        private List<List<int>> _triedCardsList = new List<List<int>>();
        public event EventHandler LevelSuccessEvent;
        public event EventHandler WrongGuessEvent;

        [Inject]
        public ResultManager(IGameUIController gameUIController, 
            IResultAreaController resultAreaController, ILevelSaveDataManager levelSaveDataManager, ITargetNumberCreator targetNumberCreator,
            IBoardCardIndexManager boardCardIndexManager)
        {
            gameUIController.CheckFinalNumbers += CheckFinalCards;
            _resultAreaController = resultAreaController;
            _levelSaveDataManager = levelSaveDataManager;
            _targetNumberCreator = targetNumberCreator;
            _boardCardIndexManager = boardCardIndexManager;
        }

        public void TryAddTriedCards()
        {
            _triedCardsList = _levelSaveDataManager.GetLevelSaveData().TriedCardsList;
            foreach (List<int> triedCards in _triedCardsList)
            {
                CalculatePositionCounts(triedCards, out int numOfCorrectPos, out int numOfWrongPos);
                _resultAreaController.AddResultBlock(new ResultBlockModel()
                {
                    finalNumbers = triedCards,
                    correctPosCount = numOfCorrectPos,
                    wrongPosCount = numOfWrongPos
                });
            }
        }

        private void CalculatePositionCounts(List<int> finalCards, out int numOfCorrectPos, out int numOfWrongPos)
        {
            numOfCorrectPos = 0;
            numOfWrongPos = 0;
            
            for (int i = 0; i < finalCards.Count; i++)
            {
                for (int j = 0; j < _targetNumberCreator.GetTargetCardsList().Count; j++)
                {
                    if (finalCards[i] == _targetNumberCreator.GetTargetCardsList()[j])
                    {
                        if (i == j) numOfCorrectPos++;
                        else numOfWrongPos++;
                    }
                }
            }
        }

        private void CheckFinalCards(object sender, EventArgs args)
        {
            if (_boardCardIndexManager.GetEmptyBoardHolderIndexList().Count != 0) return;
            List<int> finalCardIndexes = _boardCardIndexManager.GetCardIndexesOnBoard();
            List<int> finalCards = new List<int>();
            for (int i = 0; i < finalCardIndexes.Count; i++)
            {
                if (finalCardIndexes[i] == -1)
                {
                    return;
                }
                finalCards.Add(finalCardIndexes[i] + 1);
            }
            if (finalCards.Count != _targetNumberCreator.GetTargetCardsList().Count)
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
            _resultAreaController.AddResultBlock(new ResultBlockModel()
            {
                finalNumbers = finalCards,
                correctPosCount = numOfCorrectPos,
                wrongPosCount = numOfWrongPos
            });
            if (numOfCorrectPos == finalCards.Count)
            {
                LevelSuccessEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                WrongGuessEvent?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public List<List<int>> GetTriedCardsList()
        {
            return _triedCardsList;
        }
    }

    public interface IResultManager
    {
        event EventHandler LevelSuccessEvent;
        event EventHandler WrongGuessEvent;
        List<List<int>> GetTriedCardsList();
        void TryAddTriedCards();
    }
}
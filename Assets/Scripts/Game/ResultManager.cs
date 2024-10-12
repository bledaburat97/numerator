using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultManager : IResultManager
    {
        private IBoardAreaController _boardAreaController;
        private IResultAreaController _resultAreaController;
        private ILevelSaveDataManager _levelSaveDataManager;
        private ITargetNumberCreator _targetNumberCreator;
        private ILevelDataCreator _levelDataCreator;
        
        private List<int> _targetCards = new List<int>();
        private List<List<int>> _triedCardsList = new List<List<int>>();
        private int _numOfBoardHolders;
        public event EventHandler LevelSuccessEvent;
        public event EventHandler WrongGuessEvent;

        [Inject]
        public ResultManager(IGameUIController gameUIController, IBoardAreaController boardAreaController, IResultAreaController resultAreaController, ILevelSaveDataManager levelSaveDataManager, ITargetNumberCreator targetNumberCreator, ILevelDataCreator levelDataCreator)
        {
            gameUIController.CheckFinalNumbers += CheckFinalCards;
            _boardAreaController = boardAreaController;
            _resultAreaController = resultAreaController;
            _levelSaveDataManager = levelSaveDataManager;
            _targetNumberCreator = targetNumberCreator;
            _levelDataCreator = levelDataCreator;
        }
        
        public void Initialize(int removedBoardHolderCount)
        {
            _numOfBoardHolders = _levelDataCreator.GetLevelData().NumOfBoardHolders - removedBoardHolderCount;
            _targetCards = _targetNumberCreator.GetTargetCardsList();
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

        private void CheckFinalCards(object sender, EventArgs args)
        {
            if (_boardAreaController.GetEmptyBoardHolderIndexList().Count != 0) return;
            List<int> finalCards = _boardAreaController.GetFinalNumbers();
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
            _resultAreaController.AddResultBlock(new ResultBlockModel()
            {
                finalNumbers = finalCards,
                correctPosCount = numOfCorrectPos,
                wrongPosCount = numOfWrongPos
            });
            if (numOfCorrectPos == _numOfBoardHolders)
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
        void Initialize(int removedBoardHolderCount);
        event EventHandler LevelSuccessEvent;
        event EventHandler WrongGuessEvent;
        List<List<int>> GetTriedCardsList();
    }
}
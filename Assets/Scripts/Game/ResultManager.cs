using System;
using System.Collections.Generic;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultManager : IResultManager
    {
        private readonly IResultAreaController _resultAreaController;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IBoardPlacementQuery _boardPlacementQuery;
        private readonly IRoundStateManager _roundStateManager;
        
        public event EventHandler LevelSuccessEvent;
        public event EventHandler WrongGuessEvent;

        [Inject]
        public ResultManager(IGameUIController gameUIController, 
            IResultAreaController resultAreaController, ITargetNumberCreator targetNumberCreator,
            IBoardPlacementQuery boardPlacementQuery, IRoundStateManager roundStateManager)
        {
            gameUIController.CheckFinalNumbers += CheckFinalCards;
            _resultAreaController = resultAreaController;
            _targetNumberCreator = targetNumberCreator;
            _boardPlacementQuery = boardPlacementQuery;
            _roundStateManager = roundStateManager;
        }

        public void TryAddTriedCards()
        {
            foreach (List<int> triedCards in _roundStateManager.GetTriedCardsList())
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
            IReadOnlyList<int> finalCardIndexes = _boardPlacementQuery.GetPlacedCardIndexesOnBoard();
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
            
            _roundStateManager.AddTriedCards(finalCards);
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
    }

    public interface IResultManager
    {
        event EventHandler LevelSuccessEvent;
        event EventHandler WrongGuessEvent;
        void TryAddTriedCards();
    }
}

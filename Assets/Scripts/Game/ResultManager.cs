using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultManager : IResultManager
    {
        [InjectOptional] private ILevelTracker _levelTracker;
        [InjectOptional] private IMultiplayerGameController _multiplayer;
        [InjectOptional] private BoardSplitService _split;
        [InjectOptional] private IceBoardService _ice;
        private readonly IResultAreaController _resultAreaController;
        private readonly ITargetNumberCreator _targetNumberCreator;
        private readonly IBoardPlacementQuery _boardPlacementQuery;
        private readonly IRoundStateManager _roundStateManager;
        private readonly IGamePowerUpAreaController _gamePowerUpAreaController;

        public event EventHandler LevelSuccessEvent;
        public event EventHandler WrongGuessEvent;
        public event EventHandler<GuessEvaluation> GuessResolved;

        [Inject]
        public ResultManager(IGameUIController gameUIController, 
            IResultAreaController resultAreaController, ITargetNumberCreator targetNumberCreator,
            IBoardPlacementQuery boardPlacementQuery, IRoundStateManager roundStateManager,
            IGamePowerUpAreaController gamePowerUpAreaController)
        {
            gameUIController.CheckFinalNumbers += CheckFinalCards;
            _resultAreaController = resultAreaController;
            _targetNumberCreator = targetNumberCreator;
            _boardPlacementQuery = boardPlacementQuery;
            _roundStateManager = roundStateManager;
            _gamePowerUpAreaController = gamePowerUpAreaController;
        }

        public void TryAddTriedCards()
        {
            foreach (List<int> triedCards in _roundStateManager.GetTriedCardsList())
            {
                var evaluation = Evaluate(0,triedCards);
                _resultAreaController.AddResultBlock(CreateRow(triedCards,evaluation),false);
            }
        }

        private GuessEvaluation Evaluate(int number,List<int> cards)
        {
            var answer = number == 0 ? _targetNumberCreator.GetHistoryTargetCards() : _targetNumberCreator.GetTargetCardsList();
            var submission=GuessSubmission.FromHistory(cards);
            return _split != null ? _split.Evaluate(number,submission,answer) : new GuessEvaluation(number,submission,answer);
        }

        private static ResultBlockModel CreateRow(List<int> cards,GuessEvaluation evaluation) => new ResultBlockModel {
            finalNumbers = cards, correctPosCount = evaluation.Correct, wrongPosCount = evaluation.Misplaced, groups = evaluation.Groups
        };

        private void CheckFinalCards(object sender, EventArgs args)
        {
            IReadOnlyList<int> finalCardIndexes = _boardPlacementQuery.GetPlacedCardIndexesOnBoard();
            List<int> finalCards = new List<int>();
            for (int i = 0; i < finalCardIndexes.Count; i++)
            {
                if (_ice?.IsBlocked(i)==true) { finalCards.Add(GuessSubmission.FrozenMarker);continue; }
                if (finalCardIndexes[i] == -1)
                {
                    return;
                }
                finalCards.Add(finalCardIndexes[i] + 1);
            }
            if (_levelTracker != null && _levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _multiplayer?.SubmitGuess(finalCards.ToArray());
                return;
            }
            if (_roundStateManager.GetRemainingGuessCount() <= 0) return;
            if (finalCards.Count != _targetNumberCreator.GetTargetCardsList().Count)
            {
                Debug.LogError("Final number size and target number size are not equal.");
                return;
            }

            var submission=_ice?.IsActive==true?_ice.Capture(finalCards):GuessSubmission.FullBoard(finalCards,_targetNumberCreator.GetTargetCardsList().Count);
            var evaluation=_split!=null?_split.Evaluate(_roundStateManager.GetTriedCardsList().Count+1,submission,_targetNumberCreator.GetTargetCardsList())
                :new GuessEvaluation(_roundStateManager.GetTriedCardsList().Count+1,submission,_targetNumberCreator.GetTargetCardsList());
            _roundStateManager.AddTriedCards(finalCards);
            GameTelemetry.GuessAccepted(_roundStateManager.GetTriedCardsList().Count);
            _gamePowerUpAreaController.Refresh();
            DetermineAction(finalCards, evaluation);
            GuessResolved?.Invoke(this, evaluation);
            _gamePowerUpAreaController.Refresh();
        }

        private void DetermineAction(List<int> finalCards, GuessEvaluation evaluation)
        {
            _resultAreaController.AddResultBlock(CreateRow(finalCards,evaluation));
            if (evaluation.Won)
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
        event EventHandler<GuessEvaluation> GuessResolved;
        void TryAddTriedCards();
    }
}

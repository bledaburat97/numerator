using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultManager : IResultManager,IDisposable
    {
        private readonly IGameUIController _ui;
        [InjectOptional] private ILevelTracker _levelTracker;
        [InjectOptional] private IMultiplayerGameController _multiplayer;
        [InjectOptional] private BoardSplitService _split;
        [InjectOptional] private IceBoardService _ice;
        [InjectOptional] private LazyInject<SinglePlayerTurnFlow> _turnFlow;
        private SinglePlayerTurnFlow _configuredFlow;
        private bool _processing;
        public void ConfigureTurnFlow(SinglePlayerTurnFlow flow)=>_configuredFlow=flow;
        public SinglePlayerTurnFlow TurnFlow=>_configuredFlow??_turnFlow?.Value;
        private readonly IResultAreaController _resultAreaController;
        private readonly RoundHistory _history;
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
            IGamePowerUpAreaController gamePowerUpAreaController,RoundHistory history)
        {
            _ui=gameUIController;
            gameUIController.CheckFinalNumbers += CheckFinalCards;
            _resultAreaController = resultAreaController;_history=history;
            _targetNumberCreator = targetNumberCreator;
            _boardPlacementQuery = boardPlacementQuery;
            _roundStateManager = roundStateManager;
            _gamePowerUpAreaController = gamePowerUpAreaController;
        }

        public void TryAddTriedCards()
        {
            _history.Clear();
            foreach (List<int> triedCards in _roundStateManager.GetTriedCardsList())
            {
                var evaluation = Evaluate(0,triedCards);
                _history.Add(CreateRow(triedCards,evaluation));
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
            if(_processing)return;
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
            var flow=TurnFlow??throw new InvalidOperationException("Single-player turn flow is not configured.");
            _processing=true;
            try
            {
                using(var notifications=ObserverNotification.Defer())
                {
                    _roundStateManager.AddTriedCards(finalCards);
                    _resultAreaController.AddResultBlock(CreateRow(finalCards,evaluation));
                    flow.Process(evaluation);
                    ObserverNotification.Present(()=>{GameTelemetry.GuessAccepted(evaluation.Number);_gamePowerUpAreaController.Refresh();});
                    if(evaluation.Won)ObserverNotification.Send(LevelSuccessEvent,this,EventArgs.Empty);
                    else ObserverNotification.Send(WrongGuessEvent,this,EventArgs.Empty);
                    ObserverNotification.Send(GuessResolved,this,evaluation);
                    notifications.Complete();
                }
            }
            finally{_processing=false;}
        }
        public void Dispose()=>_ui.CheckFinalNumbers-=CheckFinalCards;
    }
    public interface ISinglePlayerOutcomeSink {void FinishRound(bool won);}
    // Executes rules explicitly. Events describe a completed turn; they do not advance it.
    public sealed class SinglePlayerTurnFlow
    {
        private readonly IRoundStateManager _round;private readonly IGuessManager _guesses;
        public IHintProvider Hints {get;set;}
        [InjectOptional] private LazyInject<ISinglePlayerOutcomeSink> _outcome;
        public LevelMechanicsController Mechanics {get;set;}
        public ISinglePlayerOutcomeSink Outcome {get;set;}
        public SinglePlayerTurnFlow(IRoundStateManager round,IGuessManager guesses,IHintProvider hints,LevelMechanicsController mechanics=null)
        {_round=round;_guesses=guesses;Hints=hints;Mechanics=mechanics;}
        public void Process(GuessEvaluation guess)
        {
            var loss=guess.Won?null:_guesses.ConsumeWrongGuess();
            Mechanics?.ApplyGuess(guess);
            bool failed=!guess.Won && _round.GetRemainingGuessCount()<=0;
            var hint=loss!=null && loss.Crystal && !failed?Hints?.PrepareHint(loss.CanReveal):null;
            Mechanics?.CompleteTurn(guess);
            if(failed){if(loss!=null)loss.Remaining=0;_guesses.FailRound();}
            _guesses.PresentWrongGuess(loss,hint);
            if(guess.Won || failed)(Outcome??_outcome?.Value)?.FinishRound(guess.Won);
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

using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private ILevelTracker _levelTracker;
        private ITurnOrderDeterminer _turnOrderDeterminer;
        private IResultAreaView _view;
        private List<IResultBlockController> _resultBlockControllers;
        
        [Inject]
        public ResultAreaController(ILevelTracker levelTracker, ITurnOrderDeterminer turnOrderDeterminer, IResultAreaView view)
        {
            _levelTracker = levelTracker;
            _turnOrderDeterminer = turnOrderDeterminer;
            _view = view;
            _resultBlockControllers = new List<IResultBlockController>();
        }

        public void Initialize(bool isNewGame)
        {
            if (isNewGame) RemoveResultBlocks();
            _view.SetEmptyState(_resultBlockControllers.Count == 0);
            if (isNewGame)
            {
                _view.GetCanvasGroup().alpha = 0f;
            }
            else
            {
                _view.GetCanvasGroup().alpha = 1f;
            }
        }

        public Sequence ChangeFade(float duration, float finalAlpha)
        {
            return DOTween.Sequence().Append(_view.GetCanvasGroup().DOFade(finalAlpha, duration));
        }

        public void EnableReview() => _view.EnableReview();
        public void LabelLatestMultiplayerGuess(bool isLocal, string playerName = null)
        {
            if (_resultBlockControllers.Count > 0) (_resultBlockControllers[_resultBlockControllers.Count - 1] as ResultBlockController)?.SetPlayerIdentity(isLocal, playerName);
            _view.SetScrollPositionToBottom();
        }

        public void RevealAnswer(IReadOnlyList<int> answer)
        {
            foreach (IResultBlockController row in _resultBlockControllers) row.RevealAnswer(answer);
            EnableReview();
        }

        public void RevealGuess(GuessEvaluation guess)
        {
            if (guess.Number > 0 && guess.Number <= _resultBlockControllers.Count)
                (_resultBlockControllers[guess.Number - 1] as ResultBlockController)?.RevealDetails(guess.States);
        }

        public void AddResultBlock(ResultBlockModel resultBlockModel)
        {
            IResultBlockController resultBlockController = new ResultBlockController();
            resultBlockController.Initialize(_view.CreateResultBlock(), resultBlockModel, _view.GetWidth(), _view.GetRowHeight());
            _resultBlockControllers.Add(resultBlockController);

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer || _turnOrderDeterminer.IsLocalTurn())
            {
                _view.SetScrollPositionToBottom();
            }
        }
        // Compatibility for existing numeric callers; gameplay uses the card-list model directly.
        public void AddNewResultBlock(int finalNumber, int correctPosCount, int wrongPosCount)
        {
            var cards = new List<int>();
            while (finalNumber != 0) { cards.Add(finalNumber % 10); finalNumber /= 10; }
            AddResultBlock(new ResultBlockModel { finalNumbers = cards, correctPosCount = correctPosCount, wrongPosCount = wrongPosCount });
        }

        public ResultAreaInfo GetResultAreaInfo()
        {
            return _view.GetResultAreaInfo();
        }

        public void RemoveResultBlocks()
        {
            while (_resultBlockControllers.Count > 0)
            {
                _resultBlockControllers[0].DestroyResultBlock();
                _resultBlockControllers[0] = null;
                _resultBlockControllers.Remove(_resultBlockControllers[0]);
            }
        }
    }

    public interface IResultAreaController
    {
        ResultAreaInfo GetResultAreaInfo();
        void AddNewResultBlock(int finalNumber, int correctPosCount, int wrongPosCount);
        void Initialize(bool isNewGame);
        void AddResultBlock(ResultBlockModel resultBlockModel);
        Sequence ChangeFade(float duration, float finalAlpha);
        void RemoveResultBlocks();
        void EnableReview();
        void RevealAnswer(IReadOnlyList<int> answer);
        void RevealGuess(GuessEvaluation guess);
    }
}

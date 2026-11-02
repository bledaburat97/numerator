using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class ResultAreaController : IResultAreaController
    {
        private IResultAreaView _view;
        private readonly List<ResultBlockModel> _history = new List<ResultBlockModel>();
        public int HistoryCount => _history.Count;
        public ResultBlockModel GetHistoryEntry(int index) => _history[index];
        
        [Inject]
        public ResultAreaController(IResultAreaView view)
        {
            _view = view;
            _view.SetHistory(_history);
        }

        public void Initialize(bool isNewGame)
        {
            if (isNewGame) RemoveResultBlocks();
            _view.SetEmptyState(_history.Count == 0);
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
            if (_history.Count > 0)
            {
                var row = _history[_history.Count - 1];
                row.isLocalGuess = isLocal; row.playerName = playerName;
                _view.RefreshRow(_history.Count - 1);
            }
            _view.SetScrollPositionToBottom();
        }

        public void RevealAnswer(IReadOnlyList<int> answer)
        {
            for (int i = 0; i < _history.Count; i++)
            {
                var row = _history[i];
                row.reviewStates = new GuessEvaluation(0, row.finalNumbers, answer).States;
            }
            _view.RefreshVisibleRows();
            EnableReview();
        }

        public void RevealGuess(GuessEvaluation guess)
        {
            if (guess.Number <= 0 || guess.Number > _history.Count) return;
            _history[guess.Number - 1].hintStates = guess.States;
            _view.RefreshRow(guess.Number - 1);
        }

        public void AddResultBlock(ResultBlockModel resultBlockModel)
        {
            _history.Add(resultBlockModel.Snapshot());
            _view.HistoryChanged();

            _view.SetScrollPositionToBottom();
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
            _history.Clear();
            _view.HistoryChanged();
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

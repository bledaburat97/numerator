using System;
using System.Collections.Generic;

namespace Scripts
{
    public class ResultBlockController : IResultBlockController
    {
        private IResultBlockView _view;
        private List<int> _numbers;

        public void Initialize(IResultBlockView view, ResultBlockModel model, float resultAreaWidth, float rowHeight)
        {
            _view = view;
            _numbers = new List<int>(model.finalNumbers);
            view.Init(new NonDraggableCardItemViewFactory(), resultAreaWidth, rowHeight);
            foreach (int number in model.finalNumbers)
            {
                INonDraggableCardItemView card = view.CreateCardItem();
                card.Init(number);
                card.InitLocalScale();
            }
            view.DisplayFeedback(model.correctPosCount, model.wrongPosCount);
        }

        public void DestroyResultBlock() => _view.Destroy();
        public void SetPlayerIdentity(bool isLocal, string playerName = null) => (_view as ResultBlockView)?.SetPlayerIdentity(isLocal, playerName);

        public void RevealAnswer(IReadOnlyList<int> answer)
        {
            var remaining = new Dictionary<int, int>();
            var states = new CardResultState[_numbers.Count];
            for (int i = 0; i < answer.Count; i++)
            {
                if (i < _numbers.Count && _numbers[i] == answer[i]) states[i] = CardResultState.Correct;
                else remaining[answer[i]] = remaining.TryGetValue(answer[i], out int count) ? count + 1 : 1;
            }
            for (int i = 0; i < _numbers.Count; i++)
            {
                if (states[i] != CardResultState.Correct)
                {
                    bool found = remaining.TryGetValue(_numbers[i], out int count) && count > 0;
                    states[i] = found ? CardResultState.Misplaced : CardResultState.Absent;
                    if (found) remaining[_numbers[i]]--;
                }
                _view.SetCardResult(i, states[i]);
            }
        }
    }

    public interface IResultBlockController
    {
        void Initialize(IResultBlockView view, ResultBlockModel model, float resultAreaWidth, float rowHeight);
        void DestroyResultBlock();
        void RevealAnswer(IReadOnlyList<int> answer);
    }

    public class ResultBlockModel : EventArgs
    {
        public List<int> finalNumbers;
        public int correctPosCount;
        public int wrongPosCount;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

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
            if (view is ResultBlockView row) row.ConfigureGroups(model.groups);
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

        public void RevealDetails(IReadOnlyList<CardResultState> states)
        {
            if (_view is ResultBlockView view) view.RevealDetails(states);
        }

        public void RevealAnswer(IReadOnlyList<int> answer)
        {
            var evaluation = new GuessEvaluation(0, _numbers, answer);
            for (int i = 0; i < evaluation.States.Count; i++) _view.SetCardResult(i, evaluation.States[i]);
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
        public IReadOnlyList<GuessGroupFeedback> groups;
        public bool? isLocalGuess;
        public string playerName;
        public IReadOnlyList<CardResultState> hintStates;
        public IReadOnlyList<CardResultState> reviewStates;

        public ResultBlockModel Snapshot() => new ResultBlockModel {
            finalNumbers = new List<int>(finalNumbers), correctPosCount = correctPosCount, wrongPosCount = wrongPosCount,
            groups = groups?.ToArray(), isLocalGuess = isLocalGuess, playerName = playerName,
            hintStates = hintStates?.ToArray(), reviewStates = reviewStates?.ToArray()
        };
    }
}

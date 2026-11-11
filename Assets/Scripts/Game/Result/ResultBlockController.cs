using System;
using System.Collections.Generic;
using System.Linq;

namespace Scripts
{
    public class ResultBlockController : IResultBlockController
    {
        private IResultBlockView _view;
        private ResultBlockModel _model;
        private float _width,_height;
        public void Initialize(IResultBlockView view,ResultBlockModel model,float width,float height)
        {_view=view;_model=model.Snapshot();_width=width;_height=height;Render();}
        private void Render()=>_view.Bind(_model,_width,_height);
        public void DestroyResultBlock()=>_view.Destroy();
        public void SetPlayerIdentity(bool local,string name=null){_model.isLocalGuess=local;_model.playerName=name;Render();}
        public void RevealDetails(IReadOnlyList<CardResultState> states){_model.hintStates=states.ToArray();Render();}
        public void RevealAnswer(IReadOnlyList<int> answer)
        {
            var partition=_model.groups?.Count==2?BoardPartition.ForCount(_model.finalNumbers.Count):null;
            _model.reviewStates=new GuessEvaluation(0,GuessSubmission.FromHistory(_model.finalNumbers),answer,partition).States;Render();
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

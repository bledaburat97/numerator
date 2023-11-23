using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class ResultBlockController : IResultBlockController
    {
        private IResultBlockView _view;
        private ResultBlockModel _model;
        public void Initialize(IResultBlockView view, ResultBlockModel model)
        {
            _view = view;
            _model = model;
            _view.Init(new NonDraggableCardItemViewFactory(), new ResultViewFactory());
            CreateCardItems();
            CreateResults();
        }

        private void CreateResults()
        {
            _view.SetResultHolderLocalPosition(_model.finalNumbers.Count, ConstantValues.RESULT_CARD_WIDTH);
            for (int i = 0; i < _model.resultModels.Count; i++)
            {
                IResultView resultView = _view.CreateResult();
                resultView.Init(_model.resultModels[i]);
            }
        }

        private void CreateCardItems()
        {
            _view.SetCardsHolderLocalPosition();
            for (int i = 0; i < _model.finalNumbers.Count; i++)
            {
                INonDraggableCardItemView cardItemView = _view.CreateCardItem();
                cardItemView.Init(_model.finalNumbers[i]);
                cardItemView.SetSize(new Vector2(ConstantValues.RESULT_CARD_WIDTH, ConstantValues.RESULT_CARD_HEIGHT));
                cardItemView.InitPosition();
                cardItemView.MultiplyPixelsPerUnit();
            }
        }
    }

    public interface IResultBlockController
    {
        void Initialize(IResultBlockView view, ResultBlockModel model);
    }
    
    public enum CardPositionCorrectness
    {
        Correct,
        Wrong,
        NotExisted
    }

    public class ResultBlockModel : EventArgs
    {
        public List<int> finalNumbers;
        public List<ResultModel> resultModels;
    }
    
    public class ResultModel
    {
        public Color color;
        public int number;
    }
}
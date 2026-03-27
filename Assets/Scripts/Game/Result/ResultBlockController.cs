using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class ResultBlockController : IResultBlockController
    {
        private IResultBlockView _view;
        private ResultBlockModel _model;
        public void Initialize(IResultBlockView view, ResultBlockModel model, float resultAreaWidth)
        {
            _view = view;
            _model = model;
            _view.Init(new NonDraggableCardItemViewFactory(), new ResultImageViewFactory(), resultAreaWidth);
            CreateCardItems();
            CreateResults();
        }

        private void CreateResults()
        {
            //_view.SetResultHolderLocalPosition();
            
            if (_model.correctPosCount + _model.wrongPosCount == 0)
            {
                return;
            }
            if (_model.correctPosCount + _model.wrongPosCount < 3)
            {
                for (int i = 0; i < _model.correctPosCount; i++)
                {
                    IResultImageView resultImage = _view.CreateResultImage(ResultHolderType.Middle);
                    resultImage.Init(CardPositionCorrectness.Correct);
                }
                
                for (int i = 0; i < _model.wrongPosCount; i++)
                {
                    IResultImageView resultImage = _view.CreateResultImage(ResultHolderType.Middle);
                    resultImage.Init(CardPositionCorrectness.Wrong);
                }
            }
            else
            {
                for (int i = 0; i < _model.correctPosCount + _model.wrongPosCount; i++)
                {
                    if (i < (_model.correctPosCount + _model.wrongPosCount) / 2)
                    {
                        if (i < _model.correctPosCount)
                        {
                            IResultImageView resultImage = _view.CreateResultImage(ResultHolderType.Top);
                            resultImage.Init(CardPositionCorrectness.Correct);
                        }
                        else
                        {
                            IResultImageView resultImage = _view.CreateResultImage(ResultHolderType.Top);
                            resultImage.Init(CardPositionCorrectness.Wrong);
                        }
                    }
                    else
                    {
                        if (i < _model.correctPosCount)
                        {
                            IResultImageView resultImage = _view.CreateResultImage(ResultHolderType.Bottom);
                            resultImage.Init(CardPositionCorrectness.Correct);
                        }
                        else
                        {
                            IResultImageView resultImage = _view.CreateResultImage(ResultHolderType.Bottom);
                            resultImage.Init(CardPositionCorrectness.Wrong);
                        }
                    }
                }
            }
        }

        private void CreateCardItems()
        {
            //_view.SetCardsHolderLocalPosition();
            for (int i = 0; i < _model.finalNumbers.Count; i++)
            {
                INonDraggableCardItemView cardItemView = _view.CreateCardItem();
                cardItemView.Init(_model.finalNumbers[i]);
                cardItemView.SetSize(new Vector2(ConstantValues.RESULT_CARD_WIDTH, ConstantValues.RESULT_CARD_HEIGHT));
                cardItemView.InitLocalScale();
                cardItemView.SetLocalPosition(Vector3.zero);
                cardItemView.MultiplyPixelsPerUnit();
            }
        }

        public void DestroyResultBlock()
        {
            _view.Destroy();
        }
    }

    public interface IResultBlockController
    {
        void Initialize(IResultBlockView view, ResultBlockModel model, float resultAreaWidth);
        void DestroyResultBlock();
    }
    
    public enum CardPositionCorrectness
    {
        Correct,
        Wrong,
    }

    public class ResultBlockModel : EventArgs
    {
        public List<int> finalNumbers;
        public int correctPosCount;
        public int wrongPosCount;
    }
}
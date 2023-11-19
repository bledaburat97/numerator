using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class ResultBlockController : IResultBlockController
    {
        private IResultBlockView _view;
        public void Initialize(IResultBlockView view, ResultBlockModel model)
        {
            _view = view;
            _view.Init(new CardItemViewFactory(), new ResultViewFactory());
            CreateCardItems(model.cardNumbers);
            CreateResults(model.cardNumbers);
        }

        private void CreateResults(List<int> cardNumbers)
        {
            _view.SetResultHolderLocalPosition(cardNumbers.Count, ConstantValues.RESULT_CARD_WIDTH);
            List<ResultModel> resultModels = ResultModelListCreator.GetResultModelList(cardNumbers);
            for (int i = 0; i < resultModels.Count; i++)
            {
                IResultView resultView = _view.CreateResult();
                resultModels[i].localPosition = Vector2.zero; //TODO: set positions
                resultView.Init(resultModels[i]);
            }
        }

        private void CreateCardItems(List<int> cardNumbers)
        {
            _view.SetCardsHolderLocalPosition();
            for (int i = 0; i < cardNumbers.Count; i++)
            {
                ICardItemView cardItemView = _view.CreateCardItem();
                cardItemView.Init(cardNumbers[i]);
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

    public class ResultBlockModel
    {
        public Vector2 localPosition;
        public List<int> cardNumbers;
    }
    
    public class ResultModel
    {
        public Vector2 localPosition;
        public Color color;
        public int number;
    }
}
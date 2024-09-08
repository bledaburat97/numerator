using UnityEngine;

namespace Scripts
{
    public class BoardCardHolderController : BaseCardHolderController, IBoardCardHolderController
    {
        public BoardCardHolderController(ICardHolderView cardHolderView, Camera cam) : base(cardHolderView, cam)
        {
        }

        public void Initialize(CardHolderModel model)
        {
            _view.Init(model);
            _view.SetOnClick(model.onClickAction);
        }
    
        public int GetIndex()
        {
            return _model.index;
        }
    
        public void SetHighlightStatus(bool status)
        {
            _view.SetHighlightStatus(status);
        }
    }

    public interface IBoardCardHolderController : IBaseCardHolderController
    {
        void Initialize(CardHolderModel model);
        int GetIndex();
        void SetHighlightStatus(bool status);
    }
}
using UnityEngine;

namespace Scripts
{
    public class BoardCardHolderController : BaseCardHolderController, IBoardCardHolderController
    {
        public override void Initialize(ICardHolderView cardHolderView, CardHolderModel model, Camera cam)
        {
            base.Initialize(cardHolderView, model, cam);
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
        int GetIndex();
        void SetHighlightStatus(bool status);
    }
}
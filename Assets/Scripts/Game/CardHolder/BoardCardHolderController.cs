using System;
using UnityEngine;

namespace Scripts
{
    public class BoardCardHolderController : IBoardCardHolderController
    {
        private IBoardHolderView _view;
        private ICardHolderPositionManager _cardHolderPositionManager;

        public BoardCardHolderController(IBoardHolderView boardHolderView, Camera cam, ICardHolderPositionManager cardHolderPositionManager)
        {
            _view = boardHolderView;
            _cardHolderPositionManager = cardHolderPositionManager;
            _view.SetCamera(cam);
        }

        public void Initialize(int index, Action onClickAction)
        {
            _view.SetLocalScale();
            _view.SetLocalPosition(_cardHolderPositionManager.GetHolderPositionList(CardHolderType.Board)[index]);
            _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
            _view.SetOnClick(onClickAction);
        }
    
        public void SetHighlightStatus(bool status)
        {
            _view.SetHighlightStatus(status);
        }
        
        public IBoardHolderView GetView()
        {
            return _view;
        }
        
        public Vector3 GetPositionOfCardHolder()
        {
            return _view.GetGlobalPosition();
        }
        
        public void DestroyObject()
        {
            _view.DestroyObject();
            _view = null;
        }
    }

    public interface IBoardCardHolderController
    {
        void Initialize(int index, Action onClickAction);
        void SetHighlightStatus(bool status);
        IBoardHolderView GetView();
        Vector3 GetPositionOfCardHolder();
        void DestroyObject();
    }
}
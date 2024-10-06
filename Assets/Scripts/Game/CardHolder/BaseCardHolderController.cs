using System;
using System.Collections.Generic;
using UnityEngine;
/*
namespace Scripts
{
    public class BaseCardHolderController : IBaseCardHolderController
    {
        protected IBaseHolderView _view;
        protected ICardHolderPositionManager _cardHolderPositionManager;
        public BaseCardHolderController(IBaseHolderView baseHolderView, Camera cam, ICardHolderPositionManager cardHolderPositionManager)
        {
            _view = baseHolderView;
            _cardHolderPositionManager = cardHolderPositionManager;
            _view.SetCamera(cam);
        }
        
        public IBaseHolderView GetView()
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

    public interface IBaseCardHolderController
    {
        IBaseHolderView GetView();
        Vector3 GetPositionOfCardHolder();
        void DestroyObject();
    }
    
    public class CardHolderModel
    {
        public int index;
        public Vector3 localPosition;
        public Vector2 size;
        public List<Vector2> possibleHolderIndicatorLocalPositionList;
        public Action onClickAction;
        public CardHolderType cardHolderType;
    }
}
*/
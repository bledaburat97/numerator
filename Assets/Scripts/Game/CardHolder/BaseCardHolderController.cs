using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class BaseCardHolderController : IBaseCardHolderController
    {
        protected ICardHolderView _view;
        protected CardHolderModel _model;

        public BaseCardHolderController(ICardHolderView cardHolderView, Camera cam)
        {
            _view = cardHolderView;
            _view.SetCamera(cam);
        }
        
        public ICardHolderView GetView()
        {
            return _view;
        }
        
        public Vector3 GetPositionOfCardHolder()
        {
            return _view.GetGlobalPosition();
        }
    }

    public interface IBaseCardHolderController
    {
        ICardHolderView GetView();
        Vector3 GetPositionOfCardHolder();
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
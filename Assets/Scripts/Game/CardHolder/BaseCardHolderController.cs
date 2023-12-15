using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class BaseCardHolderController : IBaseCardHolderController
    {
        protected ICardHolderView _view;
        protected CardHolderModel _model;

        public virtual void Initialize(ICardHolderView cardHolderView, CardHolderModel model, Camera cam)
        {
            _view = cardHolderView;
            _model = model;
            _view.Init(model, new PossibleHolderIndicatorViewFactory(), cam);
        }
        
        public ICardHolderView GetView()
        {
            return _view;
        }
    }

    public interface IBaseCardHolderController
    {
        void Initialize(ICardHolderView cardHolderView, CardHolderModel model, Camera cam);
        ICardHolderView GetView();
    }
    
    public class CardHolderModel
    {
        public int index;
        public Vector3 localPosition;
        public Vector2 size;
        public List<Vector2> possibleHolderIndicatorLocalPositionList;
        public CardItemType cardItemType;
        public Action onClickAction;
        public CardHolderType cardHolderType;
    }
}
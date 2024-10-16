using System;
using UnityEngine;

namespace Scripts
{
    public class BoardCardHolderController : IBoardCardHolderController
    {
        private IBoardHolderView _view;

        public BoardCardHolderController(IBoardHolderView boardHolderView, Camera cam)
        {
            _view = boardHolderView;
            _view.SetCamera(cam);
        }

        public void Initialize(Action onClickAction, Vector2 localPosition, float sizeRatio)
        {
            _view.SetLocalScale();
            _view.SetLocalPosition(localPosition);
            _view.SetSize(_view.GetRectTransform().sizeDelta * sizeRatio);
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
        void Initialize(Action onClickAction, Vector2 localPosition, float sizeRatio);
        void SetHighlightStatus(bool status);
        IBoardHolderView GetView();
        Vector3 GetPositionOfCardHolder();
        void DestroyObject();
    }
}
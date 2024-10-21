using System;
using DG.Tweening;
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

        public void SetSize(float sizeRatio)
        {
            _view.SetLocalScale();
            _view.SetSize(_view.GetRectTransform().sizeDelta * sizeRatio);
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            _view.SetLocalPosition(localPosition);
        }

        public Sequence Move(Vector2 targetLocalPosition, float duration)
        {
            return DOTween.Sequence().Append(_view.GetRectTransform().DOLocalMove(targetLocalPosition, duration));
        }

        public void SetOnClick(Action onClickAction)
        {
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
        void SetSize(float sizeRatio);
        void SetHighlightStatus(bool status);
        IBoardHolderView GetView();
        Vector3 GetPositionOfCardHolder();
        void DestroyObject();
        void SetLocalPosition(Vector2 localPosition);
        void SetOnClick(Action onClickAction);
        Sequence Move(Vector2 targetLocalPosition, float duration);
    }
}
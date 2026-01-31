using UnityEngine;

namespace Scripts
{
    public class BoundaryController : IBoundaryController
    {
        private IBoundaryView _view;
        private BoundaryModel _model;
        private IStarImageView _starImageView;
        
        public void Initialize(IBoundaryView view, BoundaryModel model)
        {
            _view = view;
            _model = model;
            _view.Init(_model.localPosition, new StarImageViewFactory());
        }

        public void AddStarImage(Vector2 starLocalPosition, bool isOriginal)
        {
            _starImageView = _view.CreateStarImage();
            _starImageView.SetLocalPosition(starLocalPosition);
            _starImageView.SetLocalScale(Vector3.one);
            _starImageView.SetColor(isOriginal);
            if (!isOriginal) AddMovingRewardItem();
        }

        public void AddMovingRewardItem()
        {
            if (_starImageView == null) return;
            if (_starImageView.GetMovingRewardItem() != null) return;

            _starImageView.CreateMovingRewardItem(
                new Vector2(_starImageView.GetRectTransform().rect.width * 1f, _starImageView.GetRectTransform().rect.width * 1f),
                _starImageView.GetRectTransform().rect.width * 0.7f
            );
        }

        public void SetMovingRewardItemStatus(bool status)
        {
            if (_starImageView == null) return;
            _starImageView.SetMovingRewardItemStatus(status);
        }

        public void SetStarStatus(bool status)
        {
            _starImageView.SetStarStatus(status);
        }

        public void DestroyObject()
        {
            _view.DestroyObject();
        }

        public IStarImageView GetStarImage()
        {
            return _starImageView;
        }
    }

    public interface IBoundaryController
    {
        void Initialize(IBoundaryView view, BoundaryModel model);
        void AddStarImage(Vector2 starLocalPosition, bool isOriginal);
        void SetStarStatus(bool status);
        void DestroyObject();
        IStarImageView GetStarImage();
        void AddMovingRewardItem();
        void SetMovingRewardItemStatus(bool status);
    }

    public class BoundaryModel
    {
        public Vector2 localPosition;
    }
}
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

        public void AddStarImage(Vector2 starLocalPosition)
        {
            _starImageView = _view.CreateStarImage();
            _starImageView.SetLocalPosition(starLocalPosition);
            _starImageView.SetLocalScale(Vector3.one);
        }

        public void RemoveStar()
        {
            _starImageView.SetStarStatus(false);
        }
    }

    public interface IBoundaryController
    {
        void Initialize(IBoundaryView view, BoundaryModel model);
        void AddStarImage(Vector2 starLocalPosition);
        void RemoveStar();
    }

    public class BoundaryModel
    {
        public Vector2 localPosition;
    }
}
using Game;
using UnityEngine;

namespace Scripts
{
    public class BoundaryController : IBoundaryController
    {
        private IBoundaryView _view;
        private BoundaryModel _model;
        private CrystalImageView _crystalImageView;
        
        public void Initialize(IBoundaryView view, BoundaryModel model)
        {
            _view = view;
            _model = model;
            _view.Init(_model.localPosition, new StarImageViewFactory());
        }

        public void AddCrystalImage(Vector2 localPosition, CrystalType crystalType)
        {
            _crystalImageView = _view.CreateCrystalImage();
            _crystalImageView.Init();
            _crystalImageView.SetLocalPosition(localPosition);
            _crystalImageView.SetLocalScale(Vector3.one);
            _crystalImageView.SetCrystalImage(crystalType);
        }

        public void RemoveCrystalImage()
        {
            _crystalImageView.SetStatus(false);
        }
    }

    public interface IBoundaryController
    {
        void Initialize(IBoundaryView view, BoundaryModel model);
        void AddCrystalImage(Vector2 localPosition, CrystalType crystalType);
        void RemoveCrystalImage();
    }

    public class BoundaryModel
    {
        public Vector2 localPosition;
    }
}
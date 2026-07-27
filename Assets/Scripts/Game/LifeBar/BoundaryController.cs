using UnityEngine;

namespace Scripts
{
    public class BoundaryController : IBoundaryController
    {
        private IBoundaryView _view;
        private BoundaryModel _model;
        private IRewardTokenView _rewardTokenView;
        
        public void Initialize(IBoundaryView view, BoundaryModel model)
        {
            _view = view;
            _model = model;
            _view.Init(_model.localPosition, new CoinViewFactory(), new CrystalViewFactory());
        }

        public void AddRewardToken(Vector2 tokenLocalPosition, LifeBarRewardType rewardType)
        {
            _rewardTokenView = _view.CreateRewardToken(rewardType);
            if (_rewardTokenView == null) return;

            _rewardTokenView.SetLocalPosition(tokenLocalPosition);
            _rewardTokenView.SetLocalScale(Vector3.one);
            _rewardTokenView.SetStatus(true);
        }

        public void SetRewardTokenStatus(bool status)
        {
            if (_rewardTokenView == null) return;

            _rewardTokenView.SetStatus(status);
        }

        public void DestroyObject()
        {
            _view.DestroyObject();
        }

        public IRewardTokenView GetRewardToken()
        {
            return _rewardTokenView;
        }
    }

    public interface IBoundaryController
    {
        void Initialize(IBoundaryView view, BoundaryModel model);
        void AddRewardToken(Vector2 tokenLocalPosition, LifeBarRewardType rewardType);
        void SetRewardTokenStatus(bool status);
        void DestroyObject();
        IRewardTokenView GetRewardToken();
    }

    public class BoundaryModel
    {
        public Vector2 localPosition;
    }
}

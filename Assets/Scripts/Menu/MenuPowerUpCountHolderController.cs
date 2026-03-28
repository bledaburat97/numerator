using Game;

namespace Scripts
{
    public class MenuPowerUpCountHolderController : IMenuPowerUpCountHolderController
    {
        private const float DisabledAlpha = 0.35f;
        private const float EnabledAlpha = 1f;

        private readonly IPowerUpListHolderView _view;
        private readonly ILevelTracker _levelTracker;

        public MenuPowerUpCountHolderController(IPowerUpListHolderView view, ILevelTracker levelTracker)
        {
            _view = view;
            _levelTracker = levelTracker;
        }

        public void Initialize()
        {
            if (_view == null) return;

            _view.SetRaycastStatus(false);
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null) return;

            foreach (RewardType rewardType in PowerUpTypeUtility.OrderedRewardTypes)
            {
                int count = _levelTracker.GetPowerUpCount(rewardType);
                _view.SetPowerUpCount(rewardType, count);
                _view.SetPowerUpAlpha(rewardType, count > 0 ? EnabledAlpha : DisabledAlpha);
            }
        }
    }

    public interface IMenuPowerUpCountHolderController
    {
        void Initialize();
        void Refresh();
    }
}

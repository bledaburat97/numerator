using UnityEngine;

namespace Scripts
{
    // Menu reads the same saved crystal balance and upcoming reward as the game.
    public class RewardProgressDisplayController : IRewardProgressDisplayController
    {
        private readonly IVerticalCrystalProgressView _view;
        private readonly ILevelTracker _levelTracker;

        public RewardProgressDisplayController(IVerticalCrystalProgressView view, ILevelTracker levelTracker)
        {
            _view = view;
            _levelTracker = levelTracker;
        }

        public void Initialize() => Refresh();

        public void Refresh()
        {
            if (_view == null) return;
            int capacity = Mathf.Max(1, ConstantValues.NUM_OF_STARS_FOR_WILD);
            int count = Mathf.Clamp(_levelTracker.GetCrystalProgressCount() % capacity, 0, capacity);
            _view.SetStatus(true);
            _view.SetAlpha(1f);
            _view.ShowRewardPreview(_levelTracker.GetCurrentRewardType());
            _view.InitProgress(count / (float)capacity);
        }
    }

    public interface IRewardProgressDisplayController
    {
        void Initialize();
        void Refresh();
    }
}

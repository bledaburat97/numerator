using System;
using System.Collections.Generic;
using Game;

namespace Scripts
{
    public class GamePowerUpAreaController : IGamePowerUpAreaController
    {
        private const float DisabledAlpha = 0.35f;
        private const float EnabledAlpha = 1f;

        private readonly BaseButtonControllerFactory _baseButtonControllerFactory;
        private readonly IPowerUpListHolderView _view;
        private readonly ILevelTracker _levelTracker;
        private readonly Dictionary<RewardType, IBaseButtonController> _buttonControllers;

        private bool _isInitialized;
        private bool _buttonsClickable = true;

        public event EventHandler<GameUIButtonType> PowerUpClickedEvent;

        public GamePowerUpAreaController(BaseButtonControllerFactory baseButtonControllerFactory,
            IPowerUpListHolderView view, ILevelTracker levelTracker)
        {
            _baseButtonControllerFactory = baseButtonControllerFactory;
            _view = view;
            _levelTracker = levelTracker;
            _buttonControllers = new Dictionary<RewardType, IBaseButtonController>();
        }

        public void Initialize()
        {
            if (_view == null) return;

            if (_isInitialized)
            {
                Refresh();
                return;
            }

            _view.SetRaycastStatus(true);
            CreateButtonController(RewardType.Revealing);
            CreateButtonController(RewardType.Life);
            CreateButtonController(RewardType.Bomb);
            _isInitialized = true;
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null || !_isInitialized) return;

            foreach (RewardType rewardType in PowerUpTypeUtility.OrderedRewardTypes)
            {
                int count = _levelTracker.GetPowerUpCount(rewardType);
                _view.SetPowerUpCount(rewardType, count);
                _view.SetPowerUpAlpha(rewardType, count > 0 ? EnabledAlpha : DisabledAlpha);
                _buttonControllers[rewardType].SetButtonClickable(_buttonsClickable && count > 0);
            }
        }

        public void SetButtonsClickable(bool isClickable)
        {
            _buttonsClickable = isClickable;
            Refresh();
        }

        private void CreateButtonController(RewardType rewardType)
        {
            if (_buttonControllers.ContainsKey(rewardType)) return;

            IBaseButtonView buttonView = _view.GetPowerUpButton(rewardType);
            GameUIButtonType buttonType = PowerUpTypeUtility.ToGameButtonType(rewardType);
            IBaseButtonController buttonController =
                _baseButtonControllerFactory.Create(buttonView, () => PowerUpClickedEvent?.Invoke(this, buttonType));
            _buttonControllers.Add(rewardType, buttonController);
        }
    }

    public interface IGamePowerUpAreaController
    {
        void Initialize();
        void Refresh();
        void SetButtonsClickable(bool isClickable);
        event EventHandler<GameUIButtonType> PowerUpClickedEvent;
    }
}

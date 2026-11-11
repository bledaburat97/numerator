using System;
using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class GamePowerUpAreaController : IGamePowerUpAreaController
    {
        [Inject] private LazyInject<PowerUpUseService> _uses;
        [InjectOptional] private UltraPowerUpController _ultra;
        private const float DisabledAlpha = 0.55f;
        private const float EnabledAlpha = 1f;

        private readonly BaseButtonControllerFactory _baseButtonControllerFactory;
        private readonly IPowerUpListHolderView _view;
        private readonly ILevelTracker _levelTracker;
        private readonly Dictionary<RewardType, IBaseButtonController> _buttonControllers;

        private bool _isInitialized;
        private bool _buttonsClickable = true;

        public event EventHandler<GameUIButtonType> PowerUpClickedEvent;

        [Inject]
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
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer) { _view.GetRectTransform().gameObject.SetActive(false); return; }

            if (_isInitialized)
            {
                Refresh();
                return;
            }

            _view.SetRaycastStatus(true);
            _uses.Value.Changed+=Refresh;
            foreach (var type in PowerUpTypeUtility.OrderedRewardTypes) CreateButtonController(type);
            _isInitialized = true;
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null || !_isInitialized) return;

            _view.SetProgression(_levelTracker.GetLevelId());
            _view.SetUltraActive(_ultra?.Active??false);
            foreach (RewardType rewardType in PowerUpTypeUtility.OrderedRewardTypes)
            {
                int count = _levelTracker.GetPowerUpCount(rewardType);
                bool isAvailable = IsPowerUpAvailable(rewardType);
                _view.SetPowerUpCount(rewardType, count);
                _view.SetPowerUpAlpha(rewardType, isAvailable ? EnabledAlpha : DisabledAlpha);
                _buttonControllers[rewardType].SetButtonClickable(_buttonsClickable && isAvailable);
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

        private bool IsPowerUpAvailable(RewardType rewardType)
            => _uses.Value.CanUse(rewardType);
    }

    public interface IGamePowerUpAreaController
    {
        void Initialize();
        void Refresh();
        void SetButtonsClickable(bool isClickable);
        event EventHandler<GameUIButtonType> PowerUpClickedEvent;
    }
}

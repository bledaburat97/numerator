using System;
using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class GamePowerUpAreaController : IGamePowerUpAreaController
    {
        [InjectOptional] private HelperEffect _helper;
        [InjectOptional] private PairEffect _pair;
        [InjectOptional] private BoardSplitService _split;
        private const float DisabledAlpha = 0.35f;
        private const float EnabledAlpha = 1f;

        private readonly BaseButtonControllerFactory _baseButtonControllerFactory;
        private readonly IPowerUpListHolderView _view;
        private readonly ILevelTracker _levelTracker;
        private readonly IRoundStateManager _roundStateManager;
        private readonly ICardItemInfoManager _cardItemInfoManager;
        private readonly IBoardHolderCountManager _boardHolderCountManager;
        private readonly Dictionary<RewardType, IBaseButtonController> _buttonControllers;

        private bool _isInitialized;
        private bool _buttonsClickable = true;

        public event EventHandler<GameUIButtonType> PowerUpClickedEvent;

        [Inject]
        public GamePowerUpAreaController(BaseButtonControllerFactory baseButtonControllerFactory,
            IPowerUpListHolderView view, ILevelTracker levelTracker, IRoundStateManager roundStateManager,
            ICardItemInfoManager cardItemInfoManager, IBoardHolderCountManager boardHolderCountManager)
        {
            _baseButtonControllerFactory = baseButtonControllerFactory;
            _view = view;
            _levelTracker = levelTracker;
            _roundStateManager = roundStateManager;
            _cardItemInfoManager = cardItemInfoManager;
            _boardHolderCountManager = boardHolderCountManager;
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
            foreach (var type in PowerUpTypeUtility.OrderedRewardTypes) CreateButtonController(type);
            _isInitialized = true;
            Refresh();
        }

        public void Refresh()
        {
            if (_view == null || !_isInitialized) return;

            _view.SetProgression(_levelTracker.GetLevelId());
            foreach (RewardType rewardType in PowerUpTypeUtility.OrderedRewardTypes)
            {
                int count = _levelTracker.GetPowerUpCount(rewardType);
                bool isAvailable = IsPowerUpAvailable(rewardType, count);
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

        private bool IsPowerUpAvailable(RewardType rewardType, int count)
        {
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer) return false;
            if (_roundStateManager.GetRemainingGuessCount() <= 0) return false;
            if (count <= 0 || !PowerUpTypeUtility.IsUnlocked(rewardType, _levelTracker.GetLevelId())) return false;
            if (rewardType == RewardType.Pair) return _pair != null && _pair.CanApply;
            if (rewardType == RewardType.Split) return _split != null && _split.CanApply;
            if (rewardType == RewardType.Helper) return _helper != null && _helper.CanApply;
            if (rewardType == RewardType.Life) return _roundStateManager.GetRemainingGuessCount() > 0 &&
                _roundStateManager.GetRemainingGuessCount() + 3 <= _roundStateManager.GetMaxGuessCount();
            if (rewardType != RewardType.Bomb) return true;

            return CanUseBombPowerUp();
        }

        private bool CanUseBombPowerUp()
        {
            if (!_boardHolderCountManager.CanShrink || _boardHolderCountManager.GetBoardHolderCount() <= 1) return false;
            if (_roundStateManager.GetTriedCardsList().Count > 0) return false;
            if (_boardHolderCountManager.GetRemovedBoardHolderCount() > 0) return false;

            return !HasRevealedCard();
        }

        private bool HasRevealedCard()
        {
            List<CardItemInfo> cardItemInfoList = _cardItemInfoManager.GetCardItemInfoList();
            if (cardItemInfoList == null) return false;

            foreach (CardItemInfo cardItemInfo in cardItemInfoList)
            {
                if (cardItemInfo != null && cardItemInfo.isLocked) return true;
            }

            return false;
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

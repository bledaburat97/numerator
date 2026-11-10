using System;
using System.Collections.Generic;
using Game;
using Zenject;

namespace Scripts
{
    public class GamePowerUpAreaController : IGamePowerUpAreaController
    {
        [InjectOptional] private IceBoardService _ice;
        [InjectOptional] private HelperEffect _helper;
        [InjectOptional] private PairEffect _pair;
        [InjectOptional] private BoardSplitService _split;
        [InjectOptional] private UltraPowerUpController _ultra;
        private const float DisabledAlpha = 0.55f;
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
            => PowerUpAvailability.CanUse(rewardType,_levelTracker,_roundStateManager,_cardItemInfoManager,_boardHolderCountManager,_helper,_pair,_split,_ultra,_ice);
    }

    // UI availability and execution share the same eligibility rules.
    public static class PowerUpAvailability
    {
        public static bool CanUse(RewardType type,ILevelTracker tracker,IRoundStateManager round,
            ICardItemInfoManager info,IBoardHolderCountManager counts,HelperEffect helper,PairEffect pair,BoardSplitService split,UltraPowerUpController ultra=null,IceBoardService ice=null)
        {
            if(tracker.GetGameOption()!=GameOption.SinglePlayer || round.GetRemainingGuessCount()<=0
                || tracker.GetPowerUpCount(type)<=0 || !PowerUpTypeUtility.IsUnlocked(type,tracker.GetLevelId()))return false;
            switch(type)
            {
                case RewardType.Helper:return helper!=null && helper.CanApply;
                case RewardType.Pair:return pair!=null && pair.CanApply;
                case RewardType.Split:return split!=null && split.CanApply && (!(ultra?.Active??false) || ultra.SupportsSplit);
                case RewardType.Ultra:return ultra!=null && ultra.CanApply;
                case RewardType.Life:return round.GetRemainingGuessCount()+3<=round.GetMaxGuessCount();
                case RewardType.Bomb:
                    if(!counts.CanShrink || counts.GetBoardHolderCount()<=1 || round.GetTriedCardsList().Count>0 || counts.GetRemovedBoardHolderCount()>0)return false;
                    foreach(var card in info.GetCardItemInfoList())if(card!=null && card.isLocked)return false;
                    return true;
                case RewardType.Revealing:
                    int locked=0;
                    foreach(var card in info.GetCardItemInfoList())
                        if(card!=null && card.isLocked && !card.possibleCardHolderIndicatorIndexes.Contains(ice?.FrozenIndex??-1))locked++;
                    return locked<counts.GetBoardHolderCount()-(ice?.IsFrozen==true?1:0);
                default:return false;
            }
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

using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Scripts
{
    public class RewardProgressDisplayController : IRewardProgressDisplayController
    {
        private const float RewardItemTargetSize = 25f;
        private const float RewardItemSizeToTargetRatio = 1f;

        private readonly ICircleProgressBarView _view;
        private readonly ILevelTracker _levelTracker;
        private readonly MovingRewardItemView _movingRewardItemPrefab;
        private readonly List<IRewardItemTargetView> _rewardItemTargetViews;

        private bool _isInitialized;

        public RewardProgressDisplayController(ICircleProgressBarView view, ILevelTracker levelTracker)
        {
            _view = view;
            _levelTracker = levelTracker;
            _movingRewardItemPrefab = view != null ? view.GetMovingRewardItemPrefab() : null;
            _rewardItemTargetViews = new List<IRewardItemTargetView>();
        }

        public void Initialize()
        {
            if (_view == null) return;

            if (!_isInitialized)
            {
                CreateRewardItemTargets();
                _isInitialized = true;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_view == null) return;

            if (!_isInitialized)
            {
                Initialize();
                return;
            }

            _view.SetStatus(true);
            _view.ShowRewardPreview(_levelTracker.GetCurrentRewardType());
            SyncRewardItems();
            _view.GetInnerCircleImage().fillAmount =
                _levelTracker.GetGiftStarCount() / (float)ConstantValues.NUM_OF_STARS_FOR_WILD;
        }

        private void CreateRewardItemTargets()
        {
            DestroyRewardItemTargets();
            _rewardItemTargetViews.Clear();

            for (int i = 0; i < ConstantValues.NUM_OF_STARS_FOR_WILD; i++)
            {
                float angle = 90f - i * (360f / ConstantValues.NUM_OF_STARS_FOR_WILD);
                float radians = Mathf.Deg2Rad * angle;
                float radius = _view.GetRectTransform().sizeDelta.x / 2f - 1f;
                Vector2 localPosition = new Vector2(radius * Mathf.Cos(radians), radius * Mathf.Sin(radians));

                IRewardItemTargetView rewardItemTargetView =
                    _view.CreateRewardItemTarget(new RewardItemTargetViewFactory());
                rewardItemTargetView.SetLocalPosition(localPosition);
                rewardItemTargetView.SetLocalScale(Vector3.one);
                rewardItemTargetView.SetSize(Vector2.one * RewardItemTargetSize);
                _rewardItemTargetViews.Add(rewardItemTargetView);
            }
        }

        private void SyncRewardItems()
        {
            int rewardItemCount = Mathf.Clamp(_levelTracker.GetGiftStarCount(), 0, ConstantValues.NUM_OF_STARS_FOR_WILD);
            for (int i = 0; i < _rewardItemTargetViews.Count; i++)
            {
                IRewardItemTargetView targetView = _rewardItemTargetViews[i];
                if (i < rewardItemCount)
                {
                    EnsureRewardItemOnTarget(targetView);
                }
                else
                {
                    ClearRewardItemOnTarget(targetView);
                }
            }
        }

        private void EnsureRewardItemOnTarget(IRewardItemTargetView rewardItemTargetView)
        {
            MovingRewardItemView rewardItem = rewardItemTargetView.GetRewardItem();
            if (rewardItem == null)
            {
                rewardItem = CreateStandaloneRewardItem(rewardItemTargetView.GetRectTransform());
            }

            AttachRewardItemToTarget(rewardItemTargetView, rewardItem);
        }

        private void ClearRewardItemOnTarget(IRewardItemTargetView rewardItemTargetView)
        {
            MovingRewardItemView rewardItem = rewardItemTargetView.GetRewardItem();
            if (rewardItem == null) return;

            rewardItem.DestroyObject();
            rewardItemTargetView.ClearRewardItem();
        }

        private MovingRewardItemView CreateStandaloneRewardItem(RectTransform parentRect)
        {
            MovingRewardItemView rewardItemView;
            if (_movingRewardItemPrefab != null)
            {
                rewardItemView = Object.Instantiate(_movingRewardItemPrefab, parentRect, false);
            }
            else
            {
                GameObject rewardItemObject = new GameObject("StaticRewardItem", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(MovingRewardItemView));
                rewardItemView = rewardItemObject.GetComponent<MovingRewardItemView>();
                rewardItemView.GetRectTransform().SetParent(parentRect, false);
            }

            rewardItemView.Init(parentRect);
            rewardItemView.SetColor(ConstantValues.BLUE_STAR_COLOR);
            return rewardItemView;
        }

        private static void AttachRewardItemToTarget(IRewardItemTargetView rewardItemTargetView, MovingRewardItemView rewardItem)
        {
            RectTransform targetRectTransform = rewardItemTargetView.GetRectTransform();
            RectTransform rewardItemTransform = rewardItem.GetRectTransform();
            rewardItemTransform.SetParent(targetRectTransform, false);
            rewardItem.SetSize(Vector2.one * targetRectTransform.sizeDelta.x * RewardItemSizeToTargetRatio);
            rewardItem.SetStatus(true);
            rewardItem.SnapToCenter();
            rewardItemTargetView.SetRewardItem(rewardItem);
        }

        private void DestroyRewardItemTargets()
        {
            foreach (IRewardItemTargetView rewardItemTargetView in _rewardItemTargetViews)
            {
                ClearRewardItemOnTarget(rewardItemTargetView);
                rewardItemTargetView.Destroy();
            }
        }
    }

    public interface IRewardProgressDisplayController
    {
        void Initialize();
        void Refresh();
    }
}

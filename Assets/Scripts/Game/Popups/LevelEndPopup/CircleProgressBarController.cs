using System.Collections.Generic;
using DG.Tweening;
using Game;
using UnityEngine;

namespace Scripts
{
    public class CircleProgressBarController : ICircleProgressBarController
    {
        private const float RewardItemTargetSize = 25f;
        private const float RewardItemSizeToTargetRatio = 1f;

        private ICircleProgressBarView _view;
        private int _currentStarCount;
        private List<IRewardItemTargetView> _rewardItemTargetViewList;
        private IHapticController _hapticController;
        private MovingRewardItemView _movingRewardItemPrefab;
        private float _currentPercentage;

        public CircleProgressBarController(ICircleProgressBarView view, IHapticController hapticController)
        {
            _view = view;
            _hapticController = hapticController;
            _movingRewardItemPrefab = view.GetMovingRewardItemPrefab();
            _rewardItemTargetViewList = new List<IRewardItemTargetView>();
        }
        
        public void Initialize(int rewardStarCount)
        {
            SetPercentageOfCircle(0f);
            DestroyRewardItemTargets();
            _rewardItemTargetViewList.Clear();
            _currentStarCount = rewardStarCount;
            _view.SetStatus(true);
            CreateRewardItemTargets();
            SetLocalPositionOfCircle(new Vector2(0, 400f));
        }

        private void SetPercentageOfCircle(float targetPercentage)
        {
            _currentPercentage = targetPercentage;
            _view.GetInnerCircleImage().fillAmount = targetPercentage;
        }
        
        private void CreateRewardItemTargets()
        {
            for (int i = 0; i < ConstantValues.NUM_OF_STARS_FOR_WILD; i++)
            {
                float startingAngle = 90f;
                float angle = startingAngle - i * (360f / ConstantValues.NUM_OF_STARS_FOR_WILD);
                float radians = Mathf.Deg2Rad * angle;
                float radius = _view.GetRectTransform().sizeDelta.x / 2 - 1;
                float x = radius * Mathf.Cos(radians);
                float y = radius * Mathf.Sin(radians);
                
                IRewardItemTargetView rewardItemTargetView = _view.CreateRewardItemTarget(new RewardItemTargetViewFactory());
                rewardItemTargetView.SetLocalPosition(new Vector2(x, y));
                rewardItemTargetView.SetLocalScale(Vector3.one);
                rewardItemTargetView.SetSize(new Vector2(RewardItemTargetSize, RewardItemTargetSize));
                _rewardItemTargetViewList.Add(rewardItemTargetView);
            }
        }
        
        private void SetLocalPositionOfCircle(Vector2 localPosition)
        {
            _view.GetRectTransform().localPosition = localPosition;
        }

        public void CreateInitialStarImages()
        {
            _currentStarCount = _currentStarCount % ConstantValues.NUM_OF_STARS_FOR_WILD;
            
            for (int i = 0; i < _currentStarCount; i++)
            {
                CreateRewardItemOnTarget(_rewardItemTargetViewList[i]);
            }
            
            if (_currentStarCount >= 1)
            {
                SetPercentageOfCircle((float)(_currentStarCount - 1) / ConstantValues.NUM_OF_STARS_FOR_WILD);
            }
            else
            {
                SetPercentageOfCircle(0f);
            }
        }
        
        public Sequence AddNewStars(StarImageView[] newStars, int numOfBlueStars, int earnedStarCount)
        {
            Sequence sequence = DOTween.Sequence();
            int firstRewardStarIndex = Mathf.Max(0, earnedStarCount - numOfBlueStars);
            for (int i = firstRewardStarIndex; i < earnedStarCount; i++)
            {
                sequence.Append(GetNewRewardItemAnimation(newStars[i]));
            }

            return sequence;
        }

        private Sequence GetNewRewardItemAnimation(IStarImageView rewardStarView)
        {
            if (rewardStarView == null) return DOTween.Sequence();

            float rewardItemSize = rewardStarView.GetRectTransform().rect.width * 1.04f;
            MovingRewardItemView rewardItem =
                rewardStarView.SpawnTransientMovingRewardItem(new Vector2(rewardItemSize, rewardItemSize));
            if (rewardItem == null) return DOTween.Sequence();

            const float transferDuration = 1f;
            _currentStarCount += 1;
            int targetIndex = (_currentStarCount - 1) % ConstantValues.NUM_OF_STARS_FOR_WILD;
            IRewardItemTargetView targetView = _rewardItemTargetViewList[targetIndex];
            RectTransform targetInCircle = targetView.GetRectTransform();
            RectTransform rewardItemTransform = rewardItem.GetRectTransform();
            float finalRewardItemSize = targetInCircle.sizeDelta.x * RewardItemSizeToTargetRatio;
            float scaleRatio = finalRewardItemSize / rewardItemTransform.sizeDelta.x;
            RectTransform targetParent = targetInCircle.parent as RectTransform;
            
            return DOTween.Sequence()
                .Append(rewardItem.AnimateSpawn())
                .Join(rewardStarView.AnimateColor(true, 0.22f))
                .AppendCallback(() =>
                {
                    rewardItemTransform.SetParent(targetParent, true);
                })
                .Append(rewardItemTransform.DOScale(Vector3.one * scaleRatio, transferDuration)
                    .SetEase(rewardStarView.GetCurvedAnimationPreset().scaleCurve))
                .Join(rewardItemTransform.DOLocalMoveX(targetInCircle.localPosition.x, transferDuration)
                    .SetEase(rewardStarView.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(rewardItemTransform.DOLocalMoveY(targetInCircle.localPosition.y, transferDuration)
                    .SetEase(rewardStarView.GetCurvedAnimationPreset().verticalPositionCurve))
                .Join(_currentStarCount > ConstantValues.NUM_OF_STARS_FOR_WILD
                    ? DOTween.Sequence()
                    : GetProgressTween(
                        (float)((_currentStarCount - 1) % ConstantValues.NUM_OF_STARS_FOR_WILD) / ConstantValues.NUM_OF_STARS_FOR_WILD, transferDuration))
                .AppendCallback(() =>
                {
                    AttachRewardItemToTarget(targetView, rewardItem);
                })
                .AppendCallback(() => _hapticController.Vibrate(HapticType.Success))
                .Append(_currentStarCount == ConstantValues.NUM_OF_STARS_FOR_WILD
                    ? GetProgressTween(1f, 0.1f)
                    : DOTween.Sequence());
        }
        
        private Tween GetProgressTween(float targetPercentage, float duration)
        {
            return DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .Pause().SetEase(Ease.OutQuad)
                .OnUpdate(() => { _view.GetInnerCircleImage().fillAmount = _currentPercentage; })
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                });
        }
        
        public Sequence MoveCircleProgressBar(float duration)
        {
            return DOTween.Sequence().Append(_view.GetRectTransform().DOLocalMoveY(0f, duration))
                .SetEase(Ease.InQuad);
        }

        public void SetStatus(bool status)
        {
            _view.GetRectTransform().gameObject.SetActive(status);
        }

        public void DestroyStarImages()
        {
            foreach (IRewardItemTargetView rewardItemTargetView in _rewardItemTargetViewList)
            {
                MovingRewardItemView rewardItem = rewardItemTargetView.GetRewardItem();
                if (rewardItem != null)
                {
                    rewardItem.DestroyObject();
                    rewardItemTargetView.ClearRewardItem();
                }
            }
        }

        private void CreateRewardItemOnTarget(IRewardItemTargetView rewardItemTargetView)
        {
            MovingRewardItemView rewardItem = rewardItemTargetView.GetRewardItem();
            if (rewardItem == null)
            {
                rewardItem = CreateStandaloneRewardItem(rewardItemTargetView.GetRectTransform());
                rewardItemTargetView.SetRewardItem(rewardItem);
            }

            AttachRewardItemToTarget(rewardItemTargetView, rewardItem);
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
                GameObject rewardItemObject = new GameObject("CircleRewardItem", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(MovingRewardItemView));
                RectTransform rewardItemRectTransform = rewardItemObject.GetComponent<RectTransform>();
                rewardItemRectTransform.SetParent(parentRect, false);
                rewardItemView = rewardItemObject.GetComponent<MovingRewardItemView>();
            }

            rewardItemView.Init(parentRect);
            rewardItemView.SetColor(ConstantValues.BLUE_STAR_COLOR);
            return rewardItemView;
        }

        private void AttachRewardItemToTarget(IRewardItemTargetView rewardItemTargetView, MovingRewardItemView rewardItem)
        {
            if (rewardItem == null) return;

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
            foreach (IRewardItemTargetView rewardItemTargetView in _rewardItemTargetViewList)
            {
                rewardItemTargetView.Destroy();
            }
        }
    }

    public interface ICircleProgressBarController
    {
        void Initialize(int rewardStarCount);
        void CreateInitialStarImages();
        Sequence AddNewStars(StarImageView[] newStars, int numOfBlueStars, int earnedStarCount);
        Sequence MoveCircleProgressBar(float duration);
        void SetStatus(bool status);
        void DestroyStarImages();
    }
}

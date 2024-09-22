using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class CircleProgressBarController : ICircleProgressBarController
    {
        private ICircleProgressBarView _view;
        private int _currentStarCount;
        private List<IStarImageView> _starFrameViewList;
        private List<IStarImageView> _starImageViewList;
        private IHapticController _hapticController;
        private float _currentPercentage;
        public void Initialize(ICircleProgressBarView view, IHapticController hapticController, int rewardStarCount)
        {
            _view = view;
            SetPercentageOfCircle(0f);
            _starFrameViewList = new List<IStarImageView>();
            _starImageViewList = new List<IStarImageView>();
            _currentStarCount = rewardStarCount;
            _hapticController = hapticController;
            CreateStarFrames();
            SetLocalPositionOfCircle(new Vector2(0, 400f));
        }

        private void SetPercentageOfCircle(float targetPercentage)
        {
            _currentPercentage = targetPercentage;
            _view.GetImage().fillAmount = targetPercentage;
        }
        
        private void CreateStarFrames()
        {
            for (int i = 0; i < ConstantValues.NUM_OF_STARS_FOR_WILD; i++)
            {
                float startingAngle = 90f;
                float angle = startingAngle - i * (360f / ConstantValues.NUM_OF_STARS_FOR_WILD);
                float radians = Mathf.Deg2Rad * angle;
                float radius = _view.GetRectTransform().sizeDelta.x / 2 - 1;
                float x = radius * Mathf.Cos(radians);
                float y = radius * Mathf.Sin(radians);
                
                IStarImageView starImageView = _view.CreateStarImage(new StarImageViewFactory());
                starImageView.SetLocalPosition(new Vector3(x, y, 0f));
                starImageView.SetLocalScale(Vector3.one);
                starImageView.SetSize(new Vector2(25f, 25f));
                starImageView.SetStarStatus(false);
                _starFrameViewList.Add(starImageView);
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
                IStarImageView starImageView = _view.CreateStarImage(new StarImageViewFactory());
                starImageView.SetLocalPosition(_starFrameViewList[i].GetRectTransform().localPosition);
                starImageView.SetLocalScale(Vector3.one);
                starImageView.SetSize(new Vector2(25f, 25f));
                starImageView.SetColor(false);
                _starImageViewList.Add(starImageView);
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
            //earned star 3 
            //numOfBlue 1 -> 0 1 sarı 2 mavi
            //2-> 0 sarı 1 2 mavi
            //3 -> 0 1 2 mavi
            
            //earned 2
            //0 -> 0 1 sarı
            //1 -> 0 sarı 1 mavi
            //2 -> 0 1 mavi
            
            //earned 1
            //0 -> 0 sarı
            //1 -> 0 mavi
            return DOTween.Sequence()
                .Append(GetNewStarAnimation(earnedStarCount - numOfBlueStars < 1 && earnedStarCount > 0 ? newStars[0] : null))
                    .Append(GetNewStarAnimation(earnedStarCount - numOfBlueStars < 2 && earnedStarCount > 1 ? newStars[1] : null))
                    .Append(GetNewStarAnimation(numOfBlueStars > 0 && earnedStarCount > 2 ? newStars[2] : null));
        }

        private Sequence GetNewStarAnimation(IStarImageView newStar)
        {
            if (newStar == null) return DOTween.Sequence();
            IStarImageView animatedStar = _view.CreateStarImage(new StarImageViewFactory());
            
            animatedStar.SetParent(newStar.GetRectTransform());
            animatedStar.SetLocalPosition(Vector2.zero);
            animatedStar.SetSize(new Vector2(ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS, ConstantValues.SIZE_OF_STARS_ON_LEVEL_SUCCESS));
            animatedStar.SetColor(false);
            _starImageViewList.Add(animatedStar);
            _currentStarCount += 1;
            RectTransform targetInCircle = _starFrameViewList[(_currentStarCount - 1) % ConstantValues.NUM_OF_STARS_FOR_WILD].GetRectTransform();
            RectTransform animatedStarTransform = animatedStar.GetRectTransform();
            float scaleRatio = targetInCircle.sizeDelta.x / animatedStarTransform.sizeDelta.x;
            
            return DOTween.Sequence().AppendCallback(() => animatedStar.SetLocalScale(Vector3.one))
                .AppendCallback(() => animatedStar.SetParent(_view.GetRectTransform()))
                .AppendCallback(() => newStar.SetColor(true))
                .Append(animatedStarTransform.DOScale(Vector3.one * scaleRatio, 0.5f)
                    .SetEase(animatedStar.GetCurvedAnimationPreset().scaleCurve))
                .Join(animatedStar.GetRectTransform().DOLocalMoveX(targetInCircle.localPosition.x, 0.5f)
                    .SetEase(animatedStar.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(animatedStar.GetRectTransform().DOLocalMoveY(targetInCircle.localPosition.y, 0.5f)
                    .SetEase(animatedStar.GetCurvedAnimationPreset().verticalPositionCurve))
                .Join(_currentStarCount > ConstantValues.NUM_OF_STARS_FOR_WILD
                    ? DOTween.Sequence()
                    : GetProgressTween(
                        (float)((_currentStarCount - 1) % ConstantValues.NUM_OF_STARS_FOR_WILD) / ConstantValues.NUM_OF_STARS_FOR_WILD, 0.5f))
                .AppendCallback(() => _hapticController.Vibrate(HapticType.Success))
                .Append(_currentStarCount == ConstantValues.NUM_OF_STARS_FOR_WILD
                    ? GetProgressTween(1f, 0.1f)
                    : DOTween.Sequence());
        }
        
        private Tween GetProgressTween(float targetPercentage, float duration)
        {
            return DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .Pause().SetEase(Ease.OutQuad)
                .OnUpdate(() => { _view.GetImage().fillAmount = _currentPercentage; })
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
            foreach (IStarImageView glowingStarImageView in _starImageViewList)
            {
                glowingStarImageView.Destroy();
            }
        }
    }

    public interface ICircleProgressBarController
    {
        void Initialize(ICircleProgressBarView view, IHapticController hapticController, int rewardStarCount);
        void CreateInitialStarImages();
        Sequence AddNewStars(StarImageView[] newStars, int numOfBlueStars, int earnedStarCount);
        Sequence MoveCircleProgressBar(float duration);
        void SetStatus(bool status);
        void DestroyStarImages();
    }
}

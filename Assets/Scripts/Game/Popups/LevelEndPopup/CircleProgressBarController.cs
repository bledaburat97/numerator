﻿using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
/*
namespace Scripts
{
    public class CircleProgressBarController : ICircleProgressBarController
    {
        private ICircleProgressBarView _view;
        private IGlowingCircleProgressBarView _glowingView;
        private int _currentStarCount;
        private List<IStarImageView> _starFrameViewList;
        private List<IStarImageView> _glowingStarImageViewList;
        private IHapticController _hapticController;
        public void Initialize(ICircleProgressBarView view, IGlowingCircleProgressBarView glowingView, ILevelTracker levelTracker, IHapticController hapticController)
        {
            _view = view;
            _glowingView = glowingView;
            _view.Init(new StarImageViewFactory());
            _glowingView.Init(new StarImageViewFactory());
            _starFrameViewList = new List<IStarImageView>();
            _glowingStarImageViewList = new List<IStarImageView>();
            _currentStarCount = levelTracker.GetBlueStarCount() % ConstantValues.NUM_OF_STARS_FOR_WILD;
            _hapticController = hapticController;
            CreateStarFrames();
            _view.SetLocalPosition(new Vector2(0,510f));
            _glowingView.SetLocalPosition(new Vector2(0,510f));
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
                
                IStarImageView starImageView = _view.CreateStarImage();
                starImageView.SetLocalPosition(new Vector3(x, y, 0f));
                starImageView.SetLocalScale(Vector3.one);
                starImageView.SetSize(new Vector2(25f, 25f));
                starImageView.SetStarStatus(false);
                _starFrameViewList.Add(starImageView);
            }
        }

        public int GetCurrentStarCount()
        {
            return _currentStarCount;
        }

        public void CreateInitialStars()
        {
            _currentStarCount = _currentStarCount % ConstantValues.NUM_OF_STARS_FOR_WILD;
            
            foreach (IStarImageView glowingStarImageView in _glowingStarImageViewList)
            {
                glowingStarImageView.Destroy();
            }
            
            for (int i = 0; i < _currentStarCount; i++)
            {
                IStarImageView glowingStarImageView = _glowingView.CreateStarImage();
                glowingStarImageView.SetLocalPosition(_starFrameViewList[i].GetRectTransform().localPosition);
                glowingStarImageView.SetLocalScale(Vector3.one);
                glowingStarImageView.SetSize(new Vector2(25f, 25f));
                glowingStarImageView.SetColor(false);
                _glowingStarImageViewList.Add(glowingStarImageView);
            }

            if (_currentStarCount >= 2)
            {
                _view.SetProgress((float)(_currentStarCount - 1) / ConstantValues.NUM_OF_STARS_FOR_WILD);
            }
            else
            {
                _view.SetProgress(0f);
            }
        }

        public Sequence AddNewStars(List<IStarImageView> newStars, int blueStarCount, int oldStarCount)
        {
            return DOTween.Sequence()
                .Append(GetNewStarAnimation(newStars.Count >= 1 && oldStarCount >= 3 - blueStarCount ? newStars[0] : null))
                    .Append(GetNewStarAnimation(newStars.Count >= 2 && oldStarCount >= 2 - blueStarCount ? newStars[1] : null))
                    .Append(GetNewStarAnimation(newStars.Count == 3 && oldStarCount >= 1 - blueStarCount ? newStars[2] : null));
        }

        public Sequence MoveCircleProgressBar(float duration)
        {
            return DOTween.Sequence().Append(_view.GetRectTransform().DOLocalMoveY(250f, duration)).SetEase(Ease.InQuad)
                .Join(_glowingView.GetRectTransform().DOLocalMoveY(250f, duration)).SetEase(Ease.InQuad);
        }

        private Sequence GetNewStarAnimation(IStarImageView newStar)
        {
            if (newStar == null) return DOTween.Sequence();
            IStarImageView animatedStar = _glowingView.CreateStarImage();
            
            animatedStar.SetParent(newStar.GetRectTransform());
            animatedStar.SetLocalPosition(Vector2.zero);
            animatedStar.SetSize(new Vector2(70f, 70f));
            animatedStar.SetColor(false);
            _glowingStarImageViewList.Add(animatedStar);
            _currentStarCount += 1;
            RectTransform targetInCircle = _starFrameViewList[(_currentStarCount - 1) % ConstantValues.NUM_OF_STARS_FOR_WILD].GetRectTransform();
            RectTransform animatedStarTransform = animatedStar.GetRectTransform();
            float scaleRatio = targetInCircle.sizeDelta.x / animatedStarTransform.sizeDelta.x;
            
            return DOTween.Sequence().AppendCallback(() => animatedStar.SetLocalScale(Vector3.one))
                .AppendCallback(() => animatedStar.SetParent(_glowingView.GetRectTransform()))
                .AppendCallback(() => newStar.SetColor(true))
                .Append(animatedStarTransform.DOScale(Vector3.one * scaleRatio, 0.5f)
                    .SetEase(animatedStar.GetCurvedAnimationPreset().scaleCurve))
                .Join(animatedStar.GetRectTransform().DOLocalMoveX(targetInCircle.localPosition.x, 0.5f)
                    .SetEase(animatedStar.GetCurvedAnimationPreset().horizontalPositionCurve))
                .Join(animatedStar.GetRectTransform().DOLocalMoveY(targetInCircle.localPosition.y, 0.5f)
                    .SetEase(animatedStar.GetCurvedAnimationPreset().verticalPositionCurve))
                .Join(_currentStarCount > ConstantValues.NUM_OF_STARS_FOR_WILD
                    ? DOTween.Sequence()
                    : _view.GetProgressTween(
                        (float)((_currentStarCount - 1) % ConstantValues.NUM_OF_STARS_FOR_WILD) / ConstantValues.NUM_OF_STARS_FOR_WILD, 0.5f))
                .AppendCallback(() => _hapticController.Vibrate(HapticType.Success))
                .Append(_currentStarCount == ConstantValues.NUM_OF_STARS_FOR_WILD
                    ? _view.GetProgressTween(1f, 0.1f)
                    : DOTween.Sequence());
        }
        
    }

    public interface ICircleProgressBarController
    {
        void Initialize(ICircleProgressBarView view, IGlowingCircleProgressBarView glowingView, ILevelTracker levelTracker, IHapticController hapticController);
        void CreateInitialStars();
        Sequence AddNewStars(List<IStarImageView> newStars, int blueStarCount, int oldStarCount);
        int GetCurrentStarCount();
        Sequence MoveCircleProgressBar(float duration);
    }
}
*/
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class CircleProgressBarController : ICircleProgressBarController
    {
        private ICircleProgressBarView _view;
        private IGlowingCircleProgressBarView _glowingView;
        private int _currentStarCount;
        private List<IStarImageView> _starFrameViewList;
        private List<IStarImageView> _glowingStarImageViewList;
        private IWildCardItemView _wildCardItemView;
        private static int NUM_OF_STARS_FOR_WILD = 6;
        
        public void Initialize(ICircleProgressBarView view, IGlowingCircleProgressBarView glowingView)
        {
            _view = view;
            _glowingView = glowingView;
            _view.Init(new StarImageViewFactory());
            _glowingView.Init(new StarImageViewFactory(), new WildCardItemViewFactory());
            _starFrameViewList = new List<IStarImageView>();
            _glowingStarImageViewList = new List<IStarImageView>();
            CreateStarFrames();
            ResetCircleProgressBar();
        }
        
        private void CreateStarFrames()
        {
            for (int i = 0; i < NUM_OF_STARS_FOR_WILD; i++)
            {
                float startingAngle = 90f;
                float angle = startingAngle - i * (360f / NUM_OF_STARS_FOR_WILD);
                float radians = Mathf.Deg2Rad * angle;
                float radius = _view.GetRectTransform().sizeDelta.x / 2 - 1;
                float x = radius * Mathf.Cos(radians);
                float y = radius * Mathf.Sin(radians);
                
                IStarImageView starImageView = _view.CreateStarImage();
                starImageView.Init(new Vector3(x, y, 0f));
                starImageView.SetSize(new Vector2(25f, 25f));
                starImageView.SetStarStatus(false);
                _starFrameViewList.Add(starImageView);
            }
        }
        
        private void ResetCircleProgressBar()
        {
            _wildCardItemView = _glowingView.CreateWildCardImage();
            _wildCardItemView.InitPosition();
            
            foreach (IStarImageView glowingStarImageView in _glowingStarImageViewList)
            {
                glowingStarImageView.Destroy();
            }
            _view.SetProgress(0f);
        }

        public void CreateInitialStars(int numOfStars)
        {
            int numOfInitialStars = numOfStars % NUM_OF_STARS_FOR_WILD;
            
            for (int i = 0; i < numOfInitialStars; i++)
            {
                IStarImageView glowingStarImageView = _glowingView.CreateStarImage();
                glowingStarImageView.Init(_starFrameViewList[i].GetRectTransform().localPosition);
                glowingStarImageView.SetSize(new Vector2(25f, 25f));
                _glowingStarImageViewList.Add(glowingStarImageView);
            }

            if (numOfInitialStars >= 2)
            {
                _view.SetProgress((float)(numOfInitialStars - 1) / NUM_OF_STARS_FOR_WILD);
            }

            _currentStarCount = numOfInitialStars;
        }

        public void AddNewStars(List<IStarImageView> newStars)
        {
            int newStarIndex = 0;
            AnimateStar();
            
            void AnimateStar()
            {
                if (newStarIndex >= newStars.Count) return;
                
                IStarImageView animatedStar = newStars[newStarIndex];
                _glowingStarImageViewList.Add(animatedStar);
                newStarIndex++;
                _currentStarCount += 1;

                RectTransform targetInCircle = _starFrameViewList[(_currentStarCount - 1) % NUM_OF_STARS_FOR_WILD].GetRectTransform();
                animatedStar.SetParent(_glowingView.GetRectTransform());
                RectTransform animatedStarTransform = animatedStar.GetRectTransform();
                float scaleRatio = targetInCircle.sizeDelta.x / animatedStarTransform.sizeDelta.x;
                
                Action onCompleteAction = _currentStarCount % NUM_OF_STARS_FOR_WILD == 0 ? () => ResetAnimation(AnimateStar) : AnimateStar;
                
                DOTween.Sequence().Append(animatedStarTransform.DOScale(Vector3.one * scaleRatio, 0.5f).SetEase(animatedStar.GetCurvedAnimationPreset().scaleCurve))
                    .Join(animatedStar.GetRectTransform().DOLocalMoveX(targetInCircle.localPosition.x, 0.5f).SetEase(animatedStar.GetCurvedAnimationPreset().horizontalPositionCurve))
                    .Join(animatedStar.GetRectTransform().DOLocalMoveY(targetInCircle.localPosition.y, 0.5f).SetEase(animatedStar.GetCurvedAnimationPreset().verticalPositionCurve))
                    .Join(_view.GetProgressTween((float)((_currentStarCount - 1) % NUM_OF_STARS_FOR_WILD) / NUM_OF_STARS_FOR_WILD, 0.5f, onCompleteAction).Play());
            }
        }

        private void ResetAnimation(Action onComplete)
        {
            DOTween.Sequence()
                .Append(_view.GetProgressTween(1f, 0.5f, null))
                .Append(MoveWildCard())
                .AppendCallback(ResetCircleProgressBar)
                .OnComplete(onComplete.Invoke);
        }
        
        public Tween MoveWildCard()
        {
            _wildCardItemView.SetParent(_glowingView.GetTempWildCardHolder());
            
            return DOTween.Sequence().Append(_wildCardItemView.GetRectTransform().DOScale(Vector3.one * 5/3f, 1.5f))
                .Join(DOTween.Sequence().AppendInterval(0.5f).Append(_wildCardItemView.GetRectTransform().DOLocalMoveY(-190f, 1f)))
                .AppendInterval(0.5f)
                .Append(_wildCardItemView.GetCanvasGroup().DOFade(0f, 1f))
                .OnComplete(() => _wildCardItemView.Destroy());
        }
    }

    public interface ICircleProgressBarController
    {
        void Initialize(ICircleProgressBarView view, IGlowingCircleProgressBarView glowingView);
        void CreateInitialStars(int numOfStars);
        void AddNewStars(List<IStarImageView> newStars);
    }
}
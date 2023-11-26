using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class CircleProgressBarController : ICircleProgressBarController
    {
        private ICircleProgressBarView _view;
        private int _currentStarCount;
        private List<IStarImageView> _starImageViewList;
        private IWildCardItemView _wildCardItemView;
        private static int NUM_OF_STARS_FOR_WILD = 6;
        public void Initialize(ICircleProgressBarView view)
        {
            _view = view;
            _view.Init(new StarImageViewFactory(), new WildCardItemViewFactory());
            _starImageViewList = new List<IStarImageView>();
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
                _starImageViewList.Add(starImageView);
            }
        }
        
        private void ResetCircleProgressBar()
        {
            _wildCardItemView = _view.CreateWildCardImage();
            _wildCardItemView.InitPosition();
            foreach (IStarImageView starImageView in _starImageViewList)
            {
                starImageView.SetStarStatus(false);
            }
            _view.GetProgressTween(0f, 0f, null).Play();
        }

        public void CreateInitialStars(int numOfStars)
        {
            int numOfInitialStars = numOfStars % NUM_OF_STARS_FOR_WILD;
            
            for (int i = 0; i < numOfInitialStars; i++)
            {
                _starImageViewList[i].SetStarStatus(true);
            }

            if (numOfInitialStars >= 2)
            {
                _view.GetProgressTween((float)(numOfInitialStars - 1) / NUM_OF_STARS_FOR_WILD, 0f, null).Play();
            }

            _currentStarCount = numOfInitialStars;
        }

        public void AddNewStars(List<IStarImageView> newStars)
        {
            int newStarIndex = 0;

            DOTween.Sequence().AppendInterval(1f).OnComplete(AnimateStar);
            
            void AnimateStar()
            {
                if (newStarIndex >= newStars.Count) return;
                
                IStarImageView animatedStar = newStars[newStarIndex];
                newStarIndex++;
                _currentStarCount += 1;
                
                RectTransform parentTransform = _starImageViewList[(_currentStarCount - 1) % NUM_OF_STARS_FOR_WILD].GetRectTransform();
                animatedStar.SetParent(parentTransform);
                RectTransform animatedStarTransform = animatedStar.GetRectTransform();
                float scaleRatio = parentTransform.sizeDelta.x / animatedStarTransform.sizeDelta.x;
                
                Action onCompleteAction = animatedStar.Destroy;
                onCompleteAction += () => _starImageViewList[(_currentStarCount - 1) % NUM_OF_STARS_FOR_WILD].SetStarStatus(true);
                onCompleteAction += _currentStarCount % NUM_OF_STARS_FOR_WILD == 0 ? () => ResetAnimation(AnimateStar) : AnimateStar;
                
                DOTween.Sequence().Append(animatedStarTransform.DOScale(Vector3.one * scaleRatio, 1f))
                    .Join(animatedStar.GetRectTransform().DOLocalMove(Vector3.zero, 1f).SetEase(Ease.OutQuad))
                    .Join(_view.GetProgressTween((float)((_currentStarCount - 1) % NUM_OF_STARS_FOR_WILD) / NUM_OF_STARS_FOR_WILD, 1f, onCompleteAction).Play());
            }
        }

        private void ResetAnimation(Action onComplete)
        {
            DOTween.Sequence()
                .Append(_view.GetProgressTween(1f, 1f, null))
                .Append(_wildCardItemView.MoveWildCard(_view.GetTempWildCardHolder()))
                .AppendCallback(ResetCircleProgressBar)
                .OnComplete(onComplete.Invoke);
        }
    }

    public interface ICircleProgressBarController
    {
        void Initialize(ICircleProgressBarView view);
        void CreateInitialStars(int numOfStars);
        void AddNewStars(List<IStarImageView> newStars);
    }
}
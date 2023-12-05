using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CircleProgressBarView : MonoBehaviour, ICircleProgressBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private StarImageView starImagePrefab;
        private StarImageViewFactory _starImageViewFactory;
        [SerializeField] private RectTransform wildCardHolder;
        [SerializeField] private RectTransform tempWildCardHolder;
        [SerializeField] private Image image;
        private Tween _currentTween;
        private float _currentPercentage;
        [SerializeField] private WildCardItemView wildCardItemPrefab;
        private WildCardItemViewFactory _wildCardItemViewFactory;
        
        public void Init(StarImageViewFactory starImageViewFactory, WildCardItemViewFactory wildCardItemViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _wildCardItemViewFactory = wildCardItemViewFactory;
            _currentPercentage = 0f;
            image.fillAmount = _currentPercentage;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(rectTransform, starImagePrefab);
        }

        public IWildCardItemView CreateWildCardImage()
        {
            return _wildCardItemViewFactory.Spawn(wildCardHolder, wildCardItemPrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public Tween GetProgressTween(float targetPercentage, float duration, Action onComplete)
        {
             return DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .Pause().SetEase(Ease.OutQuad)
                .OnUpdate(() => { image.fillAmount = _currentPercentage; })
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                    onComplete?.Invoke();
                });
        }

        public RectTransform GetTempWildCardHolder()
        {
            return tempWildCardHolder;
        }
    }

    public interface ICircleProgressBarView
    {
        void Init(StarImageViewFactory starImageViewFactory, WildCardItemViewFactory wildCardItemViewFactory);
        IStarImageView CreateStarImage();
        RectTransform GetRectTransform();
        Tween GetProgressTween(float targetPercentage, float duration, Action onComplete);
        IWildCardItemView CreateWildCardImage();
        RectTransform GetTempWildCardHolder();
    }
}
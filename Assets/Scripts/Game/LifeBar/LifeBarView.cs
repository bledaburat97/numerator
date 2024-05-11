using System;
using UnityEngine;
using DG.Tweening;

namespace Scripts
{
    public class LifeBarView : MonoBehaviour, ILifeBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private BoundaryView boundaryPrefab;
        [SerializeField] private SlicedFilledImage image;
        
        private BoundaryViewFactory _boundaryViewFactory;
        private Tween _currentTween;
        private float _currentPercentage;

        public void Init(BoundaryViewFactory boundaryViewFactory)
        {
            _boundaryViewFactory = boundaryViewFactory;
            _currentPercentage = 1f;
            image.fillAmount = _currentPercentage;
        }

        public void DisableStarProgressBar()
        {
            gameObject.SetActive(false);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public IBoundaryView CreateBoundaryView()
        {
            return _boundaryViewFactory.Spawn(transform, boundaryPrefab);
        }

        public RectTransform GetBoundaryRectTransform()
        {
            return boundaryPrefab.GetRectTransform();
        }

        public void SetProgress(float targetPercentage, float duration, Action onComplete, Action onStart)
        {
            DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => { image.fillAmount = _currentPercentage; })
                .OnStart(() => onStart?.Invoke())
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                    onComplete?.Invoke();
                });
        }
    }

    public interface ILifeBarView
    {
        void Init(BoundaryViewFactory boundaryViewFactory);
        RectTransform GetRectTransform();
        IBoundaryView CreateBoundaryView();
        RectTransform GetBoundaryRectTransform();
        void SetProgress(float targetPercentage, float duration, Action onComplete, Action onStart);
        void DisableStarProgressBar();
    }
}
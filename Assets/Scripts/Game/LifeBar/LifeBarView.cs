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
        
        private Tween _currentTween;
        private float _currentPercentage;

        public void Init()
        {
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
            return Instantiate(boundaryPrefab, transform);
        }

        public RectTransform GetBoundaryRectTransform()
        {
            return boundaryPrefab.GetRectTransform();
        }

        public void InitProgress(float targetPercentage)
        {
            _currentPercentage = targetPercentage;
            image.fillAmount = _currentPercentage;
        }

        public void SetProgress(float targetPercentage, float duration, Action onComplete)
        {
            DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => { image.fillAmount = _currentPercentage; })
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                    onComplete?.Invoke();
                });
        }
    }

    public interface ILifeBarView
    {
        void Init();
        RectTransform GetRectTransform();
        IBoundaryView CreateBoundaryView();
        RectTransform GetBoundaryRectTransform();
        void SetProgress(float targetPercentage, float duration, Action onComplete);
        void DisableStarProgressBar();
        void InitProgress(float targetPercentage);
    }
}
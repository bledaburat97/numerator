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
        [SerializeField] private CanvasGroup canvasGroup;
        
        private float _currentPercentage;
        
        public CanvasGroup GetCanvasGroup() => canvasGroup;

        public void DisableProgressBar()
        {
            gameObject.SetActive(false);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public RectTransform GetFilledImageRectTransform()
        {
            return image.rectTransform;
        }

        public IBoundaryView CreateBoundaryView()
        {
            return Instantiate(boundaryPrefab, image.rectTransform);
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

        public Tween SetProgress(float targetPercentage, float duration, Action onComplete)
        {
            return DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => { image.fillAmount = _currentPercentage; })
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                    onComplete?.Invoke();
                }).Pause();
        }
    }

    public interface ILifeBarView
    {
        RectTransform GetRectTransform();
        RectTransform GetFilledImageRectTransform();
        IBoundaryView CreateBoundaryView();
        RectTransform GetBoundaryRectTransform();
        Tween SetProgress(float targetPercentage, float duration, Action onComplete);
        void DisableProgressBar();
        void InitProgress(float targetPercentage);
        CanvasGroup GetCanvasGroup();
    }
}

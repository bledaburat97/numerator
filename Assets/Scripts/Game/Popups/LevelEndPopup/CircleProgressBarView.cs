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
        [SerializeField] private Image image;
        private Tween _currentTween;
        private float _currentPercentage;
        [SerializeField] private ParticleSystem particle;
        
        public void Init(StarImageViewFactory starImageViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _currentPercentage = 0f;
            image.fillAmount = _currentPercentage;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(rectTransform, starImagePrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public Tween GetProgressTween(float targetPercentage, float duration)
        {
             return DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .Pause().SetEase(Ease.OutQuad)
                .OnUpdate(() => { image.fillAmount = _currentPercentage; })
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                });
        }

        public void SetProgress(float targetPercentage)
        {
            image.fillAmount = targetPercentage;
            _currentPercentage = targetPercentage;
        }
        
        public void ActivateWildParticle()
        {
            particle.gameObject.SetActive(true);
            particle.Play();
        }

        public void DeactivateWildParticle()
        {
            particle.Stop();
        }
    }

    public interface ICircleProgressBarView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        IStarImageView CreateStarImage();
        RectTransform GetRectTransform();
        Tween GetProgressTween(float targetPercentage, float duration);
        void SetProgress(float targetPercentage);
        void ActivateWildParticle();
        void DeactivateWildParticle();
    }
}
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        [SerializeField] private ParticleSystem[] starParticleList;
        [SerializeField] private ParticleSystem wildParticle;
        [SerializeField] private CanvasGroup starGroup;
        [SerializeField] private RectTransform wildCardHolder;
        [SerializeField] private FadeButtonView continueButtonView;
        [SerializeField] private FadeButtonView retryButtonView;
        [SerializeField] private FadeButtonView claimButtonView;
        [SerializeField] private StarImageView[] starList;
        
        //[SerializeField] private GlowingCircleProgressBarView glowingCircleProgressBar;
        [SerializeField] private TMP_Text title;
        
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public FadeButtonView GetContinueButton()
        {
            return continueButtonView;
        }

        public FadeButtonView GetRetryButton()
        {
            return retryButtonView;
        }

        public FadeButtonView GetClaimButton()
        {
            return claimButtonView;
        }

        public StarImageView[] GetStarList()
        {
            return starList;
        }

        public ParticleSystem[] GetStarParticleList()
        {
            return starParticleList;
        }
        
        public ICircleProgressBarView CreateCircleProgressBar()
        {
            return circleProgressBarView;
        }
        
        public void ActivateWildParticle()
        {
            wildParticle.gameObject.SetActive(true);
            wildParticle.Play();
        }

        public void SetStarGroupStatus(bool status)
        {
            starGroup.alpha = status ? 1f : 0f;
        }

        public CanvasGroup GetStarGroup()
        {
            return starGroup;
        }
        
        public void SetTitle(string text)
        {
            title.SetText(text);
            title.transform.localScale = Vector3.zero;
        }
        
        public RectTransform GetTitle()
        {
            return title.rectTransform;
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init();
        ICircleProgressBarView CreateCircleProgressBar();
        void ActivateWildParticle();
        void SetStarGroupStatus(bool status);
        CanvasGroup GetStarGroup();
        void SetTitle(string text);
        RectTransform GetTitle();
        FadeButtonView GetContinueButton();
        FadeButtonView GetRetryButton();
        FadeButtonView GetClaimButton();
        StarImageView[] GetStarList();
        ParticleSystem[] GetStarParticleList();
    }
}
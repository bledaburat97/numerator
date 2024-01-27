using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GlowingLevelEndPopupView : MonoBehaviour, IGlowingLevelEndPopupView
    {
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private GlowingCircleProgressBarView glowingCircleProgressBar;
        [SerializeField] private TMP_Text title;
        [SerializeField] private RectTransform wildCardHolder;
        [SerializeField] private WildCardItemView wildCardItemPrefab;
        [SerializeField] private CanvasGroup starGroup;
        private WildCardItemViewFactory _wildCardItemViewFactory;
        
        private StarImageViewFactory _starImageViewFactory;
        
        private List<IStarImageView> _glowingStarImageList;
        
        public void Init(StarImageViewFactory starImageViewFactory, WildCardItemViewFactory wildCardItemViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _glowingStarImageList = new List<IStarImageView>();
            _wildCardItemViewFactory = wildCardItemViewFactory;
        }
        
        public void CreateStarImage(Vector2 localPosition, Vector2 size)
        {
            IStarImageView starImageView = _starImageViewFactory.Spawn(starHolder, starImagePrefab);
            starImageView.SetLocalPosition(localPosition);
            starImageView.SetSize(size);
            starImageView.SetLocalScale(Vector2.zero);
            _glowingStarImageList.Add(starImageView);
        }
        
        public IGlowingCircleProgressBarView CreateGlowingCircleProgressBar()
        {
            return glowingCircleProgressBar;
        }

        public GlowingEndGameAnimationModel GetGlowingAnimationModel()
        {
            return new GlowingEndGameAnimationModel()
            {
                starImageViewList = _glowingStarImageList
            };
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
        
        public IWildCardItemView CreateWildCardImage()
        {
            return _wildCardItemViewFactory.Spawn(wildCardHolder, wildCardItemPrefab);
        }
        
        public void SetStarGroupStatus(bool status)
        {
            starGroup.alpha = status ? 1f : 0f;
        }
        
        public CanvasGroup GetStarGroup()
        {
            return starGroup;
        }
    }

    public interface IGlowingLevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory, WildCardItemViewFactory wildCardItemViewFactory);
        void CreateStarImage(Vector2 localPosition, Vector2 size);
        IGlowingCircleProgressBarView CreateGlowingCircleProgressBar();
        GlowingEndGameAnimationModel GetGlowingAnimationModel();
        void SetTitle(string text);
        RectTransform GetTitle();
        IWildCardItemView CreateWildCardImage();
        void SetStarGroupStatus(bool status);
        CanvasGroup GetStarGroup();
    }

    public class GlowingEndGameAnimationModel
    {
        public List<IStarImageView> starImageViewList;
    }
}
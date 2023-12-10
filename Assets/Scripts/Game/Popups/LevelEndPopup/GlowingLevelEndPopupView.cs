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

        private StarImageViewFactory _starImageViewFactory;
        
        private List<IStarImageView> _glowingStarImageList;
        
        public void Init(StarImageViewFactory starImageViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _glowingStarImageList = new List<IStarImageView>();
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
            title.alpha = 0f;
        }
        
        public TMP_Text GetTitle()
        {
            return title;
        }
    }

    public interface IGlowingLevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        void CreateStarImage(Vector2 localPosition, Vector2 size);
        IGlowingCircleProgressBarView CreateGlowingCircleProgressBar();
        GlowingEndGameAnimationModel GetGlowingAnimationModel();
        void SetTitle(string text);
        TMP_Text GetTitle();

    }

    public class GlowingEndGameAnimationModel
    {
        public List<IStarImageView> starImageViewList;
    }
}
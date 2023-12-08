using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class GlowingLevelEndPopupView : MonoBehaviour, IGlowingLevelEndPopupView
    {
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private GlowingCircleProgressBarView glowingCircleProgressBar;
        private StarImageViewFactory _starImageViewFactory;
        
        private List<IStarImageView> _glowingStarImageList;
        
        public void Init(StarImageViewFactory starImageViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            _glowingStarImageList = new List<IStarImageView>();
        }
        
        public void CreateStarImage(Vector2 localPosition, Vector2 size, float alpha)
        {
            IStarImageView starImageView = _starImageViewFactory.Spawn(starHolder, starImagePrefab);
            starImageView.Init(localPosition);
            starImageView.SetSize(size);
            starImageView.SetAlpha(alpha);
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
    }

    public interface IGlowingLevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        void CreateStarImage(Vector2 localPosition, Vector2 size, float alpha);
        IGlowingCircleProgressBarView CreateGlowingCircleProgressBar();
        GlowingEndGameAnimationModel GetGlowingAnimationModel();
    }

    public class GlowingEndGameAnimationModel
    {
        public List<IStarImageView> starImageViewList;
    }
}
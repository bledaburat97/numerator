using UnityEngine;

namespace Scripts
{
    public class GlowingCircleProgressBarView : MonoBehaviour, IGlowingCircleProgressBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private StarImageView starImagePrefab;
        private StarImageViewFactory _starImageViewFactory;
        [SerializeField] private RectTransform wildCardHolder;
        [SerializeField] private RectTransform tempWildCardHolder;
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private WildCardItemView wildCardItemPrefab;
        private WildCardItemViewFactory _wildCardItemViewFactory;
        
        public void Init(StarImageViewFactory starImageViewFactory, WildCardItemViewFactory wildCardItemViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _wildCardItemViewFactory = wildCardItemViewFactory;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(starHolder, starImagePrefab);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public IWildCardItemView CreateWildCardImage()
        {
            return _wildCardItemViewFactory.Spawn(wildCardHolder, wildCardItemPrefab);
        }
        
        public RectTransform GetTempWildCardHolder()
        {
            return tempWildCardHolder;
        }
    }

    public interface IGlowingCircleProgressBarView
    {
        void Init(StarImageViewFactory starImageViewFactory, WildCardItemViewFactory wildCardItemViewFactory);
        IStarImageView CreateStarImage();
        RectTransform GetRectTransform();
        IWildCardItemView CreateWildCardImage();
        RectTransform GetTempWildCardHolder();
    }
}
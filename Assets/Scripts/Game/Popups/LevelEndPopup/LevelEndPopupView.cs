using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private PlayButtonView playButtonView;
        [SerializeField] private ReturnMenuButtonView returnMenuButtonView;
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;

        private StarImageViewFactory _starImageViewFactory;
        public void Init(StarImageViewFactory starImageViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public void SetTitle(string text)
        {
            title.SetText(text);
        }

        public IPlayButtonView GetPlayButtonView()
        {
            return playButtonView;
        }

        public IReturnMenuButtonView GetReturnMenuButtonView()
        {
            return returnMenuButtonView;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(starHolder, starImagePrefab);
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        void SetTitle(string text);
        IPlayButtonView GetPlayButtonView();
        IReturnMenuButtonView GetReturnMenuButtonView();
        IStarImageView CreateStarImage();
    }
}
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private BaseButtonView playButtonView;
        [SerializeField] private BaseButtonView returnMenuButtonView;
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        
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

        public IBaseButtonView GetPlayButtonView()
        {
            return playButtonView;
        }

        public IBaseButtonView GetReturnMenuButtonView()
        {
            return returnMenuButtonView;
        }
        
        public IStarImageView CreateStarImage()
        {
            return _starImageViewFactory.Spawn(starHolder, starImagePrefab);
        }

        public ICircleProgressBarView CreateCircleProgressBar()
        {
            return circleProgressBarView;
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init(StarImageViewFactory starImageViewFactory);
        void SetTitle(string text);
        IBaseButtonView GetPlayButtonView();
        IBaseButtonView GetReturnMenuButtonView();
        IStarImageView CreateStarImage();
        ICircleProgressBarView CreateCircleProgressBar();
    }
}
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private BaseButtonView buttonPrefab;
        [SerializeField] private RectTransform starHolder;
        [SerializeField] private StarImageView starImagePrefab;
        [SerializeField] private CircleProgressBarView circleProgressBarView;
        
        private StarImageViewFactory _starImageViewFactory;
        private BaseButtonViewFactory _baseButtonViewFactory;
        public void Init(StarImageViewFactory starImageViewFactory, BaseButtonViewFactory baseButtonViewFactory)
        {
            _starImageViewFactory = starImageViewFactory;
            _baseButtonViewFactory = baseButtonViewFactory;
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public void SetTitle(string text)
        {
            title.SetText(text);
        }

        public IBaseButtonView GetButtonView()
        {
            return _baseButtonViewFactory.Spawn(transform, buttonPrefab);
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
        void Init(StarImageViewFactory starImageViewFactory, BaseButtonViewFactory baseButtonViewFactory);
        void SetTitle(string text);
        IBaseButtonView GetButtonView();
        IStarImageView CreateStarImage();
        ICircleProgressBarView CreateCircleProgressBar();
    }
}
using UnityEngine;

namespace Scripts
{
    public class PlayButtonView : BaseButtonView, IPlayButtonView
    {
        [SerializeField] private CanvasGroup canvasGroup;

        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }

        public CanvasGroup GetCanvasGroup()
        {
            return canvasGroup;
        }
        
        public void Destroy()
        {
            Destroy(gameObject);
        }
    }

    public interface IPlayButtonView : IBaseButtonView
    {
        void SetAlpha(float alpha);
        CanvasGroup GetCanvasGroup();
        void Destroy();
    }
}
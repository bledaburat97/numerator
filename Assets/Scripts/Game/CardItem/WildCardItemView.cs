using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class WildCardItemView : DraggableCardItemView, IWildCardItemView
    {
        private Vector3 _localPositionGap = Vector2.zero;
        [SerializeField] private Image questionMarkImage;
        [SerializeField] private CanvasGroup canvasGroup;
        
        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetLocalPositionGap(int cardItemIndex)
        {
            _localPositionGap += new Vector3(-3.5f, -2f, 0f) * cardItemIndex;
        }
        
        public override void InitPosition()
        {
            rectTransform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero + _localPositionGap;
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public CanvasGroup GetCanvasGroup()
        {
            return canvasGroup;
        }
    }

    public interface IWildCardItemView : IDraggableCardItemView
    {
        void Destroy();
        void SetLocalPositionGap(int cardItemIndex);
        RectTransform GetRectTransform();
        CanvasGroup GetCanvasGroup();
    }
}
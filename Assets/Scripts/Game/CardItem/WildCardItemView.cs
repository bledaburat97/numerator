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

        public override void SetLocalPosition(Vector3 localPosition)
        {
            rectTransform.localPosition = localPosition + _localPositionGap;
        }
        
        public override Sequence ChangeLocalPosition(Vector3 localPosition, float duration)
        {
            return DOTween.Sequence().Append(rectTransform.DOLocalMove(localPosition + _localPositionGap, duration)).SetEase(Ease.OutQuad);
        }
        
        public override Sequence ChangePosition(Vector3 position, float duration)
        {
            return DOTween.Sequence().Append(rectTransform.DOMove(position, duration)).SetEase(Ease.OutQuad);
        } 

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public CanvasGroup GetCanvasGroup()
        {
            return canvasGroup;
        }

        public void SetLocalScale(Vector3 localScale)
        {
            transform.localScale = localScale;
        }
    }

    public interface IWildCardItemView : IDraggableCardItemView
    {
        void Destroy();
        void SetLocalPositionGap(int cardItemIndex);
        RectTransform GetRectTransform();
        CanvasGroup GetCanvasGroup();
        void SetLocalScale(Vector3 localScale);
    }
}
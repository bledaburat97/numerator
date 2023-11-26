using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class WildCardItemView : DraggableCardItemView, IWildCardItemView
    {
        private Vector3 _localPositionGap = Vector2.zero;
        [SerializeField] private Image questionMarkImage;
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
        
        public Tween MoveWildCard(RectTransform tempRectTransform)
        {
            transform.SetParent(tempRectTransform);
            return DOTween.Sequence().Append(transform.DOScale(Vector3.one * 5/3f, 1.5f))
                .Join(DOTween.Sequence().AppendInterval(0.5f).Append(transform.DOLocalMoveY(-190f, 1f)))
                .AppendInterval(0.5f)
                .Append(FadeOutAnimation(1f))
                .OnComplete(() => Destroy(gameObject));
        }

        private Tween FadeOutAnimation(float duration)
        {
            return DOTween.Sequence().Append(innerBGImage.DOFade(0f, duration))
                .Join(outerBGImage.DOFade(0f, duration))
                .Join(shadowImage.DOFade(0f, duration))
                .Join(questionMarkImage.DOFade(0f, duration));
        }
        
    }

    public interface IWildCardItemView : IDraggableCardItemView
    {
        void Destroy();
        void SetLocalPositionGap(int cardItemIndex);
        Tween MoveWildCard(RectTransform tempRectTransform);
    }
}
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public abstract class RewardTokenView : MonoBehaviour, IRewardTokenView
    {
        [SerializeField] private Image image;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CurvedAnimationPreset curvedAnimationPreset;

        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetLocalScale(Vector3 localScale)
        {
            transform.localScale = localScale;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetSize(Vector2 size)
        {
            if (rectTransform == null) return;

            rectTransform.sizeDelta = size;
        }

        public void SetStatus(bool status)
        {
            if (image == null) return;

            image.gameObject.SetActive(status);

            if (status)
            {
                SetImageAlpha(1f);
            }
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform != null ? rectTransform : transform as RectTransform;
        }

        public void SetParent(RectTransform parent)
        {
            RectTransform targetRectTransform = GetRectTransform();
            if (targetRectTransform == null) return;

            targetRectTransform.SetParent(parent);
        }

        public CurvedAnimationPreset GetCurvedAnimationPreset()
        {
            return curvedAnimationPreset;
        }

        public Sequence AnimateFadeOut(float duration)
        {
            if (image == null) return DOTween.Sequence();

            SetStatus(true);
            return DOTween.Sequence()
                .Append(image.DOFade(0f, duration))
                .OnComplete(() => SetStatus(false));
        }

        public Tween AnimatePunchScale(Vector3 punch, float duration, int vibrato, float elasticity)
        {
            RectTransform targetRectTransform = GetRectTransform();
            return targetRectTransform == null
                ? DOTween.Sequence()
                : targetRectTransform.DOPunchScale(punch, duration, vibrato, elasticity);
        }

        private void SetImageAlpha(float alpha)
        {
            if (image == null) return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    public interface IRewardTokenView
    {
        void SetLocalPosition(Vector2 localPosition);
        void SetLocalScale(Vector3 localScale);
        void SetStatus(bool status);
        void SetSize(Vector2 size);
        RectTransform GetRectTransform();
        void SetParent(RectTransform parent);
        void Destroy();
        CurvedAnimationPreset GetCurvedAnimationPreset();
        Sequence AnimateFadeOut(float duration);
        Tween AnimatePunchScale(Vector3 punch, float duration, int vibrato, float elasticity);
    }
}

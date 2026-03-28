using DG.Tweening;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class StarImageView : MonoBehaviour, IStarImageView
    {
        [SerializeField] private Image star;
        [SerializeField] private Image frame;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CurvedAnimationPreset curvedAnimationPreset;
        [SerializeField] private MovingRewardItemView movingRewardItemPrefab;

        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public MovingRewardItemView SpawnTransientMovingRewardItem(Vector2 size)
        {
            if (movingRewardItemPrefab == null) return null;

            MovingRewardItemView movingRewardItemView = Instantiate(movingRewardItemPrefab, rectTransform);
            movingRewardItemView.Init(rectTransform);
            movingRewardItemView.SetSize(size);
            movingRewardItemView.SetStatus(true);
            movingRewardItemView.SnapToCenter();
            return movingRewardItemView;
        }

        public Sequence AnimateRewardActivation(Vector2 movingRewardItemSize)
        {
            SetStarStatus(true);
            SetColor(true);
            MovingRewardItemView movingRewardItemView = SpawnTransientMovingRewardItem(movingRewardItemSize);
            if (movingRewardItemView == null)
            {
                return DOTween.Sequence();
            }

            RectTransform movingRewardItemRectTransform = movingRewardItemView.GetRectTransform();
            float startOffsetY = Mathf.Max(rectTransform.rect.height, movingRewardItemSize.y) * 1.15f;
            movingRewardItemRectTransform.anchoredPosition = new Vector2(0f, startOffsetY);

            return DOTween.Sequence()
                .Append(movingRewardItemView.AnimateSpawn(0.18f))
                .Join(movingRewardItemRectTransform.DOAnchorPos(Vector2.zero, 1f).SetEase(Ease.OutCubic))
                .AppendCallback(() => SetColor(false))
                .Append(rectTransform.DOPunchScale(Vector3.one * 0.08f, 0.16f, 4, 0.65f))
                .Append(movingRewardItemView.AnimateDissolve(0.18f, 1.05f))
                .OnComplete(movingRewardItemView.DestroyObject);
        }

        public MovingRewardItemView GetMovingRewardItemPrefab()
        {
            return movingRewardItemPrefab;
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
            rectTransform.sizeDelta = size;
        }
        
        public void SetStarStatus(bool status)
        {
            star.gameObject.SetActive(status);
            if (frame != null)
            {
                frame.gameObject.SetActive(status);
            }

            if (status)
            {
                SetImageAlpha(star, 1f);
                SetImageAlpha(frame, 1f);
            }
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void SetParent(RectTransform parent)
        {
            rectTransform.SetParent(parent);
        }

        public CurvedAnimationPreset GetCurvedAnimationPreset()
        {
            return curvedAnimationPreset;
        }

        public void SetColor(bool originalColor)
        {
            star.color = originalColor ? ConstantValues.YELLOW_STAR_COLOR : ConstantValues.BLUE_STAR_COLOR;
            SetImageAlpha(star, 1f);
        }

        public Tween AnimateColor(bool originalColor, float duration)
        {
            SetStarStatus(true);
            return star.DOColor(originalColor ? ConstantValues.YELLOW_STAR_COLOR : ConstantValues.BLUE_STAR_COLOR, duration);
        }

        public Sequence AnimateFadeOut(float duration)
        {
            SetStarStatus(true);
            Sequence sequence = DOTween.Sequence()
                .Append(star.DOFade(0f, duration));

            if (frame != null)
            {
                sequence.Join(frame.DOFade(0f, duration));
            }

            return sequence.OnComplete(() => SetStarStatus(false));
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null) return;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    public interface IStarImageView
    {
        void SetLocalPosition(Vector2 localPosition);
        void SetLocalScale(Vector3 localScale);
        void SetStarStatus(bool status);
        void SetSize(Vector2 size);
        RectTransform GetRectTransform();
        void SetParent(RectTransform parent);
        void Destroy();
        CurvedAnimationPreset GetCurvedAnimationPreset();
        void SetColor(bool originalColor);
        Tween AnimateColor(bool originalColor, float duration);
        Sequence AnimateFadeOut(float duration);
        Sequence AnimateRewardActivation(Vector2 movingRewardItemSize);
        MovingRewardItemView SpawnTransientMovingRewardItem(Vector2 size);
        MovingRewardItemView GetMovingRewardItemPrefab();
    }
}

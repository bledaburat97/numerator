using System;
using DG.Tweening;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class VerticalCrystalProgressView : MonoBehaviour, IVerticalCrystalProgressView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private SlicedFilledImage progressImage;
        [SerializeField] private PowerUpButtonView revealingPowerUpButton;
        [SerializeField] private PowerUpButtonView lifePowerUpButton;
        [SerializeField] private PowerUpButtonView bombPowerUpButton;
        [SerializeField] private ParticleSystem completedParticle;
        [SerializeField] private Image crystalIcon;

        private float _currentPercentage;
        private RectTransform _presentedRewardPreview;
        private Transform _presentedRewardOriginalParent;
        private int _presentedRewardOriginalSiblingIndex;
        private Vector2 _presentedRewardOriginalAnchorMin;
        private Vector2 _presentedRewardOriginalAnchorMax;
        private Vector2 _presentedRewardOriginalPivot;
        private Vector2 _presentedRewardOriginalAnchoredPosition;
        private Vector2 _presentedRewardOriginalSizeDelta;
        private Vector3 _presentedRewardOriginalLocalScale;

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public CanvasGroup GetCanvasGroup()
        {
            return canvasGroup;
        }

        public void SetStatus(bool status)
        {
            if (!status)
            {
                RestorePresentedRewardPreview();
            }

            gameObject.SetActive(status);
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = alpha;
        }

        public Sequence ChangeFade(float duration, float finalAlpha)
        {
            if (canvasGroup == null) return DOTween.Sequence();

            return DOTween.Sequence().Append(canvasGroup.DOFade(finalAlpha, duration));
        }

        public void ResetFill()
        {
            InitProgress(0f);
        }

        public void InitProgress(float targetPercentage)
        {
            _currentPercentage = Mathf.Clamp01(targetPercentage);
            if (progressImage == null) return;

            progressImage.fillAmount = _currentPercentage;
        }

        public Tween SetProgress(float targetPercentage, float duration, Action onComplete)
        {
            if (progressImage == null)
            {
                return DOTween.Sequence()
                    .AppendCallback(() => onComplete?.Invoke())
                    .Pause();
            }

            float clampedTargetPercentage = Mathf.Clamp01(targetPercentage);
            return DOTween.To(() => _currentPercentage, x => _currentPercentage = x, clampedTargetPercentage, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => { progressImage.fillAmount = _currentPercentage; })
                .OnComplete(() =>
                {
                    _currentPercentage = clampedTargetPercentage;
                    onComplete?.Invoke();
                }).Pause();
        }

        public Sequence PunchBar(float duration)
        {
            if (rectTransform == null) return DOTween.Sequence();

            return DOTween.Sequence()
                .Append(rectTransform.DOPunchScale(Vector3.one * 0.06f, duration, 5, 0.65f));
        }

        public Sequence PlayCompletedFeedback(float duration)
        {
            if (rectTransform == null) return DOTween.Sequence();

            Sequence sequence = DOTween.Sequence()
                .Append(rectTransform.DOPunchScale(Vector3.one * 0.12f, duration, 6, 0.75f));

            if (completedParticle != null)
            {
                sequence.InsertCallback(0f, completedParticle.Play);
            }

            return sequence;
        }

        public Sequence PlayRewardPreviewCompleted(RewardType rewardType, float duration)
        {
            RectTransform rewardPreviewRectTransform = GetRewardPreviewRectTransform(rewardType);
            if (rewardPreviewRectTransform == null) return DOTween.Sequence();

            return DOTween.Sequence()
                .AppendCallback(() => rewardPreviewRectTransform.localScale = Vector3.zero)
                .Append(rewardPreviewRectTransform.DOScale(Vector3.one * 1.1f, duration * 0.6f).SetEase(Ease.OutBack))
                .Append(rewardPreviewRectTransform.DOScale(Vector3.one, duration * 0.4f).SetEase(Ease.OutQuad));
        }

        public Sequence PresentRewardPreviewAtCenter(RewardType rewardType, float duration)
        {
            RectTransform rewardPreviewRectTransform = GetRewardPreviewButtonRectTransform(rewardType);
            RectTransform presentationParent = GetPresentationParent();
            if (rewardPreviewRectTransform == null || presentationParent == null) return DOTween.Sequence();

            return DOTween.Sequence()
                .AppendCallback(() =>
                {
                    ShowRewardPreview(rewardType);
                    CachePresentedRewardPreview(rewardPreviewRectTransform);
                    rewardPreviewRectTransform.SetParent(presentationParent, true);
                    rewardPreviewRectTransform.anchorMin = Vector2.one * 0.5f;
                    rewardPreviewRectTransform.anchorMax = Vector2.one * 0.5f;
                    rewardPreviewRectTransform.pivot = Vector2.one * 0.5f;
                    rewardPreviewRectTransform.SetAsLastSibling();
                })
                .Append(rewardPreviewRectTransform.DOLocalMove(Vector3.zero, duration).SetEase(Ease.OutQuad))
                .Join(rewardPreviewRectTransform.DOScale(Vector3.one * 1.15f, duration * 0.65f)
                    .SetEase(Ease.OutBack))
                .Append(rewardPreviewRectTransform.DOScale(Vector3.one, duration * 0.35f).SetEase(Ease.OutQuad));
        }

        public Sequence HidePresentedRewardPreview(float duration)
        {
            if (_presentedRewardPreview == null) return DOTween.Sequence();

            RectTransform rewardPreviewRectTransform = _presentedRewardPreview;
            return DOTween.Sequence()
                .Append(rewardPreviewRectTransform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack))
                .AppendCallback(RestorePresentedRewardPreview);
        }

        public void ShowRewardPreview(RewardType rewardType)
        {
            SetPowerUpButtonStatus(revealingPowerUpButton, rewardType == RewardType.Revealing);
            SetPowerUpButtonStatus(lifePowerUpButton, rewardType == RewardType.Life);
            SetPowerUpButtonStatus(bombPowerUpButton, rewardType == RewardType.Bomb);
        }

        public RectTransform GetRewardPreviewRectTransform(RewardType rewardType)
        {
            IBaseButtonView buttonView = GetPowerUpButton(rewardType);
            return buttonView?.GetRectTransform();
        }

        public RectTransform GetCrystalIconRectTransform()
        {
            return crystalIcon != null ? crystalIcon.rectTransform : rectTransform;
        }

        public void RestorePresentedRewardPreview()
        {
            if (_presentedRewardPreview == null) return;

            RectTransform rewardPreviewRectTransform = _presentedRewardPreview;
            if (_presentedRewardOriginalParent != null)
            {
                rewardPreviewRectTransform.SetParent(_presentedRewardOriginalParent, false);
                int siblingIndex = Mathf.Min(_presentedRewardOriginalSiblingIndex,
                    _presentedRewardOriginalParent.childCount - 1);
                rewardPreviewRectTransform.SetSiblingIndex(Mathf.Max(0, siblingIndex));
            }

            rewardPreviewRectTransform.anchorMin = _presentedRewardOriginalAnchorMin;
            rewardPreviewRectTransform.anchorMax = _presentedRewardOriginalAnchorMax;
            rewardPreviewRectTransform.pivot = _presentedRewardOriginalPivot;
            rewardPreviewRectTransform.anchoredPosition = _presentedRewardOriginalAnchoredPosition;
            rewardPreviewRectTransform.sizeDelta = _presentedRewardOriginalSizeDelta;
            rewardPreviewRectTransform.localScale = _presentedRewardOriginalLocalScale;
            _presentedRewardPreview = null;
            _presentedRewardOriginalParent = null;
        }

        private IBaseButtonView GetPowerUpButton(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.Revealing:
                    return revealingPowerUpButton;
                case RewardType.Life:
                    return lifePowerUpButton;
                case RewardType.Bomb:
                    return bombPowerUpButton;
                default:
                    return revealingPowerUpButton;
            }
        }

        private RectTransform GetRewardPreviewButtonRectTransform(RewardType rewardType)
        {
            IBaseButtonView buttonView = GetPowerUpButton(rewardType);
            return buttonView?.GetButtonRectTransform() ?? buttonView?.GetRectTransform();
        }

        private RectTransform GetPresentationParent()
        {
            if (rectTransform != null && rectTransform.parent is RectTransform parent)
            {
                return parent;
            }

            return transform.parent as RectTransform;
        }

        private void CachePresentedRewardPreview(RectTransform rewardPreviewRectTransform)
        {
            if (_presentedRewardPreview != null && _presentedRewardPreview != rewardPreviewRectTransform)
            {
                RestorePresentedRewardPreview();
            }

            _presentedRewardPreview = rewardPreviewRectTransform;
            _presentedRewardOriginalParent = rewardPreviewRectTransform.parent;
            _presentedRewardOriginalSiblingIndex = rewardPreviewRectTransform.GetSiblingIndex();
            _presentedRewardOriginalAnchorMin = rewardPreviewRectTransform.anchorMin;
            _presentedRewardOriginalAnchorMax = rewardPreviewRectTransform.anchorMax;
            _presentedRewardOriginalPivot = rewardPreviewRectTransform.pivot;
            _presentedRewardOriginalAnchoredPosition = rewardPreviewRectTransform.anchoredPosition;
            _presentedRewardOriginalSizeDelta = rewardPreviewRectTransform.sizeDelta;
            _presentedRewardOriginalLocalScale = rewardPreviewRectTransform.localScale;
        }

        private static void SetPowerUpButtonStatus(PowerUpButtonView buttonView, bool status)
        {
            if (buttonView == null) return;

            buttonView.SetButtonStatus(status);
        }
    }

    public interface IVerticalCrystalProgressView
    {
        RectTransform GetRectTransform();
        CanvasGroup GetCanvasGroup();
        void SetStatus(bool status);
        void SetAlpha(float alpha);
        Sequence ChangeFade(float duration, float finalAlpha);
        void ResetFill();
        Sequence PunchBar(float duration);
        Sequence PlayCompletedFeedback(float duration);
        Sequence PlayRewardPreviewCompleted(RewardType rewardType, float duration);
        Sequence PresentRewardPreviewAtCenter(RewardType rewardType, float duration);
        Sequence HidePresentedRewardPreview(float duration);
        void ShowRewardPreview(RewardType rewardType);
        RectTransform GetRewardPreviewRectTransform(RewardType rewardType);
        RectTransform GetCrystalIconRectTransform();
        void RestorePresentedRewardPreview();
        Tween SetProgress(float targetPercentage, float duration, Action onComplete);
        void InitProgress(float targetPercentage);
    }
}

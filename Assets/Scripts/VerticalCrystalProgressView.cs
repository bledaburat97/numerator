using System;
using DG.Tweening;
using Game;
using TMPro;
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
        private TMP_Text _countLabel, _rewardLabel;
        private RectTransform _presentationTarget;
        private RewardType _rewardType;
        private void Awake() => ApplyLabStyle();

        public void SetPresentationTarget(RectTransform target) => _presentationTarget = target;

        public void ApplyLabStyle()
        {
            if (rectTransform == null) rectTransform = (RectTransform)transform;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            float width = rectTransform.rect.width;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Tile);
            FruitLabStyle.Rim(rectTransform);
            TMP_Text title = GetLabel("CrystalTitle", 13f, FruitLabStyle.Text);
            title.fontSize = 13f;
            title.gameObject.SetActive(true);
            title.text = "Kristal enerjisi";
            title.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(title.rectTransform, transform, 14f, 76f, width - 88f, 24f);
            _countLabel = GetLabel("CrystalCount", 14f, FruitLabStyle.Pink);
            _countLabel.fontSize = 14f;
            _countLabel.alignment = TextAlignmentOptions.MidlineRight;
            FruitLabStyle.Place(_countLabel.rectTransform, transform, width - 70f, 76f, 56f, 24f);
            if (crystalIcon != null)
            {
                FruitLabStyle.SetIcon(crystalIcon, "Crystal");
                FruitLabStyle.Place(crystalIcon.rectTransform, crystalIcon.transform.parent, 12f, 27f, 36f, 36f);
                crystalIcon.raycastTarget = false;
            }
            Image track = GetSurface("CrystalTrack");
            FruitLabStyle.RecessSurface(track);
            FruitLabStyle.Place(track.rectTransform, transform, 58f, 40f, width - 132f, 14f);
            Image glass = GetSurface("GlassRim");
            FruitLabStyle.Surface(glass, new Color(.72f, .87f, .82f, .7f), true);
            FruitLabStyle.Place(glass.rectTransform, transform, 57f, 39f, width - 130f, 16f);
            if (progressImage != null)
            {
                FruitLabStyle.Place(progressImage.rectTransform, progressImage.transform.parent, 60f, 42f, width - 136f, 10f);
                progressImage.sprite = FruitLabStyle.Rounded;
                progressImage.color = FruitLabStyle.Pink;
                progressImage.pixelsPerUnitMultiplier = 1.5f;
                progressImage.fillDirection = SlicedFilledImage.FillDirection.Right;
                progressImage.raycastTarget = false;
                track.transform.SetSiblingIndex(1);
                progressImage.transform.SetSiblingIndex(2);
            }
            Image shine = GetSurface("GlassShine");
            FruitLabStyle.Surface(shine, new Color(1f, 1f, 1f, .15f));
            FruitLabStyle.Place(shine.rectTransform, transform, 64f, 48f, Mathf.Max(1f, width - 144f), 2f);
            for (int index = 1; index < ConstantValues.NUM_OF_STARS_FOR_WILD; index++)
            {
                Image tick = GetSurface("CrystalTick" + index);
                FruitLabStyle.Surface(tick, new Color(.08f, .18f, .19f, .32f));
                FruitLabStyle.Place(tick.rectTransform, transform, 60f + (width - 136f) * index / ConstantValues.NUM_OF_STARS_FOR_WILD, 43f, 1f, 8f);
            }
            _rewardLabel = GetLabel("NextReward", 11f, FruitLabStyle.Muted);
            _rewardLabel.fontSize = 11f;
            _rewardLabel.gameObject.SetActive(true);
            _rewardLabel.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(_rewardLabel.rectTransform, transform, 58f, 12f, width - 72f, 22f);
            Transform holder = transform.Find("RewardHolder");
            if (holder == null)
            {
                GameObject item = new GameObject("RewardHolder", typeof(RectTransform));
                item.transform.SetParent(transform, false); holder = item.transform;
            }
            FruitLabStyle.Place((RectTransform)holder, transform, width - 60f, 27f, 44f, 44f);
            // Older scenes serialized prefab assets here. Always animate local instances.
            ResolvePreview(ref revealingPowerUpButton, holder, "Reveal");
            ResolvePreview(ref lifePowerUpButton, holder, "Life");
            ResolvePreview(ref bombPowerUpButton, holder, "Bomb");
            ShowRewardPreview(_rewardType);
            UpdateCountLabel();
            if (rectTransform.rect.height < 90f) ApplyCompactLayout(width, rectTransform.rect.height, title, holder);
        }

        private void ApplyCompactLayout(float width, float height, TMP_Text title, Transform holder)
        {
            bool roomy = height >= 50f;
            bool shortBar = height < 40f;
            title.gameObject.SetActive(roomy);
            title.fontSize = 10f;
            FruitLabStyle.Place(title.rectTransform, transform, 10f, height - 18f, width - 92f, 14f);
            _countLabel.fontSize = 11f;
            FruitLabStyle.Place(_countLabel.rectTransform, transform, width - 94f, shortBar ? (height - 14f) * .5f : height - 18f, 46f, 14f);
            if (crystalIcon != null) FruitLabStyle.Place(crystalIcon.rectTransform, crystalIcon.transform.parent, 8f, roomy ? 6f : (height - 26f) * .5f, 26f, 26f);
            float trackY = shortBar ? (height - 8f) * .5f - 1f : roomy ? 21f : 9f;
            float fillWidth = width - (shortBar ? 154f : 98f);
            FruitLabStyle.Place(GetSurface("CrystalTrack").rectTransform, transform, 43f, trackY - 1f, fillWidth + 4f, 12f);
            FruitLabStyle.Place(GetSurface("GlassRim").rectTransform, transform, 42f, trackY - 2f, fillWidth + 6f, 14f);
            FruitLabStyle.Place(progressImage.rectTransform, progressImage.transform.parent, 45f, trackY + 1f, fillWidth, 8f);
            FruitLabStyle.Place(GetSurface("GlassShine").rectTransform, transform, 48f, trackY + 6f, fillWidth - 6f, 1f);
            for (int index = 1; index < ConstantValues.NUM_OF_STARS_FOR_WILD; index++)
                FruitLabStyle.Place(GetSurface("CrystalTick" + index).rectTransform, transform,
                    45f + fillWidth * index / ConstantValues.NUM_OF_STARS_FOR_WILD, trackY + 2f, 1f, 6f);
            _rewardLabel.gameObject.SetActive(roomy);
            _rewardLabel.fontSize = 9f;
            FruitLabStyle.Place(_rewardLabel.rectTransform, transform, 44f, 1f, width - 94f, 14f);
            FruitLabStyle.Place((RectTransform)holder, transform, width - 40f, (height - 28f) * .5f, 28f, 28f);
        }

        private void ResolvePreview(ref PowerUpButtonView preview, Transform holder, string icon)
        {
            if (preview == null) return;
            if (!preview.transform.IsChildOf(transform)) preview = Instantiate(preview, holder, false);
            FruitLabStyle.Stretch(preview.GetButtonRectTransform());
            preview.ApplyLabPreview(icon);
        }

        private TMP_Text GetLabel(string name, float size, Color color)
        {
            TMP_Text label = transform.Find(name)?.GetComponent<TMP_Text>();
            return label != null ? label : FruitLabStyle.CreateText(name, transform, size, color);
        }

        private Image GetSurface(string name)
        {
            Image surface = transform.Find(name)?.GetComponent<Image>();
            if (surface != null) return surface;
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            item.transform.SetParent(transform, false);
            surface = item.GetComponent<Image>(); surface.raycastTarget = false;
            return surface;
        }

        private void UpdateCountLabel()
        {
            int capacity = Mathf.Max(1, ConstantValues.NUM_OF_STARS_FOR_WILD);
            if (_countLabel != null) _countLabel.text = Mathf.RoundToInt(_currentPercentage * capacity) + " / " + capacity;
        }
        private RectTransform _presentedRewardPreview;
        private Transform _presentedRewardOriginalParent;
        private int _presentedRewardOriginalSiblingIndex;
        private Vector2 _presentedRewardOriginalAnchorMin;
        private Vector2 _presentedRewardOriginalAnchorMax;
        private Vector2 _presentedRewardOriginalPivot;
        private Vector2 _presentedRewardOriginalAnchoredPosition;
        private Vector2 _presentedRewardOriginalSizeDelta;
        private Vector3 _presentedRewardOriginalLocalScale;
        private bool _presentedRewardOriginalActive;

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
            UpdateCountLabel();
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
                .OnUpdate(() => { progressImage.fillAmount = _currentPercentage; UpdateCountLabel(); })
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
            float presentationSize = Mathf.Min(76f, Mathf.Min(presentationParent.rect.width, presentationParent.rect.height));

            return DOTween.Sequence()
                .AppendCallback(() =>
                {
                    CachePresentedRewardPreview(rewardPreviewRectTransform);
                    rewardPreviewRectTransform.gameObject.SetActive(true);
                    rewardPreviewRectTransform.SetParent(presentationParent, true);
                    rewardPreviewRectTransform.anchorMin = Vector2.one * 0.5f;
                    rewardPreviewRectTransform.anchorMax = Vector2.one * 0.5f;
                    rewardPreviewRectTransform.pivot = Vector2.one * 0.5f;
                    rewardPreviewRectTransform.SetAsLastSibling();
                })
                .Append(rewardPreviewRectTransform.DOLocalMove(Vector3.zero, duration).SetEase(Ease.OutQuad))
                .Join(rewardPreviewRectTransform.DOSizeDelta(Vector2.one * presentationSize, duration).SetEase(Ease.OutQuad))
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
            _rewardType = rewardType;
            if (_rewardLabel != null) _rewardLabel.text = "Sonraki powerup: " + (rewardType == RewardType.Bomb ? "Bomba" : rewardType == RewardType.Life ? "Can" : "Göster");
            SetPowerUpButtonStatus(revealingPowerUpButton, rewardType == RewardType.Revealing);
            SetPowerUpButtonStatus(lifePowerUpButton, rewardType == RewardType.Life);
            SetPowerUpButtonStatus(bombPowerUpButton, rewardType == RewardType.Bomb);
        }

        public RectTransform GetRewardPreviewRectTransform(RewardType rewardType)
        {
            IBaseButtonView buttonView = GetPowerUpButton(rewardType);
            return buttonView?.GetButtonRectTransform();
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
            rewardPreviewRectTransform.gameObject.SetActive(_presentedRewardOriginalActive);
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
            if (_presentationTarget != null) return _presentationTarget;
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
            _presentedRewardOriginalActive = rewardPreviewRectTransform.gameObject.activeSelf;
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

using System;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

namespace Scripts
{
    public class LifeBarView : MonoBehaviour, ILifeBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private BoundaryView boundaryPrefab;
        [SerializeField] private SlicedFilledImage image;
        [SerializeField] private CanvasGroup canvasGroup;
        
        private Tween _progressTween;
        private Sequence _gainFeedback;
        private TMP_Text _gainLabel;
        private Image _gainGlow;
        private float _currentPercentage;
        private int _maxGuessCount;
        private TMP_Text _guessLabel;
        private Image _track;
        private Image _trackFrame;
        private Image _guessBackground;

        public void ApplyLabLayout(RectTransform header, float width, float headerHeight, float bottomOffset = 0f)
        {
            FruitLabStyle.Place(rectTransform, header, 0f, bottomOffset, width, 34f);
            Image oldFrame = GetComponent<Image>();
            if (oldFrame != null) oldFrame.enabled = false;
            _trackFrame = GetSurface(_trackFrame, "TrackFrame");
            _track = GetSurface(_track, "Track");
            _guessBackground = GetSurface(_guessBackground, "GuessCountBackground");
            FruitLabStyle.Surface(_trackFrame, FruitLabStyle.Border);
            FruitLabStyle.Surface(_track, FruitLabStyle.Recess);
            FruitLabStyle.Surface(_guessBackground, FruitLabStyle.Panel);
            FruitLabStyle.Place(_trackFrame.rectTransform, transform, 0f, 4f, width - 94f, 16f);
            FruitLabStyle.Place(_track.rectTransform, transform, 1f, 5f, width - 96f, 14f);
            FruitLabStyle.Place(image.rectTransform, transform, 3f, 7f, width - 100f, 10f);
            // Keep the frame and recess behind the fill and its reward markers.
            _track.transform.SetAsFirstSibling();
            _trackFrame.transform.SetAsFirstSibling();
            image.sprite = FruitLabStyle.Rounded;
            image.pixelsPerUnitMultiplier = 1.5f;
            image.color = FruitLabStyle.Mint;
            image.raycastTarget = false;
            if (_guessLabel == null) _guessLabel = transform.Find("GuessesRemaining")?.GetComponent<TMP_Text>();
            if (_guessLabel == null) _guessLabel = FruitLabStyle.CreateText("GuessesRemaining", transform, 16f, FruitLabStyle.Text);
            FruitLabStyle.TextStyle(_guessLabel, 16f);
            _guessLabel.fontStyle = FontStyles.Bold;
            _guessLabel.color = FruitLabStyle.Text;
            FruitLabStyle.Place(_guessBackground.rectTransform, transform, width - 90f, 0f, 90f, 28f);
            FruitLabStyle.Place(_guessLabel.rectTransform, transform, width - 90f, 0f, 90f, 28f);
            _guessLabel.transform.SetAsLastSibling();
            UpdateGuessLabel();
        }

        private Image GetSurface(Image existing, string name)
        {
            if (existing == null) existing = transform.Find(name)?.GetComponent<Image>();
            if (existing == null)
            {
                GameObject surface = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                surface.transform.SetParent(transform, false);
                existing = surface.GetComponent<Image>();
            }
            existing.raycastTarget = false;
            return existing;
        }

        public void SetGuessCounts(int remaining, int maximum)
        {
            _maxGuessCount = maximum;
            if (_guessLabel != null) _guessLabel.text = remaining + "/" + maximum + " hak";
        }

        private void UpdateGuessLabel()
        {
            if (_guessLabel != null && _maxGuessCount > 0)
                _guessLabel.text = Mathf.RoundToInt(_currentPercentage * _maxGuessCount) + "/" + _maxGuessCount + " hak";
        }
        
        public CanvasGroup GetCanvasGroup() => canvasGroup;

        public void DisableProgressBar()
        {
            gameObject.SetActive(false);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public RectTransform GetFilledImageRectTransform()
        {
            return image.rectTransform;
        }

        public IBoundaryView CreateBoundaryView()
        {
            return Instantiate(boundaryPrefab, image.rectTransform);
        }

        public RectTransform GetBoundaryRectTransform()
        {
            return boundaryPrefab.GetRectTransform();
        }

        public void InitProgress(float targetPercentage)
        {
            _gainFeedback?.Kill(); _progressTween?.Kill();
            _currentPercentage = targetPercentage;
            image.fillAmount = _currentPercentage;
            UpdateGuessLabel();
        }

        public Tween SetProgress(float targetPercentage, float duration, Action onComplete)
        {
            _progressTween?.Kill();
            return _progressTween = DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => { image.fillAmount = _currentPercentage; UpdateGuessLabel(); })
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                    onComplete?.Invoke();
                }).Pause();
        }
        public Sequence AnimateGain(float targetPercentage, int amount)
        {
            _gainFeedback?.Kill();
            if (_gainLabel == null) _gainLabel = FruitLabStyle.CreateText("LifeGain", transform, 20f, FruitLabStyle.Mint);
            _gainLabel.fontStyle = FontStyles.Bold;
            _gainLabel.text = "+" + amount;
            _gainLabel.raycastTarget = false;
            FruitLabStyle.Place(_gainLabel.rectTransform, transform, rectTransform.rect.width - 90f, 0f, 90f, 28f);
            _gainGlow = FruitLabStyle.Detail(transform, "LifeGainGlow", new Color(.55f, 1f, .86f, 0f), true);
            _gainGlow.sprite = FruitLabStyle.ResultFrame;
            FruitLabStyle.Place(_gainGlow.rectTransform, transform, 0f, 3f, rectTransform.rect.width - 81f, 18f);
            _gainGlow.transform.SetAsLastSibling(); _gainLabel.transform.SetAsLastSibling();
            _gainLabel.alpha = 1f; _guessLabel.alpha = 0f;
            _gainFeedback = DOTween.Sequence()
                .Append(SetProgress(targetPercentage, .58f, null))
                .Join(image.DOColor(new Color(.86f, 1f, .94f), .14f).SetLoops(2, LoopType.Yoyo))
                .Join(rectTransform.DOPunchScale(new Vector3(.025f, .10f, 0f), .55f, 5, .6f))
                .Insert(0f, _gainGlow.DOFade(.85f, .12f))
                .Insert(.12f, _gainGlow.DOFade(0f, .48f))
                .Insert(.30f, _gainLabel.DOFade(0f, .26f))
                .Insert(.30f, _guessLabel.DOFade(1f, .26f))
                .OnKill(RestoreGainAppearance)
                .OnComplete(RestoreGainAppearance)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            return _gainFeedback;
        }

        private void RestoreGainAppearance()
        {
            if (this == null) return;
            if (_gainLabel != null) _gainLabel.alpha = 0f;
            if (_gainGlow != null) { var tint = _gainGlow.color; tint.a = 0; _gainGlow.color = tint; }
            if (_guessLabel != null) _guessLabel.alpha = 1f;
            if (image != null) image.color = FruitLabStyle.Mint;
            if (rectTransform != null) rectTransform.localScale = Vector3.one;
        }
        private void OnDestroy() { _gainFeedback?.Kill(); _progressTween?.Kill(); }

    }

    public interface ILifeBarView
    {
        RectTransform GetRectTransform();
        RectTransform GetFilledImageRectTransform();
        IBoundaryView CreateBoundaryView();
        RectTransform GetBoundaryRectTransform();
        Tween SetProgress(float targetPercentage, float duration, Action onComplete);
        Sequence AnimateGain(float targetPercentage, int amount);
        void DisableProgressBar();
        void InitProgress(float targetPercentage);
        CanvasGroup GetCanvasGroup();
        void SetGuessCounts(int remaining, int maximum);
    }
}

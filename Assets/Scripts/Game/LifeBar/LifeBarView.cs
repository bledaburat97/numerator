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
        
        private float _currentPercentage;
        private int _maxGuessCount;
        private TMP_Text _guessLabel;
        private Image _track;
        private Image _trackFrame;
        private Image _guessBackground;

        public void ApplyLabLayout(RectTransform header, float width, float headerHeight)
        {
            FruitLabStyle.Place(rectTransform, header, 0f, 0f, width, 34f);
            Image oldFrame = GetComponent<Image>();
            if (oldFrame != null) oldFrame.enabled = false;
            _trackFrame = GetSurface(_trackFrame, "TrackFrame");
            _track = GetSurface(_track, "Track");
            _guessBackground = GetSurface(_guessBackground, "GuessCountBackground");
            FruitLabStyle.Surface(_trackFrame, FruitLabStyle.Border);
            FruitLabStyle.Surface(_track, FruitLabStyle.Recess);
            FruitLabStyle.Surface(_guessBackground, FruitLabStyle.Panel);
            FruitLabStyle.Place(_trackFrame.rectTransform, transform, 0f, 4f, width - 82f, 16f);
            FruitLabStyle.Place(_track.rectTransform, transform, 1f, 5f, width - 84f, 14f);
            FruitLabStyle.Place(image.rectTransform, transform, 3f, 7f, width - 88f, 10f);
            // Keep the frame and recess behind the fill and its reward markers.
            _track.transform.SetAsFirstSibling();
            _trackFrame.transform.SetAsFirstSibling();
            image.sprite = FruitLabStyle.Rounded;
            image.pixelsPerUnitMultiplier = 1.5f;
            image.color = FruitLabStyle.Mint;
            image.raycastTarget = false;
            if (_guessLabel == null) _guessLabel = transform.Find("GuessesRemaining")?.GetComponent<TMP_Text>();
            if (_guessLabel == null) _guessLabel = FruitLabStyle.CreateText("GuessesRemaining", transform, 12f, FruitLabStyle.Text);
            _guessLabel.fontSize = 12f;
            _guessLabel.fontStyle = FontStyles.Bold;
            _guessLabel.color = FruitLabStyle.Text;
            FruitLabStyle.Place(_guessBackground.rectTransform, transform, width - 74f, 0f, 74f, 26f);
            FruitLabStyle.Place(_guessLabel.rectTransform, transform, width - 74f, 0f, 74f, 26f);
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
            _currentPercentage = targetPercentage;
            image.fillAmount = _currentPercentage;
            UpdateGuessLabel();
        }

        public Tween SetProgress(float targetPercentage, float duration, Action onComplete)
        {
            return DOTween.To(() => _currentPercentage, x => _currentPercentage = x, targetPercentage, duration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(() => { image.fillAmount = _currentPercentage; UpdateGuessLabel(); })
                .OnComplete(() =>
                {
                    _currentPercentage = targetPercentage;
                    onComplete?.Invoke();
                }).Pause();
        }
    }

    public interface ILifeBarView
    {
        RectTransform GetRectTransform();
        RectTransform GetFilledImageRectTransform();
        IBoundaryView CreateBoundaryView();
        RectTransform GetBoundaryRectTransform();
        Tween SetProgress(float targetPercentage, float duration, Action onComplete);
        void DisableProgressBar();
        void InitProgress(float targetPercentage);
        CanvasGroup GetCanvasGroup();
        void SetGuessCounts(int remaining, int maximum);
    }
}

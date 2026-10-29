using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseCardItemView : MonoBehaviour, IBaseCardItemView
    {
        [SerializeField] protected RectTransform rectTransform;
        [SerializeField] protected Image image;
        [SerializeField] private Image fruitImage;
        [SerializeField] private FruitColorConfig[] fruits;
        private Image _cardFace, _cardHighlight;
        private Image _resultOutline, _resultGlow;
        private Color _resultColor;
        private float _alpha = 1f;

        public virtual void Init(int cardNumber)
        {
            fruitImage.sprite = GetFruitSprite(cardNumber);
            Color faceColor = fruits[cardNumber - 1].fruitColor;
            // The darker base is visible below the raised, softly lit face.
            FruitLabStyle.Surface(image, new Color(faceColor.r * .52f, faceColor.g * .52f, faceColor.b * .52f, 1f));
            _cardFace = FruitLabStyle.Detail(image.transform, "CardFace", faceColor);
            _cardFace.rectTransform.anchorMin = new Vector2(.025f, .09f);
            _cardFace.rectTransform.anchorMax = new Vector2(.975f, .985f);
            _cardFace.rectTransform.offsetMin = _cardFace.rectTransform.offsetMax = Vector2.zero;
            _cardHighlight = FruitLabStyle.Detail(_cardFace.transform, "CardHighlight", new Color(1f, 1f, 1f, .28f), true);
            FruitLabStyle.Stretch(_cardHighlight.rectTransform, .5f);
            fruitImage.preserveAspect = true;
            fruitImage.rectTransform.anchorMin = new Vector2(.10f, .14f);
            fruitImage.rectTransform.anchorMax = new Vector2(.90f, .94f);
            fruitImage.rectTransform.offsetMin = fruitImage.rectTransform.offsetMax = Vector2.zero;
            SetResultOutline(CardResultState.Unrevealed);
        }

        public virtual void SetLocalPosition(Vector3 localPosition)
        {
            rectTransform.localPosition = localPosition;
        }

        public void InitLocalScale()
        {
            rectTransform.localScale = Vector3.one;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        public void SetAlpha(float alpha)
        {
            _alpha = alpha;
            Color imageColor = image.color;
            imageColor.a = alpha;
            image.color = imageColor;
            if (_cardFace != null)
            {
                Color faceColor = _cardFace.color;
                faceColor.a = alpha;
                _cardFace.color = faceColor;
            }
            if (_cardHighlight != null) _cardHighlight.color = new Color(1f, 1f, 1f, .28f * alpha);

            Color fruitColor = fruitImage.color;
            fruitColor.a = alpha;
            fruitImage.color = fruitColor;
            UpdateResultAlpha();
        }

        public void SetResultOutline(CardResultState state)
        {
            bool visible = state == CardResultState.Correct || state == CardResultState.Misplaced;
            if (visible)
            {
                _resultColor = state == CardResultState.Correct ? FruitLabStyle.Correct : FruitLabStyle.Misplaced;
                _resultGlow = FruitLabStyle.Detail(transform, "ResultGlow", _resultColor, true);
                _resultGlow.pixelsPerUnitMultiplier = .45f;
                FruitLabStyle.Stretch(_resultGlow.rectTransform);
                _resultGlow.transform.SetAsLastSibling();
                _resultOutline = FruitLabStyle.Detail(transform, "ResultOutline", _resultColor, true);
                _resultOutline.pixelsPerUnitMultiplier = .9f;
                FruitLabStyle.Stretch(_resultOutline.rectTransform, .7f);
                _resultOutline.transform.SetAsLastSibling();
                UpdateResultAlpha();
            }
            if (_resultGlow != null) _resultGlow.gameObject.SetActive(visible);
            if (_resultOutline != null) _resultOutline.gameObject.SetActive(visible);
        }

        private void UpdateResultAlpha()
        {
            if (_resultOutline != null) _resultOutline.color = new Color(_resultColor.r, _resultColor.g, _resultColor.b, _alpha);
            if (_resultGlow != null) _resultGlow.color = new Color(_resultColor.r, _resultColor.g, _resultColor.b, .32f * _alpha);
        }

        public Sprite GetFruitSprite(int number) => fruits[number - 1].fruitImage;
    }

    public interface IBaseCardItemView
    {
        void Init(int cardNumber);
        void InitLocalScale();
        void SetSize(Vector2 size);
        void SetLocalPosition(Vector3 localPosition);
        void SetAlpha(float alpha);
        void SetResultOutline(CardResultState state);
    }

    public enum CardResultState { Unrevealed, Correct, Misplaced, Absent }
}

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

        public virtual void Init(int cardNumber)
        {
            SetCardNumberText(cardNumber);
            image.color = fruits[cardNumber - 1].fruitColor;
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
            Color imageColor = image.color;
            imageColor.a = alpha;
            image.color = imageColor;

            Color fruitColor = fruitImage.color;
            fruitColor.a = alpha;
            fruitImage.color = fruitColor;
        }

        private void SetCardNumberText(int number)
        {
            fruitImage.sprite = fruits[number - 1].fruitImage;
        }
    }

    public interface IBaseCardItemView
    {
        void Init(int cardNumber);
        void InitLocalScale();
        void SetSize(Vector2 size);
        void SetLocalPosition(Vector3 localPosition);
        void SetAlpha(float alpha);
    }
}
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseCardItemView : MonoBehaviour, IBaseCardItemView 
    {
        [SerializeField] protected RectTransform rectTransform;
        [SerializeField] protected TMP_Text cardNumberText;
        [SerializeField] protected Image image;
        
        public virtual void Init(int cardNumber)
        {
            SetCardNumberText(cardNumber);
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
        
        private void SetCardNumberText(int number)
        {
            cardNumberText.text = number.ToString();
        }
    }

    public interface IBaseCardItemView
    {
        void Init(int cardNumber);
        void InitLocalScale();
        void SetSize(Vector2 size);
        void SetLocalPosition(Vector3 localPosition);
    }
}
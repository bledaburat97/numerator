using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseCardItemView : MonoBehaviour, IBaseCardItemView 
    {
        [SerializeField] protected RectTransform rectTransform;
        [SerializeField] protected TMP_Text cardNumberText;
        [SerializeField] protected Image outerBGImage;
        [SerializeField] protected Image innerBGImage;
        [SerializeField] protected Image shadowImage;
        [SerializeField] protected Image cardFrame;
        
        public virtual void Init(int cardNumber)
        {
            SetCardNumberText(cardNumber);
            SetFrameStatus(false);
        }

        public virtual void InitPosition()
        {
            rectTransform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public virtual void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
        
        private void SetCardNumberText(int number)
        {
            cardNumberText.text = number.ToString();
        }

        public virtual void SetFrameStatus(bool status)
        {
            cardFrame.gameObject.SetActive(status);
        }
    }

    public interface IBaseCardItemView
    {
        void Init(int cardNumber);
        void InitPosition();
        void SetSize(Vector2 size);
        void SetFrameStatus(bool status);
    }
}
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
        [SerializeField] protected Image outerBGImage;
        [SerializeField] protected Image innerBGImage;
        [SerializeField] protected Image shadowImage;
        [SerializeField] protected Image cardFrame;
        
        public virtual void Init(int cardNumber)
        {
            SetCardNumberText(cardNumber);
            SetFrameStatus(false);
        }

        public virtual Sequence SetLocalPosition(Vector3 localPosition, float duration)
        {
            return DOTween.Sequence().Append(rectTransform.DOLocalMove(localPosition, duration)).SetEase(Ease.OutQuad);
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

        public void SetFrameStatus(bool status)
        {
            cardFrame.gameObject.SetActive(status);
        }
    }

    public interface IBaseCardItemView
    {
        void Init(int cardNumber);
        void InitLocalScale();
        void SetSize(Vector2 size);
        void SetFrameStatus(bool status);
        Sequence SetLocalPosition(Vector3 localPosition, float duration);
    }
}
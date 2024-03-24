﻿using DG.Tweening;
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
        
        public void Init(int cardNumber)
        {
            SetCardNumberText(cardNumber);
            SetFrameStatus(false);
        }
        
        public virtual void SetLocalPosition(Vector3 localPosition)
        {
            rectTransform.localPosition = localPosition;
        }

        public virtual Sequence ChangeLocalPosition(Vector3 localPosition, float duration)
        {
            return DOTween.Sequence().Append(rectTransform.DOLocalMove(localPosition, duration)).SetEase(Ease.OutQuad);
        }

        public virtual Sequence ChangePosition(Vector3 position, float duration)
        {
            return DOTween.Sequence().Append(rectTransform.DOMove(position, duration)).SetEase(Ease.OutQuad);
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
        void SetLocalPosition(Vector3 localPosition);
        Sequence ChangeLocalPosition(Vector3 localPosition, float duration);
        Sequence ChangePosition(Vector3 position, float duration);
    }
}
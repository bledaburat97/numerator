using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CardInfoButtonView : MonoBehaviour, ICardInfoButtonView
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform crossIcon;
        [SerializeField] private RectTransform colorIcon;
        [SerializeField] private RectTransform pointerImage;
        [SerializeField] private RectTransform rectTransform;

        public void Init(Action onClick)
        {
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void SetCardInfoToggleStatus(bool status, float animationDuration)
        {
            colorIcon.gameObject.SetActive(status);
            crossIcon.gameObject.SetActive(!status);
            SetPointerImagePosition(new Vector3(status ? 15 : -15, 0, 0), animationDuration);
        }
        
        private void SetPointerImagePosition(Vector3 localPos, float duration)
        {
            DOTween.Sequence().Append(pointerImage.DOLocalMove(localPos, duration)).SetEase(Ease.InQuad);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void SetActive(bool status)
        {
            rectTransform.gameObject.SetActive(status);
        }
    }

    public interface ICardInfoButtonView
    {
        void Init(Action onClick);
        void SetCardInfoToggleStatus(bool status, float animationDuration);
        RectTransform GetRectTransform();
        void SetActive(bool status);
    }
}
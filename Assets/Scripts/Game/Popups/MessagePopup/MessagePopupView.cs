using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class MessagePopupView : MonoBehaviour , IMessagePopupView
    {
        [SerializeField] private TMP_Text header;
        public void Init(string text, float initialAlpha, Vector2 localPosition)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = localPosition;
            header.text = text;
            header.alpha = initialAlpha;
        }

        public void SetColor(Color color)
        {
            header.color = color;
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        public void Animate(float showDuration, Action onClose)
        {
            onClose += Close;
            DOTween.Sequence()
                .Append(header.DOFade(1f, 0.5f))
                .AppendInterval(showDuration)
                .Append(header.DOFade(0f, 0.5f))
                .OnComplete(onClose.Invoke);
        }
    }

    public interface IMessagePopupView
    {
        void Init(string text, float initialAlpha, Vector2 localPosition);
        void Close();
        void Animate(float showDuration, Action onClose);
        void SetColor(Color color);
    }
}
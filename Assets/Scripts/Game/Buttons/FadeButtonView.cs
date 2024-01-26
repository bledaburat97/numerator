using System;
using UnityEngine;

namespace Scripts
{
    public class FadeButtonView : MonoBehaviour, IFadeButtonView
    {
        [SerializeField] private BaseButtonView baseButton;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Init(Action onClick)
        {
            baseButton.Init(onClick);
        }
        
        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }

        public CanvasGroup GetCanvasGroup()
        {
            return canvasGroup;
        }
        
        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetText(string text)
        {
            baseButton.SetText(text);
        }

        public void SetLocalPosition(Vector2 localPos)
        {
            baseButton.SetLocalPosition(localPos);
        }
    }

    public interface IFadeButtonView
    {
        void Init(Action onClick);
        void SetAlpha(float alpha);
        CanvasGroup GetCanvasGroup();
        void Destroy();
        void SetText(string text);
        void SetLocalPosition(Vector2 localPos);
    }
}
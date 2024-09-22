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
            baseButton.Init();
            baseButton.SetOnPointerDownCallBack(onClick);
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

        public void SetButtonStatus(bool status)
        {
            baseButton.SetButtonStatus(status);
        }
    }

    public interface IFadeButtonView
    {
        void Init(Action onClick);
        void SetAlpha(float alpha);
        CanvasGroup GetCanvasGroup();
        void Destroy();
        void SetText(string text);
        void SetButtonStatus(bool status);
    }
}
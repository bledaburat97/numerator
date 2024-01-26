using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class FadeButtonController : IFadeButtonController
    {
        [Inject] private IHapticController _hapticController;
        private IFadeButtonView _view;

        public FadeButtonController(IFadeButtonView view)
        {
            _view = view;
        }
        
        public void Initialize(Action onClick)
        {
            onClick += () => _hapticController.Vibrate(HapticType.ButtonClick);
            _view.Init(onClick);
        }
        
        public void SetAlpha(float alpha)
        {
            _view.SetAlpha(alpha);
        }

        public CanvasGroup GetCanvasGroup()
        {
            return _view.GetCanvasGroup();
        }
        
        public void Destroy()
        {
            _view.Destroy();
        }

        public void SetText(string text)
        {
            _view.SetText(text);
        }

        public void SetLocalPosition(Vector2 localPos)
        {
            _view.SetLocalPosition(localPos);
        }
    }

    public interface IFadeButtonController
    {
        void Initialize(Action onClick);
        void SetAlpha(float alpha);
        CanvasGroup GetCanvasGroup();
        void Destroy();
        void SetText(string text);
        void SetLocalPosition(Vector2 localPos);
    }
    
    public class FadeButtonControllerFactory : PlaceholderFactory<IFadeButtonView, IFadeButtonController>
    {
            
    }
}
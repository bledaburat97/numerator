using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BaseButtonController : IBaseButtonController
    {
        [Inject] private IHapticController _hapticController;
        private IBaseButtonView _view;

        public BaseButtonController(IBaseButtonView view)
        {
            _view = view;
        }

        public void Initialize(Action onClick)
        {
            onClick += () => _hapticController.Vibrate(HapticType.ButtonClick);
            _view.Init(onClick);
        }

        public void SetText(string text)
        {
            _view.SetText(text);
        }

        public void SetLocalPosition(Vector2 localPos)
        {
            _view.SetLocalPosition(localPos);
        }

        public void SetImageStatus(bool status)
        {
            _view.SetImageStatus(status);
        }

        public void SetTextStatus(bool status)
        {
            _view.SetTextStatus(status);
        }
        
        public void SetButtonStatus(bool status)
        {
            _view.SetButtonStatus(status);
        }

        public void SetButtonEnable(bool status)
        {
            _view.SetButtonEnable(status);
        }

        public void SetColor(Color color)
        {
            _view.SetColorOfImage(color);
        }

        public IBaseButtonView GetView()
        {
            return _view;
        }
    }

    public interface IBaseButtonController
    {
        void Initialize(Action onClick);
        void SetText(string text);
        void SetLocalPosition(Vector2 localPos);
        void SetImageStatus(bool status);
        void SetTextStatus(bool status);
        void SetButtonStatus(bool status);
        void SetButtonEnable(bool status);
        void SetColor(Color color);
        IBaseButtonView GetView();
    }
    
    public class BaseButtonControllerFactory : PlaceholderFactory<IBaseButtonView, IBaseButtonController>
    {
            
    }
}
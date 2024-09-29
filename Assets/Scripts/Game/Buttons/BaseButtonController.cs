using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class BaseButtonController : IBaseButtonController
    {
        [Inject] private IHapticController _hapticController;
        private IBaseButtonView _view;
        private bool _isButtonClickable;
        private Action _onClick;
        public BaseButtonController(IBaseButtonView view, Action onClick)
        {
            _view = view;
            _view.Init();
            _onClick = onClick;
            _isButtonClickable = true;
            _view.SetOnPointerDownCallBack(OnPointerDown);
            _view.SetOnPointerUpCallBack(OnPointerUp);
        }

        public void AddAction(Action action)
        {
            _onClick += action;
        }

        public void SetButtonClickable(bool isClickable)
        {
            _isButtonClickable = isClickable;
        }

        private void OnPointerDown()
        {
            if (_isButtonClickable)
            {
                _hapticController.Vibrate(HapticType.ButtonClick);
                _onClick?.Invoke();
                _view.SetButtonDown();
            }
        }
        
        private void OnPointerUp()
        {
            if (_isButtonClickable)
            {
                _view.SetButtonUp();
            }
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

        public void SetColor(Color color)
        {
            _view.SetColorOfImage(color);
        }

        public IBaseButtonView GetView()
        {
            return _view;
        }

        public void DestroyObject()
        {
            _view.DestroyObject();
            _view = null;
        }
    }

    public interface IBaseButtonController
    {
        void SetText(string text);
        void SetLocalPosition(Vector2 localPos);
        void SetImageStatus(bool status);
        void SetTextStatus(bool status);
        void SetButtonStatus(bool status);
        void SetColor(Color color);
        IBaseButtonView GetView();
        void SetButtonClickable(bool isClickable);
        void DestroyObject();
        void AddAction(Action action);
    }
    
    public class BaseButtonControllerFactory : PlaceholderFactory<IBaseButtonView, Action, IBaseButtonController>
    {
            
    }
}
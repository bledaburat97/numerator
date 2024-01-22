using System;
using UnityEngine;

namespace Scripts
{
    public class OptionButtonController : IOptionButtonController
    {
        private IOptionButtonView _view;
        private OptionButtonModel _model;
        public void Initialize(IOptionButtonView view, OptionButtonModel model)
        {
            _view = view;
            _model = model;
            _view.Init(_model);
        }

        public void SetPointStatus(bool status)
        {
            _view.SetPointStatus(status);
        }

        public void SetColor(Color color)
        {
            _view.SetColorOfImage(color);
        }
    }
    
    public interface IOptionButtonController
    {
        void Initialize(IOptionButtonView view, OptionButtonModel model);
        void SetPointStatus(bool status);
        void SetColor(Color color);
    }
    
    public class OptionButtonModel
    {
        public int optionIndex;
        public Action onClickAction;
    }
}
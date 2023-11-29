using System;

namespace Scripts
{
    public class CloseButtonController : ICloseButtonController
    {
        private IBaseButtonView _view;
        
        public void Initialize(IBaseButtonView view, BaseButtonModel model)
        {
            _view = view;
            _view.Init(model);
        }
    }

    public interface ICloseButtonController
    {
        void Initialize(IBaseButtonView view, BaseButtonModel model);
    }
}
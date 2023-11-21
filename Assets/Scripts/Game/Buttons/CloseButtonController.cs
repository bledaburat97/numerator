using System;

namespace Scripts
{
    public class CloseButtonController : ICloseButtonController
    {
        private ICloseButtonView _view;
        
        public void Initialize(ICloseButtonView view, CloseButtonModel model)
        {
            _view = view;
            _view.Init(model);
        }
    }

    public interface ICloseButtonController
    {
        void Initialize(ICloseButtonView view, CloseButtonModel model);
    }

    public class CloseButtonModel
    {
        public Action OnClick;
    }
}
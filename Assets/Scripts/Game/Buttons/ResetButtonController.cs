using System;

namespace Scripts
{
    public class ResetButtonController : IResetButtonController
    {
        private IBaseButtonView _view;
        public event EventHandler ResetNumbers;
        
        public void Initialize(IBaseButtonView view)
        {
            _view = view;
            _view.Init(new BaseButtonModel(){OnClick = OnClickResetButton, text = "C"});
        }

        private void OnClickResetButton()
        {
            ResetNumbers?.Invoke(this,  null);
        }
    }

    public interface IResetButtonController
    {
        void Initialize(IBaseButtonView view);
        event EventHandler ResetNumbers;
    }
}
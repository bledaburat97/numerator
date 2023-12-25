using System;

namespace Scripts
{
    public class ResetButtonController : IResetButtonController
    {
        private IBaseButtonView _view;
        public event EventHandler ResetNumbers;

        public ResetButtonController(IBaseButtonView view)
        {
            _view = view;
        }
        
        public void Initialize()
        {
            _view.Init(new BaseButtonModel(){OnClick = OnClickResetButton, text = "C"});
        }

        private void OnClickResetButton()
        {
            ResetNumbers?.Invoke(this,  null);
        }
    }

    public interface IResetButtonController
    {
        void Initialize();
        event EventHandler ResetNumbers;
    }
}
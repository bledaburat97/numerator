using System;

namespace Scripts
{
    public class ResetButtonController : IResetButtonController
    {
        private IResetButtonView _view;
        public event EventHandler ResetNumbers;
        
        public void Initialize(IResetButtonView view)
        {
            _view = view;
            _view.Init(new ResetButtonModel(){OnClick = OnClickResetButton});
        }

        private void OnClickResetButton()
        {
            ResetNumbers?.Invoke(this,  null);
        }
    }

    public interface IResetButtonController
    {
        void Initialize(IResetButtonView view);
        event EventHandler ResetNumbers;
    }
}
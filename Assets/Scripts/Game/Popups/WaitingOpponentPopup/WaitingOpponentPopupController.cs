using System;

namespace Scripts
{
    public class WaitingOpponentPopupController : IWaitingOpponentPopupController
    {
        private IWaitingOpponentPopupView _view;
        private Action _closeWaitingOpponentPopup;
        public void Initialize(IWaitingOpponentPopupView view, Action closeWaitingOpponentPopup)
        {
            _view = view;
            _closeWaitingOpponentPopup = closeWaitingOpponentPopup;
            BaseButtonModel closeButtonModel = new BaseButtonModel()
            {
                OnClick = OnCloseButtonClick
            };
            _view.Init(closeButtonModel);
        }

        private void OnCloseButtonClick()
        {
            _closeWaitingOpponentPopup.Invoke();
            _view.Close();
        }
    }

    public interface IWaitingOpponentPopupController
    {
        void Initialize(IWaitingOpponentPopupView view, Action closeWaitingOpponentPopup);
    }
}
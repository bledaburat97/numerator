using System;

namespace Scripts
{
    public class WaitingOpponentPopupController : IWaitingOpponentPopupController
    {
        private IWaitingOpponentPopupView _view;
        private Action _closeWaitingOpponentPopup;
        public void Initialize(IWaitingOpponentPopupView view, Action closeWaitingOpponentPopup, BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _view = view;
            _closeWaitingOpponentPopup = closeWaitingOpponentPopup;
            _view.Init();
            IBaseButtonController closeButtonController = baseButtonControllerFactory.Create(_view.GetCloseButton());
            closeButtonController.Initialize(OnCloseButtonClick);
        }

        private void OnCloseButtonClick()
        {
            _closeWaitingOpponentPopup.Invoke();
            _view.Close();
        }
    }

    public interface IWaitingOpponentPopupController
    {
        void Initialize(IWaitingOpponentPopupView view, Action closeWaitingOpponentPopup, BaseButtonControllerFactory baseButtonControllerFactory);
    }
}
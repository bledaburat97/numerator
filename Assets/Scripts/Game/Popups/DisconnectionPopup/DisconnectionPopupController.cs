namespace Scripts
{
    public class DisconnectionPopupController : IDisconnectionPopupController
    {
        private IDisconnectionPopupView _view;
        
        public void Initialize(IDisconnectionPopupView view)
        {
            _view = view;
            _view.Init();
            CreateReturnMenuButton();
        }
        
        private void CreateReturnMenuButton()
        {
            IReturnMenuButtonController returnMenuButtonController = new ReturnMenuButtonController();
            returnMenuButtonController.Initialize(_view.GetReturnMenuButtonView(), null);
        }
    }

    public interface IDisconnectionPopupController
    {
        void Initialize(IDisconnectionPopupView view);
    }
}
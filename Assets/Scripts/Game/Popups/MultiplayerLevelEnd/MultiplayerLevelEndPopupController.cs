namespace Scripts
{
    public class MultiplayerLevelEndPopupController : IMultiplayerLevelEndPopupController
    {
        private IMultiplayerLevelEndPopupView _view;
        public void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess)
        {
            _view = view;
            _view.Init(isSuccess);
        }
    }

    public interface IMultiplayerLevelEndPopupController
    {
        void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess);
    }
}
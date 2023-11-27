using System;

namespace Scripts
{
    public class SettingsPopupController : ISettingsPopupController
    {
        private ISettingsPopupView _view;

        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction)
        {
            _view = view;
            _view.Init();
            CreateCloseButton(onCloseAction);
            CreatePlayButton();
            CreateReturnMenuButton(saveGameAction);
        }
        
        private void CreateCloseButton(Action onCloseAction)
        {
            ICloseButtonController closeButtonController = new CloseButtonController();
            closeButtonController.Initialize(_view.GetCloseButtonView(), new CloseButtonModel()
            {
                OnClick = () =>
                {
                    _view.Close();
                    onCloseAction.Invoke();
                }
            });
        }
        
        private void CreatePlayButton()
        {
            IPlayButtonController playButtonController = new PlayButtonController();
            playButtonController.Initialize(_view.GetPlayButtonView(), "RETRY");
        }

        private void CreateReturnMenuButton(Action saveGameAction)
        {
            IReturnMenuButtonController returnMenuButtonController = new ReturnMenuButtonController();
            returnMenuButtonController.Initialize(_view.GetReturnMenuButtonView(), saveGameAction);
        }
    }

    public interface ISettingsPopupController
    {
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction);
    }
}
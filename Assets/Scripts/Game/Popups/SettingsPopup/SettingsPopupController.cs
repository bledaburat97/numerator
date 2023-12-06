using System;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class SettingsPopupController : ISettingsPopupController
    {
        private ISettingsPopupView _view;

        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction)
        {
            _view = view;
            _view.Init();
            CreateCloseButton(onCloseAction);
            CreatePlayButton(deleteSaveAction);
            CreateReturnMenuButton(saveGameAction);
        }
        
        private void CreateCloseButton(Action onCloseAction)
        {
            ICloseButtonController closeButtonController = new CloseButtonController();
            closeButtonController.Initialize(_view.GetCloseButtonView(), new BaseButtonModel()
            {
                OnClick = () =>
                {
                    _view.Close();
                    onCloseAction.Invoke();
                }
            });
        }
        
        private void CreatePlayButton(Action deleteSaveAction)
        {
            IPlayButtonController playButtonController = new PlayButtonController();
            Action onClickAction = deleteSaveAction;
            onClickAction += () => SceneManager.LoadScene("Game");
            playButtonController.Initialize(_view.GetPlayButtonView(), new BaseButtonModel()
            {
                text = "Retry",
                OnClick = onClickAction
            });
        }

        private void CreateReturnMenuButton(Action saveGameAction)
        {
            IReturnMenuButtonController returnMenuButtonController = new ReturnMenuButtonController();
            returnMenuButtonController.Initialize(_view.GetReturnMenuButtonView(), saveGameAction);
        }
    }

    public interface ISettingsPopupController
    {
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction);
    }
}
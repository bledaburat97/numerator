using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class SettingsPopupController : ISettingsPopupController
    {
        private ISettingsPopupView _view;
        private ILevelTracker _levelTracker;

        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker)
        {
            _view = view;
            _levelTracker = levelTracker;
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
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                _view.DestroyRetryButton();
                return;
            }
            IPlayButtonController playButtonController = new PlayButtonController();
            Action onClickAction = deleteSaveAction;
            onClickAction += () => NetworkManager.Singleton.StartHost();
            onClickAction += () => NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
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
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker);
    }
}
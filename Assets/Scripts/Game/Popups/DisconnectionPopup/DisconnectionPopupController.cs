using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class DisconnectionPopupController : IDisconnectionPopupController
    {
        private IDisconnectionPopupView _view;
        
        public void Initialize(IDisconnectionPopupView view, BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _view = view;
            _view.Init();
            IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButtonView(), OnMenuButtonClick);
            menuButtonController.SetText("MENU");
        }

        public void SetText(string text)
        {
            _view.SetText(text);
        }
        
        private void OnMenuButtonClick()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
            SceneManager.LoadScene("Menu");
        }
    }

    public interface IDisconnectionPopupController
    {
        void Initialize(IDisconnectionPopupView view, BaseButtonControllerFactory baseButtonControllerFactory);
        void SetText(string text);
    }
}
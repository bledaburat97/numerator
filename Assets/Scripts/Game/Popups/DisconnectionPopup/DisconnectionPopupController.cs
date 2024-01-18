using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class DisconnectionPopupController : IDisconnectionPopupController
    {
        private IDisconnectionPopupView _view;
        
        public void Initialize(IDisconnectionPopupView view)
        {
            _view = view;
            BaseButtonModel menuButtonModel = new BaseButtonModel()
            {
                text = "MENU",
                OnClick = OnMenuButtonClick
            };
            _view.Init(menuButtonModel);
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
        void Initialize(IDisconnectionPopupView view);
    }
}
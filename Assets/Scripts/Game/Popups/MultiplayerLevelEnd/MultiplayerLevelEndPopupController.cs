using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MultiplayerLevelEndPopupController : IMultiplayerLevelEndPopupController
    {
        private IMultiplayerLevelEndPopupView _view;
        private IUserReady _userReady;
        public void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess, IUserReady userReady)
        {
            _view = view;
            _userReady = userReady;
            BaseButtonModel playAgainButtonModel = new BaseButtonModel()
            {
                text = "Play Again",
                OnClick = OnPlayAgainButtonClick
            };
            BaseButtonModel menuButtonModel = new BaseButtonModel()
            {
                text = "Menu",
                OnClick = OnMenuButtonClick
            };
            _view.Init(isSuccess, playAgainButtonModel, menuButtonModel);
            
        }
        
        private void OnPlayAgainButtonClick()
        {
            _userReady.SetPlayerReady();
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

    public interface IMultiplayerLevelEndPopupController
    {
        void Initialize(IMultiplayerLevelEndPopupView view, bool isSuccess, IUserReady userReady);
    }
}
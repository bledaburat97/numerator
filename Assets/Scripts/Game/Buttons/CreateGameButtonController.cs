using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class CreateGameButtonController : ICreateGameButtonController
    {
        private IPlayButtonView _view;

        public CreateGameButtonController(IPlayButtonView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            BaseButtonModel model = new BaseButtonModel()
            {
                text = "Create Game",
                OnClick = OnPlayButtonClick
            };
            _view.Init(model);
        }

        private void OnPlayButtonClick()
        {
            MultiplayerManager.Instance.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("WaitingScene", LoadSceneMode.Single);
        }
    }

    public interface ICreateGameButtonController
    {
        void Initialize();
    }
}
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MultiPlayerButtonController : IMultiPlayerButtonController
    {
        private IPlayButtonView _view;
        
        public MultiPlayerButtonController(IPlayButtonView view)
        {
            _view = view;
        }

        public void Initialize(IGameOptionTracker gameOptionTracker)
        {
            BaseButtonModel model = new BaseButtonModel()
            {
                text = "Multiplayer",
                OnClick = () => OnPlayButtonClick(gameOptionTracker)
            };
            _view.Init(model);
        }
        
        private void OnPlayButtonClick(IGameOptionTracker gameOptionTracker)
        {
            gameOptionTracker.SetGameOption(GameOption.MultiPlayer);
            SceneManager.LoadScene("Game");
        }
    }

    public interface IMultiPlayerButtonController
    {
        void Initialize(IGameOptionTracker gameOptionTracker);
    }
}
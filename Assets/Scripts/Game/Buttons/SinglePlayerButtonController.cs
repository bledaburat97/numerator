using UnityEngine.SceneManagement;

namespace Scripts
{
    public class SinglePlayerButtonController : ISinglePlayerButtonController
    {
        private IPlayButtonView _view;
        
        public SinglePlayerButtonController(IPlayButtonView view)
        {
            _view = view;
        }

        public void Initialize(IGameOptionTracker gameOptionTracker)
        {
            BaseButtonModel model = new BaseButtonModel()
            {
                text = "Single Player",
                OnClick = () => OnPlayButtonClick(gameOptionTracker)
            };
            _view.Init(model);
        }
        
        private void OnPlayButtonClick(IGameOptionTracker gameOptionTracker)
        {
            gameOptionTracker.SetGameOption(GameOption.SinglePlayer);
            SceneManager.LoadScene("Menu");
        }
    }

    public interface ISinglePlayerButtonController
    {
        void Initialize(IGameOptionTracker gameOptionTracker);
    }

    public enum GameOption
    {
        SinglePlayer = 0,
        MultiPlayer = 1
    }
}
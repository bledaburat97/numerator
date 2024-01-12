using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MultiPlayerButtonController : IMultiPlayerButtonController
    {
        private IPlayButtonView _view;
        private ILevelTracker _levelTracker;
        
        public MultiPlayerButtonController(IPlayButtonView view)
        {
            _view = view;
        }

        public void Initialize(ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            BaseButtonModel model = new BaseButtonModel()
            {
                text = "Multiplayer",
                OnClick = OnPlayButtonClick
            };
            _view.Init(model);
        }
        
        private void OnPlayButtonClick()
        {
            _levelTracker.SetGameOption(GameOption.MultiPlayer);
            SceneManager.LoadScene("Lobby");
        }
    }

    public interface IMultiPlayerButtonController
    {
        void Initialize(ILevelTracker gameOptionTracker);
    }
}
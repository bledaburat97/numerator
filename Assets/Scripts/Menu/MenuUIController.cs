using Scripts;
using UnityEngine.SceneManagement;

namespace Menu
{
    public class MenuUIController : IMenuUIController
    {
        private IMenuUIView _view;
        private ILevelTracker _levelTracker;
        
        public MenuUIController(IMenuUIView view)
        {
            _view = view;
        }

        public void Initialize(IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            activeLevelIdController.LevelSelectionChanged += OnLevelChange;
            _view.SetSinglePlayerButton(new BaseButtonModel()
            {
                text = GetLevelText(activeLevelIdController.GetActiveLevelId(), activeLevelIdController.IsNewGame()),
                OnClick = OnSinglePlayerButtonClick
            });
            
            _view.SetMultiplayerButton(new BaseButtonModel()
            {
                text = "MULTIPLAYER",
                OnClick = OnMultiplayerButtonClick
            });
        }

        private void OnSinglePlayerButtonClick()
        {
            _levelTracker.SetGameOption(GameOption.SinglePlayer);
            SceneManager.LoadScene("Lobby");
        }
        
        private void OnMultiplayerButtonClick()
        {
            _levelTracker.SetGameOption(GameOption.MultiPlayer);
            SceneManager.LoadScene("Lobby");
        }

        private void OnLevelChange(object sender, ActiveLevelChangedEventArgs args)
        {
            _view.SetSinglePlayerButtonText(GetLevelText(args.activeLevelId, args.isNewGame));
        }

        private string GetLevelText(int activeLevelId, bool isNewLevel)
        {
            if (!isNewLevel)
            {
                return "CONTINUE";
            }

            return "LEVEL " + (activeLevelId + 1);
        }
    }

    public interface IMenuUIController
    {
        void Initialize(IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker);
    }
}
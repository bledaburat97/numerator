using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Menu
{
    public class MenuUIController : IMenuUIController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        private IMenuUIView _view;
        private ILevelTracker _levelTracker;
        private IBaseButtonController _singlePlayerButtonController;
        public MenuUIController(IMenuUIView view)
        {
            _view = view;
        }

        public void Initialize(IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
            activeLevelIdController.LevelSelectionChanged += OnLevelChange;
            
            _singlePlayerButtonController = _baseButtonControllerFactory.Create(_view.GetSinglePlayerButton());
            _singlePlayerButtonController.Initialize(OnSinglePlayerButtonClick);
            _singlePlayerButtonController.SetText(GetLevelText(activeLevelIdController.GetActiveLevelId(), activeLevelIdController.IsNewGame()));

            IBaseButtonController multiplayerButtonController =
                _baseButtonControllerFactory.Create(_view.GetMultiplayerButton());
            multiplayerButtonController.Initialize(OnMultiplayerButtonClick);
            multiplayerButtonController.SetText("MULTIPLAYER");
        }

        private void OnSinglePlayerButtonClick()
        {
            _levelTracker.SetGameOption(GameOption.SinglePlayer);
            SceneManager.LoadScene("Game");
        }
        
        private void OnMultiplayerButtonClick()
        {
            _levelTracker.SetGameOption(GameOption.MultiPlayer);
            SceneManager.LoadScene("Lobby");
        }

        private void OnLevelChange(object sender, ActiveLevelChangedEventArgs args)
        {
            _singlePlayerButtonController.SetText(GetLevelText(args.activeLevelId, args.isNewGame));
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
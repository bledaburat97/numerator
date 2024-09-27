using Scripts;
using UnityEngine.SceneManagement;
using Zenject;

namespace Menu
{
    public class MenuUIController : IMenuUIController
    {
        private BaseButtonControllerFactory _baseButtonControllerFactory;
        private ILevelTracker _levelTracker;
        private IGameSaveService _gameSaveService;
        private IMenuUIView _view;
        private IBaseButtonController _singlePlayerButtonController;
        
        [Inject]
        public MenuUIController(ILevelTracker levelTracker, IGameSaveService gameSaveService, BaseButtonControllerFactory baseButtonControllerFactory, IMenuUIView view)
        {
            _levelTracker = levelTracker;
            _baseButtonControllerFactory = baseButtonControllerFactory;
            _view = view;
            _gameSaveService = gameSaveService;
            CreateButtons();
        }

        private void CreateButtons()
        {
            _singlePlayerButtonController = _baseButtonControllerFactory.Create(_view.GetSinglePlayerButton(), OnSinglePlayerButtonClick);
            _singlePlayerButtonController.SetText(GetLevelText());

            IBaseButtonController multiplayerButtonController =
                _baseButtonControllerFactory.Create(_view.GetMultiplayerButton(), OnMultiplayerButtonClick);
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

        private string GetLevelText()
        {
            if (_gameSaveService.HasSavedGame())
            {
                return "Continue";
            }

            return "Level " + (_levelTracker.GetLevelId() + 1);
        }
    }

    public interface IMenuUIController
    {
    }
}
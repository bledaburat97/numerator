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
        private bool _navigating;
        
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
            _singlePlayerButtonController.SetClickOnRelease(true);
            if(_view.GetSinglePlayerButton() is BaseButtonView button)
                ButtonAppearance.For(button).ApplyLevelIdentity(LevelDefinitionPreview.Resolve(_levelTracker.GetLevelId(),_gameSaveService));

        }

        private void OnSinglePlayerButtonClick()
        {
            if (_navigating) return;
            _navigating = true;
            _levelTracker.SetGameOption(GameOption.SinglePlayer);
            SceneManager.LoadScene("Game");
        }
        
        private string GetLevelText()
        {
            if (_gameSaveService.HasSavedGame())
            {
                return "Bölüm " + (_levelTracker.GetLevelId()+1) + " · Devam";
            }

            return "Bölüm " + (_levelTracker.GetLevelId() + 1);
        }
    }

    public interface IMenuUIController
    {
    }
}

using System;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class MenuPlayButtonController : IMenuPlayButtonController
    {
        private IBaseButtonView _view;
        private ILevelTracker _levelTracker;
        private IGameSaveService _gameSaveService;
        
        public void Initialize(IBaseButtonView view, IActiveLevelIdController activeLevelIdController)
        {
            _view = view;
            activeLevelIdController.LevelSelectionChanged += OnTextChange;
            BaseButtonModel model = new BaseButtonModel()
            {
                text = GetText(activeLevelIdController.GetActiveLevelId(), activeLevelIdController.IsNewGame()),
                OnClick = () => OnPlayButtonClick()
            };
            _view.Init(model);
        }

        private void OnPlayButtonClick()
        {
            SceneManager.LoadScene("Game");
        }

        private void OnTextChange(object sender, ActiveLevelChangedEventArgs args)
        {
            _view.SetText(GetText(args.activeLevelId, args.isNewGame));
        }

        private string GetText(int activeLevelId, bool isNewLevel)
        {
            if (!isNewLevel)
            {
                return "Continue";
            }

            return "Level " + (activeLevelId + 1);
        }
    }

    public interface IMenuPlayButtonController
    {
        void Initialize(IBaseButtonView view, IActiveLevelIdController activeLevelIdController);
    }
}
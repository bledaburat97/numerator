using System.Collections.Generic;

namespace Scripts
{
    public class LobbyCreationPopupController : ILobbyCreationPopupController
    {
        private ILobbyCreationPopupView _view;
        private Dictionary<int, IOptionButtonController> _difficultyButtonControllers;
        private ILevelTracker _levelTracker;
        
        public void Initialize(ILobbyCreationPopupView view, ILevelTracker levelTracker)
        {
            _view = view;
            _levelTracker = levelTracker;
            _view.Init();
            CreateDifficultyButtons();
        }
        
        private void CreateDifficultyButtons()
        {
            _difficultyButtonControllers = new Dictionary<int, IOptionButtonController>();
            OptionButtonControllerFactory difficultyButtonControllerFactory = new OptionButtonControllerFactory();
            for (int i = 0; i < ConstantValues.NUM_OF_DIFFICULTY_BUTTONS; i++)
            {
                int difficultyIndex = i;
                OptionButtonModel difficultyButtonModel = new OptionButtonModel()
                {
                    optionIndex = difficultyIndex,
                    onClickAction = () => OnDifficultyButtonClicked(difficultyIndex)
                };
                IOptionButtonController difficultyButtonController = difficultyButtonControllerFactory.Spawn();
                IOptionButtonView difficultyButtonView = _view.GetDifficultyButtonViewByIndex(difficultyIndex);
                difficultyButtonController.Initialize(difficultyButtonView, difficultyButtonModel);
                difficultyButtonController.SetPointStatus(difficultyIndex == (int)Difficulty.Hard);
                _difficultyButtonControllers.Add(difficultyButtonModel.optionIndex, difficultyButtonController);
            }
            _levelTracker.SetMultiplayerLevelDifficulty(Difficulty.Hard);
        }

        private void OnDifficultyButtonClicked(int difficultyIndex)
        {
            for (int i = 0; i < _difficultyButtonControllers.Count; i++)
            {
                _difficultyButtonControllers[i].SetPointStatus(i == difficultyIndex);
            }
            _levelTracker.SetMultiplayerLevelDifficulty((Difficulty)difficultyIndex);
        }

        public void ShowPopup()
        {
            _view.Show();
        }
    }

    public interface ILobbyCreationPopupController
    {
        void Initialize(ILobbyCreationPopupView view, ILevelTracker levelTracker);
        void ShowPopup();
    }
}
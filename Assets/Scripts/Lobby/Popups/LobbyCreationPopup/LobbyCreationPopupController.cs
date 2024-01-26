﻿using System.Collections.Generic;

namespace Scripts
{
    public class LobbyCreationPopupController : ILobbyCreationPopupController
    {
        private ILobbyCreationPopupView _view;
        private Dictionary<int, IBaseButtonController> _difficultyButtonControllers;
        private ILevelTracker _levelTracker;
        private BaseButtonControllerFactory _baseButtonControllerFactory;
        
        public void Initialize(ILobbyCreationPopupView view, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory)
        {
            _view = view;
            _levelTracker = levelTracker;
            _view.Init();
            _baseButtonControllerFactory = baseButtonControllerFactory;
            IBaseButtonController closeButtonController = _baseButtonControllerFactory.Create(_view.GetCloseButton());
            closeButtonController.Initialize(_view.Hide);

            IBaseButtonController publicButtonController = _baseButtonControllerFactory.Create(_view.GetPublicButton());
            publicButtonController.Initialize(() => PlayerLobby.Instance.CreateLobby(_view.GetLobbyName(), false));
            publicButtonController.SetText("PUBLIC");

            IBaseButtonController privateButtonController =
                _baseButtonControllerFactory.Create(_view.GetPrivateButton());
            privateButtonController.Initialize(() => PlayerLobby.Instance.CreateLobby(_view.GetLobbyName(), true));
            privateButtonController.SetText("PRIVATE");
            
            CreateDifficultyButtons();
        }
        
        private void CreateDifficultyButtons()
        {
            _difficultyButtonControllers = new Dictionary<int, IBaseButtonController>();
            for (int i = 0; i < ConstantValues.NUM_OF_DIFFICULTY_BUTTONS; i++)
            {
                int difficultyIndex = i;
                IBaseButtonView difficultyButtonView = _view.GetDifficultyButtonViewByIndex(difficultyIndex);
                IBaseButtonController difficultyButtonController = _baseButtonControllerFactory.Create(difficultyButtonView);
                difficultyButtonController.Initialize(() => OnDifficultyButtonClicked(difficultyIndex));
                difficultyButtonController.SetImageStatus(difficultyIndex == (int)Difficulty.Hard);
                _difficultyButtonControllers.Add(difficultyIndex, difficultyButtonController);
            }
        }

        private void OnDifficultyButtonClicked(int difficultyIndex)
        {
            for (int i = 0; i < _difficultyButtonControllers.Count; i++)
            {
                _difficultyButtonControllers[i].SetImageStatus(i == difficultyIndex);
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
        void Initialize(ILobbyCreationPopupView view, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory);
        void ShowPopup();
    }
}
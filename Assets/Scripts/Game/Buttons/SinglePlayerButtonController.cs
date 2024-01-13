using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class SinglePlayerButtonController : ISinglePlayerButtonController
    {
        private IPlayButtonView _view;
        private ILevelTracker _levelTracker;

        public SinglePlayerButtonController(IPlayButtonView view)
        {
            _view = view;
        }
        
        public void Initialize(IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker)
        {
            _levelTracker = levelTracker;
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
            _levelTracker.SetGameOption(GameOption.SinglePlayer);
            SceneManager.LoadScene("Lobby");
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

    public interface ISinglePlayerButtonController
    {
        void Initialize(IActiveLevelIdController activeLevelIdController, ILevelTracker levelTracker);
    }
}
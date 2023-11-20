namespace Scripts
{
    public class LevelEndPopupController : ILevelEndPopupController
    {
        private ILevelEndPopupView _view;
        private ILevelTracker _levelTracker;
        public void Initialize(ILevelEndPopupView view, LevelEndEventArgs args)
        {
            _view = view;
            _levelTracker = args.levelTracker;
            _view.Init();
            _view.SetTitle(args.isLevelCompleted ? "Well Done!" : "Try Again!");
            CreatePlayButton();
            CreateReturnMenuButton();
        }
        
        private void CreatePlayButton()
        {
            IPlayButtonController playButtonController = new PlayButtonController();
            playButtonController.Initialize(_view.GetPlayButtonView(), "Level " + _levelTracker.GetLevelId());
        }

        private void CreateReturnMenuButton()
        {
            IReturnMenuButtonController returnMenuButtonController = new ReturnMenuButtonController();
            returnMenuButtonController.Initialize(_view.GetReturnMenuButtonView());
        }
    }

    public interface ILevelEndPopupController
    {
        void Initialize(ILevelEndPopupView view, LevelEndEventArgs args);
    }
}
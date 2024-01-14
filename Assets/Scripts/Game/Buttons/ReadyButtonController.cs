namespace Scripts
{
    public class ReadyButtonController : IReadyButtonController
    {
        private IPlayButtonView _view;
        private IUserReady _userReady;
        public ReadyButtonController(IPlayButtonView view)
        {
            _view = view;
        }

        public void Initialize(IUserReady userReady)
        {
            _userReady = userReady;
            BaseButtonModel model = new BaseButtonModel()
            {
                text = "Ready",
                OnClick = OnReadyButtonClick
            };
            _view.Init(model);
        }

        private void OnReadyButtonClick()
        {
            _userReady.SetPlayerReady();
        }
    }

    public interface IReadyButtonController
    {
        void Initialize(IUserReady userReady);
    }
}
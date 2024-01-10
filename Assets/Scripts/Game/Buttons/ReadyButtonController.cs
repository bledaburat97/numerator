namespace Scripts
{
    public class ReadyButtonController : IReadyButtonController
    {
        private IPlayButtonView _view;

        public ReadyButtonController(IPlayButtonView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            BaseButtonModel model = new BaseButtonModel()
            {
                text = "Ready",
                OnClick = OnReadyButtonClick
            };
            _view.Init(model);
        }

        private void OnReadyButtonClick()
        {
            UserReady.Instance.SetPlayerReady();
        }
    }

    public interface IReadyButtonController
    {
        void Initialize();
    }
}
namespace Scripts
{
    public class JoinGameButtonController : IJoinGameButtonController
    {
        private IPlayButtonView _view;

        public JoinGameButtonController(IPlayButtonView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            BaseButtonModel model = new BaseButtonModel()
            {
                text = "Join Game",
                OnClick = OnPlayButtonClick
            };
            _view.Init(model);
        }

        private void OnPlayButtonClick()
        {
            MultiplayerManager.Instance.StartClient();
        }
    }

    public interface IJoinGameButtonController
    {
        void Initialize();
    }
}
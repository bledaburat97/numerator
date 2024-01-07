namespace Scripts
{
    public class PlayButtonController : IPlayButtonController
    {
        private IPlayButtonView _view;
        public void Initialize(IPlayButtonView view, BaseButtonModel model)
        {
            _view = view;
            _view.Init(model);
        }
    }

    public interface IPlayButtonController
    {
        void Initialize(IPlayButtonView view, BaseButtonModel model);
    }
}
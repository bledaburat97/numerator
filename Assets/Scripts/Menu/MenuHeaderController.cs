namespace Scripts
{
    public class MenuHeaderController : IMenuHeaderController
    {
        private IMenuHeaderView _view;

        public MenuHeaderController(IMenuHeaderView view)
        {
            _view = view;
        }

        public void Initialize(ILevelTracker levelTracker)
        {
            _view.Init(0, 0);
        }
    }

    public interface IMenuHeaderController
    {
        void Initialize(ILevelTracker levelTracker);
    }
}
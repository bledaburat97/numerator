namespace Scripts
{
    public class FadePanelController : IFadePanelController
    {
        private IFadePanelView _view;

        public void Initialize(IFadePanelView view)
        {
            _view = view;
            _view.SetFadeImageStatus(false);
        }

        public void SetFadeImageStatus(bool status)
        {
            _view.SetFadeImageStatus(status);
        }
        
    }

    public interface IFadePanelController
    {
        void Initialize(IFadePanelView view);
        void SetFadeImageStatus(bool status);
    }
}
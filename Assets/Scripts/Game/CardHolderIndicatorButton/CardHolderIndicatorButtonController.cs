namespace Scripts
{
    public class CardHolderIndicatorButtonController : ICardHolderIndicatorButtonController
    {
        private ICardHolderIndicatorButtonView _view;
        private CardHolderIndicatorButtonModel _model;
        public void Initialize(ICardHolderIndicatorButtonView view, CardHolderIndicatorButtonModel model)
        {
            _view = view;
            _model = model;
            _view.Init(_model);
        }

        public void SetStatus(bool status)
        {
            _view.SetStatus(status);
        }
    }
    
    public interface ICardHolderIndicatorButtonController
    {
        void Initialize(ICardHolderIndicatorButtonView view, CardHolderIndicatorButtonModel model);
        void SetStatus(bool status);
    }
}
using System;

namespace Scripts
{
    public class NewGameOfferPopupController : INewGameOfferPopupController
    {
        private INewGameOfferPopupView _view;
        private IGamePopupCreator _gamePopupCreator;
        public void Initialize(INewGameOfferPopupView view, IGamePopupCreator gamePopupCreator)
        {
            _view = view;
            _gamePopupCreator = gamePopupCreator;
            _view.Init();
            _gamePopupCreator.closeNewGameOfferPopup += Close;
        }

        private void Close(object sender, EventArgs args)
        {
            _view.Close();
            _gamePopupCreator.closeNewGameOfferPopup -= Close;
        }
        
    }

    public interface INewGameOfferPopupController
    {
        void Initialize(INewGameOfferPopupView view, IGamePopupCreator gamePopupCreator);
    }
}
using UnityEngine;

namespace Views
{
    public class CardHolderController : ICardHolderController
    {
        private bool _isAvailable;
        private ICardHolderView _view;
        public void Initialize(ICardHolderView cardHolderView, CardHolderModel model)
        {
            _view = cardHolderView;
            SetAvailability(true);
            _view.Init(model);
        }

        public ICardHolderView GetView()
        {
            return _view;
        }

        public bool IsAvailable()
        {
            return _isAvailable;
        }

        public void SetAvailability(bool isAvailable)
        {
            _isAvailable = isAvailable;
        }
    }
    
    public interface ICardHolderController
    {
        void Initialize(ICardHolderView cardHolderView, CardHolderModel model);
        ICardHolderView GetView();
        bool IsAvailable();
        void SetAvailability(bool isAvailable);
    }
    
    public class CardHolderModel
    {
        public Vector3 localPosition;
        public Vector2 size;
    }
}
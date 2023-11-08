using System.Dynamic;
using UnityEngine;

namespace Views
{
    public class CardHolderController : ICardHolderController
    {
        private bool _isAvailable;
        private ICardHolderView _view;
        private int _index;
        public void Initialize(ICardHolderView cardHolderView, CardHolderModel model)
        {
            _view = cardHolderView;
            SetAvailability(true);
            _index = model.index;
            _view.Init(model);
        }

        public int GetIndex()
        {
            return _index;
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
        int GetIndex();
    }
    
    public class CardHolderModel
    {
        public int index;
        public Vector3 localPosition;
        public Vector2 size;
    }
}
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
    }
    
    public interface ICardHolderController
    {
        void Initialize(ICardHolderView cardHolderView, CardHolderModel model);
        ICardHolderView GetView();
        int GetIndex();
    }
    
    public class CardHolderModel
    {
        public int index;
        public Vector3 localPosition;
        public Vector2 size;
    }
}
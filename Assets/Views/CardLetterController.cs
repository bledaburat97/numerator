using UnityEngine;

namespace Views
{
    public class CardLetterController : ICardLetterController
    {
        private ICardLetterView _view;
        
        public void Initialize(ICardLetterView cardLetterView, CardLetterModel model)
        {
            _view = cardLetterView;
            _view.Init(model.letter);
        }
        
        public void SetStatus(bool status)
        {
            _view.SetStatus(status);
        }

        public void SetParent(RectTransform parent)
        {
            _view.SetParent(parent);
        }

    }
    
    public interface ICardLetterController
    {
        void Initialize(ICardLetterView cardLetterView, CardLetterModel model);
        void SetStatus(bool status);
        void SetParent(RectTransform parent);
    }
    
    public class CardLetterModel
    {
        public string letter;
    }
}
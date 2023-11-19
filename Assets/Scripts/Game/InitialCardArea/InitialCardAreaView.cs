using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaView : MonoBehaviour, IInitialCardAreaView
    {
        [SerializeField] private CardHolderView cardHolderPrefab;
        [SerializeField] private CardItemView cardItemPrefab;
        [SerializeField] private RectTransform tempParentRectTransform;
        private CardItemViewFactory _cardItemViewFactory;
        private CardHolderFactory _cardHolderFactory;
        
        public void Init(CardHolderFactory cardHolderFactory, CardItemViewFactory cardItemViewFactory)
        {
            transform.localScale = Vector3.one;
            _cardHolderFactory = cardHolderFactory;
            _cardItemViewFactory = cardItemViewFactory;
        }
        
        public ICardHolderView CreateCardHolderView()
        {
            return _cardHolderFactory.Spawn(transform, cardHolderPrefab);
        }

        public ICardItemView CreateCardItemView(Transform parent)
        {
            return _cardItemViewFactory.Spawn(parent, cardItemPrefab);
        }

        public RectTransform GetTempRectTransform()
        {
            return tempParentRectTransform;
        }
    }
    
    public interface IInitialCardAreaView
    {
        void Init(CardHolderFactory cardHolderFactory, CardItemViewFactory cardItemViewFactory);
        ICardHolderView CreateCardHolderView();
        ICardItemView CreateCardItemView(Transform parent);
        RectTransform GetTempRectTransform();
    }
}
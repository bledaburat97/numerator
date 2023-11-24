using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaView : MonoBehaviour, IInitialCardAreaView
    {
        [SerializeField] private CardHolderView cardHolderPrefab;
        [SerializeField] private DraggableCardItemView draggableCardItemPrefab;
        [SerializeField] private RectTransform tempParentRectTransform;
        [SerializeField] private InvisibleClickHandler invisibleClickHandler;
        [SerializeField] private WildCardItemView wildCardItemPrefab;
        
        private DraggableCardItemViewFactory _draggableCardItemViewFactory;
        private CardHolderFactory _cardHolderFactory;
        private WildCardItemViewFactory _wildCardItemViewFactory;
        
        public void Init(CardHolderFactory cardHolderFactory, DraggableCardItemViewFactory draggableCardItemViewFactory, WildCardItemViewFactory wildCardItemViewFactory)
        {
            transform.localScale = Vector3.one;
            _cardHolderFactory = cardHolderFactory;
            _draggableCardItemViewFactory = draggableCardItemViewFactory;
            _wildCardItemViewFactory = wildCardItemViewFactory;
        }

        public IInvisibleClickHandler GetInvisibleClickHandler()
        {
            return invisibleClickHandler;
        }
        
        public ICardHolderView CreateCardHolderView()
        {
            return _cardHolderFactory.Spawn(transform, cardHolderPrefab);
        }

        public IDraggableCardItemView CreateCardItemView(Transform parent)
        {
            return _draggableCardItemViewFactory.Spawn(parent, draggableCardItemPrefab);
        }

        public RectTransform GetTempRectTransform()
        {
            return tempParentRectTransform;
        }

        public IWildCardItemView CreateWildCardItemView(Transform parent)
        {
            return _wildCardItemViewFactory.Spawn(parent, wildCardItemPrefab);
        }
    }
    
    public interface IInitialCardAreaView
    {
        void Init(CardHolderFactory cardHolderFactory, DraggableCardItemViewFactory draggableCardItemViewFactory, WildCardItemViewFactory wildCardItemViewFactory);
        ICardHolderView CreateCardHolderView();
        IDraggableCardItemView CreateCardItemView(Transform parent);
        IWildCardItemView CreateWildCardItemView(Transform parent);
        RectTransform GetTempRectTransform();
        IInvisibleClickHandler GetInvisibleClickHandler();
    }
}
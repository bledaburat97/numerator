using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaView : MonoBehaviour, IInitialCardAreaView
    {
        [SerializeField] private CardHolderView cardHolderPrefab;
        [SerializeField] private DraggableCardItemView _draggableCardItemPrefab;
        [SerializeField] private RectTransform tempParentRectTransform;
        [SerializeField] private InvisibleClickHandler invisibleClickHandler;
        private DraggableCardItemViewFactory _draggableCardItemViewFactory;
        private CardHolderFactory _cardHolderFactory;
        
        public void Init(CardHolderFactory cardHolderFactory, DraggableCardItemViewFactory draggableCardItemViewFactory)
        {
            transform.localScale = Vector3.one;
            _cardHolderFactory = cardHolderFactory;
            _draggableCardItemViewFactory = draggableCardItemViewFactory;
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
            return _draggableCardItemViewFactory.Spawn(parent, _draggableCardItemPrefab);
        }

        public RectTransform GetTempRectTransform()
        {
            return tempParentRectTransform;
        }
    }
    
    public interface IInitialCardAreaView
    {
        void Init(CardHolderFactory cardHolderFactory, DraggableCardItemViewFactory draggableCardItemViewFactory);
        ICardHolderView CreateCardHolderView();
        IDraggableCardItemView CreateCardItemView(Transform parent);
        RectTransform GetTempRectTransform();
        IInvisibleClickHandler GetInvisibleClickHandler();
    }
}
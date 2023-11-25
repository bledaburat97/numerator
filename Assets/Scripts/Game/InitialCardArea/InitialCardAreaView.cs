using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaView : MonoBehaviour, IInitialCardAreaView
    {
        [SerializeField] private CardHolderView cardHolderPrefab;
        [SerializeField] private NormalCardItemView _normalCardItemPrefab;
        [SerializeField] private RectTransform tempParentRectTransform;
        [SerializeField] private InvisibleClickHandler invisibleClickHandler;
        [SerializeField] private WildCardItemView wildCardItemPrefab;
        
        private NormalCardItemViewFactory _normalCardItemViewFactory;
        private CardHolderFactory _cardHolderFactory;
        private WildCardItemViewFactory _wildCardItemViewFactory;
        
        public void Init(CardHolderFactory cardHolderFactory, NormalCardItemViewFactory normalCardItemViewFactory, WildCardItemViewFactory wildCardItemViewFactory)
        {
            transform.localScale = Vector3.one;
            _cardHolderFactory = cardHolderFactory;
            _normalCardItemViewFactory = normalCardItemViewFactory;
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

        public INormalCardItemView CreateCardItemView(Transform parent)
        {
            return _normalCardItemViewFactory.Spawn(parent, _normalCardItemPrefab);
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
        void Init(CardHolderFactory cardHolderFactory, NormalCardItemViewFactory normalCardItemViewFactory, WildCardItemViewFactory wildCardItemViewFactory);
        ICardHolderView CreateCardHolderView();
        INormalCardItemView CreateCardItemView(Transform parent);
        IWildCardItemView CreateWildCardItemView(Transform parent);
        RectTransform GetTempRectTransform();
        IInvisibleClickHandler GetInvisibleClickHandler();
    }
}
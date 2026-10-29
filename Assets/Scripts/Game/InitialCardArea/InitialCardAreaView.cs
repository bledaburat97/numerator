using UnityEngine;

namespace Scripts
{
    public class InitialCardAreaView : MonoBehaviour, IInitialCardAreaView
    {
        [SerializeField] private InitialHolderView initialHolderPrefab;
        [SerializeField] private NormalCardItemView normalCardItemPrefab;
        [SerializeField] private RectTransform tempParentRectTransform;
        [SerializeField] private InvisibleClickHandler invisibleClickHandler;
        [SerializeField] private Camera cam;
        [SerializeField] private RectTransform upperHolder;
        [SerializeField] private RectTransform lowerHolder;
        [SerializeField] private CanvasGroup canvasGroup;
        
        public CanvasGroup GetCanvasGroup() => canvasGroup;
        public RectTransform GetRectTransform() => (RectTransform)transform;
        
        public Camera GetCamera()
        {
            return cam;
        }

        public IInvisibleClickHandler GetInvisibleClickHandler()
        {
            return invisibleClickHandler;
        }

        public Sprite GetFruitSprite(int number) => normalCardItemPrefab.GetFruitSprite(number);
        
        public IInitialHolderView CreateCardHolderView()
        {
            return Instantiate(initialHolderPrefab, transform);
        }

        public IInitialHolderView CreateCardHolderViewOnUpperHolder()
        {
            return Instantiate(initialHolderPrefab, upperHolder);
        }

        public IInitialHolderView CreateCardHolderViewOnLowerHolder()
        {
            return Instantiate(initialHolderPrefab, lowerHolder);
        }

        public INormalCardItemView CreateCardItemView(Transform parent)
        {
            return Instantiate(normalCardItemPrefab, parent);
        }

        public RectTransform GetTempRectTransform()
        {
            return tempParentRectTransform;
        }

        public Vector2 GetSizeOfInitialHolderPrefab()
        {
            return initialHolderPrefab.GetRectTransform().sizeDelta;
        }

        public Vector2 GetSizeOfBoxPrefab()
        {
            return normalCardItemPrefab.GetRectTransform().sizeDelta;
        }
        
        public Vector2 GetSizeOfHolderIndicatorPrefab()
        {
            return initialHolderPrefab.GetSizeOfHolderIndicatorPrefab();
        }
    }
    
    public interface IInitialCardAreaView
    {
        RectTransform GetRectTransform();
        IInitialHolderView CreateCardHolderViewOnUpperHolder();
        IInitialHolderView CreateCardHolderViewOnLowerHolder();

        INormalCardItemView CreateCardItemView(Transform parent);
        RectTransform GetTempRectTransform();
        IInvisibleClickHandler GetInvisibleClickHandler();
        Sprite GetFruitSprite(int number);
        Camera GetCamera();
        Vector2 GetSizeOfInitialHolderPrefab();
        Vector2 GetSizeOfBoxPrefab();
        Vector2 GetSizeOfHolderIndicatorPrefab();
        CanvasGroup GetCanvasGroup();
    }
}

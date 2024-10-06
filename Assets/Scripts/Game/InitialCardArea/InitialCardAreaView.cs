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
        
        public void Init()
        {
            transform.localScale = Vector3.one;
        }

        public Camera GetCamera()
        {
            return cam;
        }

        public IInvisibleClickHandler GetInvisibleClickHandler()
        {
            return invisibleClickHandler;
        }
        
        public IInitialHolderView CreateCardHolderView()
        {
            return Instantiate(initialHolderPrefab, transform);
        }

        public INormalCardItemView CreateCardItemView(Transform parent)
        {
            return Instantiate(normalCardItemPrefab, parent);
        }

        public RectTransform GetTempRectTransform()
        {
            return tempParentRectTransform;
        }
    }
    
    public interface IInitialCardAreaView
    {
        void Init();
        IInitialHolderView CreateCardHolderView();
        INormalCardItemView CreateCardItemView(Transform parent);
        RectTransform GetTempRectTransform();
        IInvisibleClickHandler GetInvisibleClickHandler();
        Camera GetCamera();
    }
}
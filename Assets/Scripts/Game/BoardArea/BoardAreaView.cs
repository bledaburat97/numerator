using UnityEngine;

namespace Scripts
{
    public class BoardAreaView : MonoBehaviour, IBoardAreaView
    {
        [SerializeField] private CardHolderView boardCardHolderPrefab;
        [SerializeField] private Camera cam;
        
        public ICardHolderView CreateCardHolderView()
        {
            return Instantiate(boardCardHolderPrefab, transform, true);
        }

        public Camera GetCamera()
        {
            return cam;
        }
    }

    public interface IBoardAreaView
    {
        ICardHolderView CreateCardHolderView();
        Camera GetCamera();
    }
}
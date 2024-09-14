using UnityEngine;

namespace Scripts
{
    public class BoardAreaView : MonoBehaviour, IBoardAreaView
    {
        [SerializeField] private CardHolderView boardCardHolderPrefab;
        [SerializeField] private Camera cam;
        [SerializeField] private Canvas canvas;

        public ICardHolderView CreateCardHolderView()
        {
            return Instantiate(boardCardHolderPrefab, transform, true);
        }

        public Camera GetCamera()
        {
            return cam;
        }

        public Canvas GetCanvas()
        {
            return canvas;
        }
    }

    public interface IBoardAreaView
    {
        ICardHolderView CreateCardHolderView();
        Camera GetCamera();
        Canvas GetCanvas();
    }
}
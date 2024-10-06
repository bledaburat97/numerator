using UnityEngine;

namespace Scripts
{
    public class BoardAreaView : MonoBehaviour, IBoardAreaView
    {
        [SerializeField] private BoardHolderView boardHolderPrefab;
        [SerializeField] private Camera cam;
        [SerializeField] private Canvas canvas;

        public IBoardHolderView CreateBoardHolderView()
        {
            return Instantiate(boardHolderPrefab, transform, true);
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
        IBoardHolderView CreateBoardHolderView();
        Camera GetCamera();
        Canvas GetCanvas();
    }
}
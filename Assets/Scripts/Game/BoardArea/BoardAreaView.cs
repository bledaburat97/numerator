using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class BoardAreaView : MonoBehaviour, IBoardAreaView
    {
        public float FooterHeight { get; private set; }
        public void SetFooterHeight(float height) => FooterHeight = Mathf.Max(0,height);
        [SerializeField] private BoardHolderView boardHolderPrefab;
        [SerializeField] private Camera cam;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image firstSplitGroup;
        [SerializeField] private Image secondSplitGroup;

        public void PrepareSplitVisuals()
        {
            if (firstSplitGroup == null) firstSplitGroup = FruitLabStyle.Detail(transform,"SplitGroupA",FruitLabStyle.Tile);
            if (secondSplitGroup == null) secondSplitGroup = FruitLabStyle.Detail(transform,"SplitGroupB",FruitLabStyle.Tile);
            StyleSplitGroup(firstSplitGroup,FruitLabStyle.SplitFirstGroup);
            StyleSplitGroup(secondSplitGroup,FruitLabStyle.SplitSecondGroup);
        }

        private static void StyleSplitGroup(Image image,Color color)
        {
            FruitLabStyle.Surface(image,Color.Lerp(FruitLabStyle.Recess,color,.12f));
            FruitLabStyle.Rim(image.rectTransform,.7f);
            image.transform.Find("LabRim").GetComponent<Image>().color = new Color(color.r,color.g,color.b,.7f);
            image.raycastTarget = false; image.transform.SetAsFirstSibling(); image.gameObject.SetActive(false);
        }

        public void ShowSplitGroups(Rect? first,Rect? second)
        {
            if (!first.HasValue || !second.HasValue)
            {
                if (firstSplitGroup != null) firstSplitGroup.gameObject.SetActive(false);
                if (secondSplitGroup != null) secondSplitGroup.gameObject.SetActive(false);
                return;
            }
            if (firstSplitGroup == null || secondSplitGroup == null) PrepareSplitVisuals();
            PlaceSplitGroup(firstSplitGroup,first.Value); PlaceSplitGroup(secondSplitGroup,second.Value);
        }

        private void PlaceSplitGroup(Image image,Rect bounds)
        {
            var area = ((RectTransform)transform).rect;
            float bottom = Mathf.Max(area.yMin+1f,bounds.yMin-2f), top = Mathf.Min(area.yMax-1f,bounds.yMax+2f);
            FruitLabStyle.Place(image.rectTransform,transform,bounds.xMin-area.xMin-2f,bottom-area.yMin,bounds.width+4f,top-bottom);
            image.transform.SetAsFirstSibling(); image.gameObject.SetActive(true);
        }
        
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

        public RectTransform GetRectTransform()
        {
            return (RectTransform)transform;
        }

        public Vector2 GetSizeOfBoardHolder()
        {
            return boardHolderPrefab.GetRectTransform().sizeDelta;
        }
    }

    public interface IBoardAreaView
    {
        IBoardHolderView CreateBoardHolderView();
        Camera GetCamera();
        Canvas GetCanvas();
        RectTransform GetRectTransform();
        Vector2 GetSizeOfBoardHolder();
    }
}

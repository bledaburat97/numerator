namespace Scripts
{
    using UnityEngine;
    using UnityEngine.UI;

    public class NonDraggableCardItemView : BaseCardItemView, INonDraggableCardItemView
    {
        public override void Init(int cardNumber)
        {
            base.Init(cardNumber);
            Image outline = transform.Find("CardBorder")?.GetComponent<Image>();
            if (outline == null)
            {
                GameObject border = new GameObject("CardBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                border.transform.SetParent(transform, false);
                outline = border.GetComponent<Image>();
            }
            FruitLabStyle.Stretch(outline.rectTransform);
            FruitLabStyle.Surface(outline, new Color(.04f, .08f, .12f, .35f), true);
            outline.raycastTarget = false;
            outline.transform.SetAsLastSibling();
        }

        public void MultiplyPixelsPerUnit()
        {
            image.pixelsPerUnitMultiplier = image.pixelsPerUnitMultiplier * 2;
        }
    }
    
    public interface INonDraggableCardItemView : IBaseCardItemView
    {
        void MultiplyPixelsPerUnit();

    }
}

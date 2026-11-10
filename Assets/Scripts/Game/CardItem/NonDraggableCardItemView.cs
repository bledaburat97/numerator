namespace Scripts
{
    using UnityEngine;
    using UnityEngine.UI;

    public class NonDraggableCardItemView : BaseCardItemView, INonDraggableCardItemView
    {
        [SerializeField] private IceCardGraphic frozenVisual;
        public void PrepareFrozenVisual() { frozenVisual=IceCardGraphic.Ensure(transform);frozenVisual.gameObject.SetActive(false); }
        public void InitFrozen()
        {
            Init(1);image.gameObject.SetActive(false);SetFruitVisible(false);
            transform.Find("CardBorder")?.gameObject.SetActive(false);
            if(frozenVisual==null)PrepareFrozenVisual();
            frozenVisual.gameObject.SetActive(true);frozenVisual.transform.SetAsLastSibling();
            SetResultOutline(CardResultState.Frozen);
        }
        public override void SetAlpha(float alpha)
        {
            base.SetAlpha(alpha);
            if(frozenVisual!=null){var tint=frozenVisual.color;tint.a=alpha;frozenVisual.color=tint;}
        }
        public override void Init(int cardNumber)
        {
            if(frozenVisual!=null)frozenVisual.gameObject.SetActive(false);
            image.gameObject.SetActive(true);SetFruitVisible(true);
            base.Init(cardNumber);
            Image outline = transform.Find("CardBorder")?.GetComponent<Image>();
            if (outline == null)
            {
                GameObject border = new GameObject("CardBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                border.transform.SetParent(transform, false);
                outline = border.GetComponent<Image>();
            }
            outline.gameObject.SetActive(true);
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
        void InitFrozen();

    }
}

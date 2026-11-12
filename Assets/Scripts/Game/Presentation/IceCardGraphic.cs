using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // The same supplied sprite is used by the live slot and the immutable history row.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IceCardGraphic : Image
    {
        public static IceCardGraphic Ensure(Transform parent)
        {
            var child = parent.Find("FrozenCard");
            var graphic = child != null ? child.GetComponent<IceCardGraphic>() : null;
            if (graphic == null)
            {
                var root = new GameObject("FrozenCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(IceCardGraphic));
                root.transform.SetParent(parent, false);
                root.layer = parent.gameObject.layer;
                graphic = root.GetComponent<IceCardGraphic>();
                FruitLabStyle.Stretch(graphic.rectTransform);
                graphic.gameObject.SetActive(false);
            }
            graphic.ApplyArtwork();
            return graphic;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ApplyArtwork();
        }

        private void ApplyArtwork()
        {
            sprite = PopupArtStyle.Ice;
            type = Type.Simple;
            preserveAspect = true;
            raycastTarget = false;
        }
    }
}

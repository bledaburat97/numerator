using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    [DefaultExecutionOrder(-20)]
    public class UnmaskCardItemView : MonoBehaviour, IUnmaskCardItemView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image image;
        private RectTransform _target;
        private float _padding;
        private readonly Vector3[] _corners = new Vector3[4];
        public void Follow(RectTransform target, float padding)
        {
            _target = target; _padding = padding;
            image.sprite = FruitLabStyle.ResultFrame; image.type = Image.Type.Sliced;
            image.color = new Color(FruitLabStyle.Mint.r, FruitLabStyle.Mint.g, FruitLabStyle.Mint.b, .8f);
            image.raycastTarget = false; Refresh();
        }
        private void LateUpdate() { if (_target != null) Refresh(); }
        private void Refresh()
        {
            var corners = _corners; _target.GetWorldCorners(corners);
            Vector2 min = transform.parent.InverseTransformPoint(corners[0]), max = min;
            foreach (Vector3 corner in corners) { Vector2 p = transform.parent.InverseTransformPoint(corner); min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            Vector2 center = (min + max) * .5f, size = max - min + Vector2.one * _padding * 2f;
            if (((Vector2)rectTransform.localPosition - center).sqrMagnitude < .001f && (rectTransform.sizeDelta - size).sqrMagnitude < .001f) return;
            rectTransform.anchorMin = rectTransform.anchorMax = Vector2.one * .5f;
            rectTransform.pivot = Vector2.one * .5f;
            rectTransform.localPosition = center;
            rectTransform.sizeDelta = size;
        }
        public void SetPosition(Vector2 position, float anchorMaxYOfSafeArea, float heightOfCanvas, float changeInLocalPosY)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.position = new Vector3(position.x, position.y, transform.parent.position.z);
            rectTransform.localPosition += Vector3.up * changeInLocalPosY;
            image.raycastTarget = false;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        public void Destroy()
        {
            TutorialObjectUtility.Destroy(gameObject);
        }

        public void SetPixelPerUnit(float value)
        {
            image.pixelsPerUnitMultiplier = value;
        }
    }

    public interface IUnmaskCardItemView
    {
        void SetPosition(Vector2 position, float anchorMaxYOfSafeArea, float heightOfCanvas, float changeInLocalPosY);
        void SetSize(Vector2 size);
        void Destroy();
        void SetPixelPerUnit(float value);
    }
}

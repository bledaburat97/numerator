using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class UnmaskServiceView : MonoBehaviour, IUnmaskServiceView
    {
        [SerializeField] private TutorialSpotlightView spotlight;
        [SerializeField] private UnmaskCardItemView unmaskCardItemPrefab;
        private readonly List<UnmaskCardItemView> _items = new List<UnmaskCardItemView>();
        public void Init(Image maskImage, Color color, float alpha)
        {
            // Real UI holes replace the old mask camera and RenderTexture.
            maskImage.enabled = false;
            var rect = (RectTransform)transform;
            rect.SetParent(maskImage.transform.parent, false); FruitLabStyle.Stretch(rect);
            foreach (Transform child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = maskImage.gameObject.layer;
            spotlight.color = new Color(color.r, color.g, color.b, alpha);
            spotlight.raycastTarget = true;
        }
        public void SetBaseAlpha(float alpha) { Color c = spotlight.color; c.a = alpha; spotlight.color = c; }
        public void SetColor(Color color) { color.a = spotlight.color.a; spotlight.color = color; }
        public Tween SetAlpha(float alpha, float duration) => spotlight.DOFade(alpha, duration).SetLink(gameObject);
        public void Highlight(RectTransform target, float padding = 5f)
        {
            if (target == null) return;
            var item = Create(); item.Follow(target, padding); RefreshHoles();
        }
        private UnmaskCardItemView Create()
        {
            var item = Instantiate(unmaskCardItemPrefab, transform, false);
            item.gameObject.layer = gameObject.layer;
            item.SetPixelPerUnit(1.2f);
            _items.Add(item); return item;
        }
        public void CreateUnmaskCardItem(Vector2 position, Vector2 size, float anchorMaxYOfSafeArea, float heightOfCanvas, float pixelPerUnit, float changeInLocalPosY)
        {
            var item = Create(); item.SetPosition(position, 1f, 0f, changeInLocalPosY); item.SetSize(size); RefreshHoles();
        }
        private void RefreshHoles() => spotlight.SetHoles(_items.ConvertAll(item => (RectTransform)item.transform));
        private void LateUpdate()
        {
            foreach (var item in _items)
                if (item != null && item.transform.hasChanged) { item.transform.hasChanged = false; spotlight.SetVerticesDirty(); }
        }
        public void ClearAllUnmaskCardItems()
        {
            foreach (var item in _items) if (item != null) item.Destroy();
            _items.Clear(); RefreshHoles();
        }
        public void ClearUnmaskCardItem(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            _items[index].Destroy(); _items.RemoveAt(index); RefreshHoles();
        }
        private void OnDestroy() { if (spotlight != null) spotlight.DOKill(); }
    }
    public interface IUnmaskServiceView
    {
        void Init(Image maskImage, Color color, float alpha);
        void SetBaseAlpha(float alpha);
        Tween SetAlpha(float alpha, float duration);
        void SetColor(Color color);
        void CreateUnmaskCardItem(Vector2 position, Vector2 size, float anchorMaxYOfSafeArea, float heightOfCanvas, float pixelPerUnit, float changeInLocalPosY);
        void ClearAllUnmaskCardItems();
        void ClearUnmaskCardItem(int index);
    }
}

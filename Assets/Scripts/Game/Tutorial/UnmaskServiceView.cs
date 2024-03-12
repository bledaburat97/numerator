using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class UnmaskServiceView : MonoBehaviour, IUnmaskServiceView
    {
        [NonSerialized] private Image maskImage;
        [SerializeField] private Camera cam;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Shader shader;
        [SerializeField] private UnmaskCardItemView unmaskCardItemPrefab;
        [SerializeField] private RectTransform scaleHelper;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int BaseAlpha = Shader.PropertyToID("_BaseAlpha");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private Material _material;
        private RenderTexture _texture;
        private List<IUnmaskCardItemView> _unmaskCardItemViews = new List<IUnmaskCardItemView>();
        
        public void Init(Image maskImage, Color color, float alpha)
        {
            this.maskImage = maskImage;
            _material = new Material(shader);
            _texture = new RenderTexture(Screen.width, Screen.height, 0);
            cam.targetTexture = _texture;
            canvas.gameObject.SetActive(true);
            cam.gameObject.SetActive(true);

            _material.SetTexture(MainTex, _texture);
            maskImage.material = _material;

            SetColor(color);
            SetBaseAlpha(alpha);
        }

        public void SetBaseAlpha(float alpha)
        {
            _material.SetFloat(BaseAlpha, alpha);
        }

        public Tween SetAlpha(float alpha, float duration)
        {
            maskImage.DOFade(alpha, 0f);
            _material.SetFloat(BaseAlpha, 0f);
            return _material.DOFloat(alpha, BaseAlpha, duration).SetEase(Ease.OutSine);
        }

        public void SetColor(Color color)
        {
            _material.SetColor(BaseColor, color);
        }

        public void CreateUnmaskCardItem(Vector2 position, Vector2 size, float pixelPerUnit)
        {
            UnmaskCardItemViewFactory unmaskCardItemViewFactory = new UnmaskCardItemViewFactory();
            IUnmaskCardItemView unmaskCardItemView = unmaskCardItemViewFactory.Spawn(scaleHelper, unmaskCardItemPrefab);
            unmaskCardItemView.SetPosition(position);
            unmaskCardItemView.SetSize(size);
            unmaskCardItemView.SetPixelPerUnit(pixelPerUnit);
            _unmaskCardItemViews.Add(unmaskCardItemView);
        }

        public void ClearAllUnmaskCardItems()
        {
            foreach (var unmaskCardItemView in _unmaskCardItemViews)
            {
                unmaskCardItemView.Destroy();
            }
            _unmaskCardItemViews.Clear();
        }
        
        public void ClearUnmaskCardItem(int index)
        {
            _unmaskCardItemViews[index].Destroy();
            _unmaskCardItemViews.RemoveAt(index);
        }

        public void ChangeLocalPositionOfUnmaskCardItem(Vector2 changeInLocalPos)
        {
            foreach (var unmaskCardItemView in _unmaskCardItemViews)
            {
                unmaskCardItemView.ChangeLocalPosition(changeInLocalPos);
            }
        }
    }

    public interface IUnmaskServiceView
    {
        void Init(Image maskImage, Color color, float alpha);
        void SetBaseAlpha(float alpha);
        Tween SetAlpha(float alpha, float duration);
        void SetColor(Color color);
        void CreateUnmaskCardItem(Vector2 position, Vector2 size, float pixelPerUnit);
        void ClearAllUnmaskCardItems();
        void ClearUnmaskCardItem(int index);
        void ChangeLocalPositionOfUnmaskCardItem(Vector2 changeInLocalPos);
    }
}
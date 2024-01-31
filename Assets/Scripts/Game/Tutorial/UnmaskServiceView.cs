using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class UnmaskServiceView : MonoBehaviour, IUnmaskServiceView
    {
        public RectTransform ScaleHelper;

        [NonSerialized] private Image maskImage;
        [SerializeField] private Camera cam;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Shader shader;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int BaseAlpha = Shader.PropertyToID("_BaseAlpha");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private Material _material;
        private RenderTexture _texture;
        private Transform _holder;
        public Camera Cam => cam;
        
        public void Init(Image maskImage, Color color, float alpha)
        {
            this.maskImage = maskImage;
            _material = new Material(shader);
            _holder = canvas.transform;
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
    }

    public interface IUnmaskServiceView
    {
        void Init(Image maskImage, Color color, float alpha);
        void SetBaseAlpha(float alpha);
        Tween SetAlpha(float alpha, float duration);
        void SetColor(Color color);
    }
}
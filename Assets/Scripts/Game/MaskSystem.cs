using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class MaskSystem : MonoBehaviour, IMaskSystem
    {
        [NonSerialized] private Image _maskImage;
        [SerializeField] private Camera cam;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Shader shader;
        
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int BaseAlpha = Shader.PropertyToID("_BaseAlpha");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        
        private Material _material;
        private RenderTexture _texture;
        private Transform _holder;
        
        public void Init(Image maskImage)
        {
            _maskImage = maskImage;
            _material = new Material(shader);
            _holder = canvas.transform;
            _texture = new RenderTexture(Screen.width, Screen.height, 0);
            //cam.targetTexture = _texture;
            canvas.gameObject.SetActive(true);
            cam.gameObject.SetActive(true);

            _material.SetTexture(MainTex, _texture);
            //_maskImage.material = _material;

            SetColor(Color.black);
            SetBaseAlpha(0.6f);
        }
        
        private void SetBaseAlpha(float alpha)
        {
            _material.SetFloat(BaseAlpha, alpha);
        }

        private void SetColor(Color color)
        {
            _material.SetColor(BaseColor, color);
        }
    }

    public interface IMaskSystem
    {
        void Init(Image maskImage);
    }
}
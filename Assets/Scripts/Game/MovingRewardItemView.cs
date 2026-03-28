using DG.Tweening;
using Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class MovingRewardItemView : MonoBehaviour
    {
        private static readonly Color CoreHighlightColor = new Color(0.92f, 0.98f, 1f, 0.95f);
        private static Sprite _fallbackSprite;

        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Sprite glowSprite;
        [SerializeField] private float midGlowScale = 1.35f;
        [SerializeField] private float coreGlowScale = 0.58f;

        private Image _image;
        private CanvasGroup _canvasGroup;
        private Image _midGlowImage;
        private Image _coreGlowImage;

        private void Awake()
        {
            CacheComponents();
        }
        
        public void Init(RectTransform parentRect)
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            rectTransform.SetParent(parentRect, false);
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
            CacheComponents();
            SetColor(ConstantValues.BLUE_STAR_COLOR);
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
            UpdateGlowLayerSizes();
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }

        public RectTransform GetRectTransform()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            return rectTransform;
        }

        public void SnapToCenter()
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }

        public void SetColor(Color color)
        {
            CacheComponents();
            EnsureGlowLayers();
            _image.color = Color.white;

            _midGlowImage.color = new Color(
                Mathf.Lerp(color.r, 1f, 0.08f),
                Mathf.Lerp(color.g, 1f, 0.08f),
                Mathf.Lerp(color.b, 1f, 0.08f),
                0.9f);
            _coreGlowImage.color = CoreHighlightColor;
        }

        public Sequence AnimateSpawn(float duration = 0.18f)
        {
            CacheComponents();
            SetStatus(true);
            rectTransform.localScale = Vector3.zero;
            _canvasGroup.alpha = 0f;

            return DOTween.Sequence()
                .Append(rectTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack))
                .Join(_canvasGroup.DOFade(1f, duration * 0.75f));
        }

        public Sequence AnimateDissolve(float duration = 0.24f, float targetScaleMultiplier = 1.2f)
        {
            CacheComponents();
            SetStatus(true);
            _canvasGroup.alpha = 1f;

            return DOTween.Sequence()
                .Append(_canvasGroup.DOFade(0f, duration))
                .Join(rectTransform.DOScale(Vector3.one * targetScaleMultiplier, duration).SetEase(Ease.OutQuad));
        }

        public void DestroyObject()
        {
            Destroy(gameObject);
        }

        private void CacheComponents()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (_image == null)
            {
                _image = GetComponent<Image>();
                if (_image == null)
                {
                    _image = CreateVisualLayer("OrbBody", 1f);
                }
            }
            
            if (_image != null)
            {
                _image.raycastTarget = false;
                _image.preserveAspect = true;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            EnsureGlowLayers();
        }

        private void EnsureGlowLayers()
        {
            if (_image == null) return;

            if (_midGlowImage == null)
            {
                _midGlowImage = CreateGlowLayer("MidGlow", midGlowScale);
            }

            if (_coreGlowImage == null)
            {
                _coreGlowImage = CreateGlowLayer("CoreGlow", coreGlowScale);
            }

            _midGlowImage.rectTransform.SetSiblingIndex(0);
            _image.rectTransform.SetSiblingIndex(1);
            _coreGlowImage.rectTransform.SetSiblingIndex(2);

            UpdateGlowLayerSizes();
        }

        private Image CreateGlowLayer(string objectName, float scale)
        {
            Image layerImage = CreateVisualLayer(objectName, scale);
            layerImage.sprite = glowSprite != null ? glowSprite : _image.sprite;
            return layerImage;
        }

        private Image CreateVisualLayer(string objectName, float scale)
        {
            GameObject layerObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform layerRectTransform = layerObject.GetComponent<RectTransform>();
            layerRectTransform.SetParent(rectTransform, false);
            layerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            layerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            layerRectTransform.pivot = new Vector2(0.5f, 0.5f);
            layerRectTransform.anchoredPosition = Vector2.zero;
            layerRectTransform.localScale = Vector3.one * scale;
            layerRectTransform.SetSiblingIndex(0);

            Image layerImage = layerObject.GetComponent<Image>();
            layerImage.sprite = GetDefaultSprite();
            layerImage.raycastTarget = false;
            layerImage.preserveAspect = true;

            return layerImage;
        }

        private void UpdateGlowLayerSizes()
        {
            if (_image != null && _image.rectTransform != rectTransform)
            {
                _image.rectTransform.sizeDelta = rectTransform.sizeDelta;
                _image.rectTransform.localScale = Vector3.one;
            }

            UpdateGlowLayerSize(_midGlowImage, midGlowScale);
            UpdateGlowLayerSize(_coreGlowImage, coreGlowScale);
        }

        private void UpdateGlowLayerSize(Image glowImage, float scale)
        {
            if (glowImage == null) return;

            RectTransform glowRectTransform = glowImage.rectTransform;
            glowRectTransform.sizeDelta = rectTransform.sizeDelta;
            glowRectTransform.localScale = Vector3.one * scale;
        }

        private Sprite GetDefaultSprite()
        {
            if (glowSprite != null) return glowSprite;

            if (_fallbackSprite == null)
            {
                _fallbackSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f));
            }

            return _fallbackSprite;
        }
    }
}

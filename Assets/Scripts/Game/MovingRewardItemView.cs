using DG.Tweening;
using Scripts;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game
{
    public class MovingRewardItemView : MonoBehaviour
    {
        private static Sprite _fallbackSprite;

        [SerializeField] private RectTransform rectTransform;
        [FormerlySerializedAs("glowSprite")]
        [SerializeField] private Sprite rewardSprite;

        private Image _image;
        private CanvasGroup _canvasGroup;

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
            _image.color = color;
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
                    _image = gameObject.AddComponent<Image>();
                }
            }

            if (_image.sprite == null)
            {
                _image.sprite = rewardSprite != null ? rewardSprite : GetDefaultSprite();
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
        }

        private Sprite GetDefaultSprite()
        {
            if (rewardSprite != null) return rewardSprite;

            if (_fallbackSprite == null)
            {
                _fallbackSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f));
            }

            return _fallbackSprite;
        }
    }
}

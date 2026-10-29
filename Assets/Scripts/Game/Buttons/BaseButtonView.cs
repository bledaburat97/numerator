using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseButtonView : MonoBehaviour, IBaseButtonView, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image outerBg;
        [SerializeField] private Image shadow;
        [SerializeField] private Image innerBg;
        [SerializeField] protected Image image;
        [SerializeField] private bool useLabActionStyle;
        [SerializeField] private bool primaryLabAction;
        private Canvas tutorialCanvas;
        private GraphicRaycaster tutorialRaycaster;
        private Vector2 innerBgOffsetMin;
        private Vector2 innerBgOffsetMax;
        private Action _onPointerDown;
        private Action _onPointerUp;
        private FruitLabGlyph _labGlyph;

        private void ResolveReferences()
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            if (image == null)
            {
                Transform icon = transform.Find("CheckImage") ?? transform.Find("Image");
                if (icon != null) image = icon.GetComponent<Image>();
            }
        }

        public void Init()
        {
            ResolveReferences();
            transform.localScale = Vector3.one;
            ApplyLabStyle(gameObject.name.StartsWith("CheckButton"));
            if (innerBg == null) return;

            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
        }

        public void ApplyLabStyle(bool primary = false)
        {
            ResolveReferences();
            // Powerup prefabs used Picture as their old pressed background.
            // Their presentation assigns the separate Bg surface instead.
            if (this is PowerUpButtonView) return;
            if (useLabActionStyle) primary = primaryLabAction;
            FruitLabStyle.Surface(outerBg, primary ? FruitLabStyle.Mint * 0.7f : FruitLabStyle.Border);
            FruitLabStyle.Surface(innerBg, primary ? FruitLabStyle.Mint : FruitLabStyle.Tile);
            FruitLabStyle.Surface(shadow, new Color(0f, 0f, 0f, 0.22f));
            Transform frame = transform.Find("Frame");
            if (frame != null) FruitLabStyle.Surface(frame.GetComponent<Image>(), FruitLabStyle.Border, true);
            if (useLabActionStyle)
            {
                if (image != null) image.gameObject.SetActive(false);
                if (text == null) text = FruitLabStyle.CreateText("Label", transform, 15f, FruitLabStyle.Text);
                text.color = primary ? FruitLabStyle.Background : FruitLabStyle.Text;
                text.fontSize = 15f;
                text.enableAutoSizing = false;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Midline;
                text.raycastTarget = false;
                FruitLabStyle.Stretch(text.rectTransform, 3f);
                return;
            }
            if (primary)
            {
                if (image != null) image.gameObject.SetActive(false);
                if (text == null) text = FruitLabStyle.CreateText("Label", transform, 14f, FruitLabStyle.Background);
                text.gameObject.SetActive(true);
                text.text = "Tahmin et";
                text.color = FruitLabStyle.Background;
                text.fontSize = 14f;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Midline;
                FruitLabStyle.Stretch(text.rectTransform, 2f);
                if (rectTransform.rect.height > rectTransform.rect.width)
                    ApplyVerticalAction(FruitLabGlyph.Symbol.Check, "Tahmin\net", FruitLabStyle.Background);
                else if (_labGlyph != null) _labGlyph.gameObject.SetActive(false);
            }
            else if (gameObject.name.StartsWith("ResetButton"))
            {
                if (text != null) text.gameObject.SetActive(false);
                if (image != null) image.gameObject.SetActive(false);
                if (_labGlyph == null) _labGlyph = transform.Find("Reset")?.GetComponent<FruitLabGlyph>();
                if (_labGlyph == null) _labGlyph = FruitLabGlyph.Create(transform, FruitLabGlyph.Symbol.Reset, FruitLabStyle.Text);
                _labGlyph.gameObject.SetActive(true);
                FruitLabStyle.Stretch(_labGlyph.rectTransform, 8f);
                if (rectTransform.rect.height > rectTransform.rect.width)
                    ApplyVerticalAction(FruitLabGlyph.Symbol.Reset, "Temizle", FruitLabStyle.Text);
            }
            else if (gameObject.name.StartsWith("SettingsButton"))
            {
                FruitLabStyle.SetIcon(image, "Settings");
            }
        }

        private void ApplyVerticalAction(FruitLabGlyph.Symbol symbol, string label, Color color)
        {
            if (_labGlyph == null) _labGlyph = transform.Find(symbol.ToString())?.GetComponent<FruitLabGlyph>();
            if (_labGlyph == null) _labGlyph = FruitLabGlyph.Create(transform, symbol, color);
            _labGlyph.gameObject.SetActive(true);
            float width = rectTransform.rect.width, height = rectTransform.rect.height;
            float iconSize = Mathf.Min(24f, width - 12f);
            FruitLabStyle.Place(_labGlyph.rectTransform, transform,
                (width - iconSize) * .5f, height * .65f - iconSize * .5f, iconSize, iconSize);
            if (text == null) text = FruitLabStyle.CreateText("Label", transform, 11f, color);
            text.gameObject.SetActive(true);
            text.text = label;
            text.color = color;
            text.fontSize = 11f;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Midline;
            // Existing prefab labels can belong to InnerBG; keep that hierarchy
            // intact so the same layout can also be saved in the editor.
            RectTransform labelParent = (RectTransform)text.transform.parent;
            FruitLabStyle.Place(text.rectTransform, labelParent, 2f, labelParent.rect.height * .15f,
                Mathf.Max(1f, labelParent.rect.width - 4f), labelParent.rect.height * .35f);
        }

        public void SetLabIconScale(float scale)
        {
            ResolveReferences();
            if (image != null) image.rectTransform.localScale = Vector3.one * scale;
        }

        public void ApplyLabActionStyle(bool primary)
        {
            useLabActionStyle = true;
            primaryLabAction = primary;
            ApplyLabStyle();
        }

        protected void SetLabBackground(Image background) => innerBg = background;

        public void SetOnPointerDownCallBack(Action onPointerDown)
        {
            _onPointerDown = onPointerDown;
        }
        
        public void SetOnPointerUpCallBack(Action onPointerUp)
        {
            _onPointerUp = onPointerUp;
        }

        public void SetButtonDown()
        {
            ResolveReferences();
            rectTransform.localScale = Vector3.one * 0.96f;
            if (outerBg != null)
            {
                outerBg.enabled = false;
            }

            if (shadow != null)
            {
                shadow.enabled = false;
            }

            if (innerBg == null) return;

            innerBg.rectTransform.offsetMin = Vector2.zero;
            innerBg.rectTransform.offsetMax = Vector2.zero;
        }

        public void SetButtonUp()
        {
            ResolveReferences();
            rectTransform.localScale = Vector3.one;
            if (outerBg != null)
            {
                outerBg.enabled = true;
            }

            if (shadow != null)
            {
                shadow.enabled = true;
            }

            if (innerBg == null) return;

            innerBg.rectTransform.offsetMin = innerBgOffsetMin;
            innerBg.rectTransform.offsetMax = innerBgOffsetMax;
        }
        
        public void SetText(string newText)
        {
            text.SetText(newText);
        }

        public void SetLocalPosition(Vector2 localPos)
        {
            transform.localPosition = localPos;
        }

        public void SetImageStatus(bool status)
        {
            image.gameObject.SetActive(status);
        }

        public void SetTextStatus(bool status)
        {
            text.gameObject.SetActive(status);
        }
        
        public void SetButtonStatus(bool status)
        {
            gameObject.SetActive(status);
        }
        
        public void SetColorOfImage(Color color)
        {
            if (innerBg != null)
            {
                innerBg.color = color;
            }

            if (outerBg != null)
            {
                outerBg.color = color;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDown?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUp?.Invoke();
        }

        public RectTransform GetRectTransform()
        {
            if (outerBg != null)
            {
                return outerBg.rectTransform;
            }

            return GetButtonRectTransform();
        }
        public RectTransform GetButtonRectTransform()
        {
            ResolveReferences();
            return rectTransform;
        }

        public void DestroyObject()
        {
            Destroy(gameObject);
        }
        
        public void SetupTutorialMode()
        {
            tutorialCanvas = gameObject.AddComponent<Canvas>();
            tutorialCanvas.overrideSorting = true;
            tutorialCanvas.sortingOrder = 2;

            tutorialRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        public void CleanupTutorialMode()
        {
            if( tutorialCanvas != null )
            {
                Destroy( tutorialCanvas );
            }
            if( tutorialRaycaster != null )
            {
                Destroy( tutorialRaycaster );
            }
        }
    }
    
    public interface IBaseButtonView
    {
        void Init();
        void SetText(string newText);
        void SetLocalPosition(Vector2 localPos);
        void SetImageStatus(bool status);
        void SetTextStatus(bool status);
        void SetButtonStatus(bool status);
        void SetColorOfImage(Color color);
        RectTransform GetRectTransform();
        RectTransform GetButtonRectTransform();
        void SetOnPointerDownCallBack(Action onPointerDown);
        void SetOnPointerUpCallBack(Action onPointerUp);
        void SetButtonDown();
        void SetButtonUp();
        void DestroyObject();
        void SetupTutorialMode();
        void CleanupTutorialMode();
    }
    
}

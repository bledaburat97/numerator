using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class BaseButtonView : MonoBehaviour, IBaseButtonView, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image outerBg;
        [SerializeField] private Image shadow;
        [SerializeField] private Image innerBg;
        [SerializeField] protected Image image;
        [SerializeField] private bool useLabActionStyle;
        [SerializeField] private bool primaryLabAction;
        [SerializeField] private bool useLabSettingsStyle;
        private Canvas tutorialCanvas;
        private GraphicRaycaster tutorialRaycaster;
        private Vector2 innerBgOffsetMin;
        private Vector2 innerBgOffsetMax;
        private Action _onPointerDown;
        private Action _onPointerUp;
        private Action _onPointerClick;
        private FruitLabGlyph _labGlyph;
        private LevelData _levelIdentity;
        public void ApplyActionAccent(Color color)
        {ResolveReferences();FruitLabStyle.Surface(innerBg,color);FruitLabStyle.Surface(outerBg,color*.72f);}
        public void ApplyLevelIdentity(LevelData definition)
        {
            _levelIdentity=definition?.Copy();ApplyLabStyle();
        }

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
            if (useLabSettingsStyle)
            {
                if (text != null) text.gameObject.SetActive(false);
                if (image == null) image = FruitLabStyle.Detail(transform, "SettingsIcon", Color.white);
                if (image != null)
                {
                    image.gameObject.SetActive(true); image.enabled = true;
                    FruitLabStyle.SetIcon(image, "Settings");
                    FruitLabStyle.Place(image.rectTransform, transform, 9, 9, 26, 26);
                    image.raycastTarget = false;
                }
                return;
            }
            if (useLabActionStyle)
            {
                if (image != null) image.gameObject.SetActive(false);
                if (text == null) text = FruitLabStyle.CreateText("Label", transform, 15f, FruitLabStyle.Text);
                text.color = primary ? FruitLabStyle.Background : FruitLabStyle.Text;
                FruitLabStyle.TextStyle(text, 18f);
                text.enableAutoSizing = false;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Midline;
                text.raycastTarget = false;
                FruitLabStyle.Stretch(text.rectTransform, 3f);
                var oldBadge=transform.Find("LevelKind");if(oldBadge!=null)oldBadge.gameObject.SetActive(_levelIdentity!=null);
                if(_levelIdentity!=null)
                {
                    var identity=LevelIdentity.For(_levelIdentity);
                    FruitLabStyle.Surface(innerBg,identity.Accent);FruitLabStyle.Surface(outerBg,identity.Accent*.72f);
                    text.color=FruitLabStyle.Background;
                    var badge=transform.Find("LevelKind")?.GetComponent<FruitLabGlyph>() ?? FruitLabGlyph.Create(transform,identity.Symbol,FruitLabStyle.Background);
                    badge.gameObject.SetActive(true);badge.name="LevelKind";badge.Type=identity.Symbol;badge.color=FruitLabStyle.Background;badge.SetAllDirty();
                    FruitLabStyle.Place(badge.rectTransform,transform,12,(rectTransform.rect.height-26)*.5f,26,26);
                    text.rectTransform.offsetMin=new Vector2(44,3);text.rectTransform.offsetMax=new Vector2(-6,-3);
                }
                return;
            }
            if (primary || gameObject.name.StartsWith("ResetButton"))
            {
                ApplyIconOnlyAction(primary ? FruitLabGlyph.Symbol.Check : FruitLabGlyph.Symbol.Eraser,
                    primary ? FruitLabStyle.Background : FruitLabStyle.Text);
            }
            else if (gameObject.name.StartsWith("SettingsButton"))
            {
                FruitLabStyle.SetIcon(image, "Settings");
            }
        }

        private void ApplyIconOnlyAction(FruitLabGlyph.Symbol symbol, Color tint)
        {
            if (text != null) text.gameObject.SetActive(false);
            if (image != null) image.gameObject.SetActive(false);
            foreach (var label in GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
            foreach (var old in GetComponentsInChildren<FruitLabGlyph>(true)) old.gameObject.SetActive(false);
            if (_labGlyph == null) _labGlyph = transform.Find("ActionIcon")?.GetComponent<FruitLabGlyph>();
            if (_labGlyph == null) { _labGlyph = FruitLabGlyph.Create(transform, symbol, tint); _labGlyph.name = "ActionIcon"; }
            _labGlyph.Type = symbol; _labGlyph.color = tint; _labGlyph.SetAllDirty();
            _labGlyph.gameObject.SetActive(true);
            float size = Mathf.Min(38f, Mathf.Min(rectTransform.rect.width - 10f, rectTransform.rect.height - 10f));
            FruitLabStyle.Place(_labGlyph.rectTransform, transform,
                (rectTransform.rect.width-size)*.5f, (rectTransform.rect.height-size)*.5f, size, size);
        }

        public void SetLabIconInset(float inset)
        {
            ResolveReferences();
            if (image == null) return;
            // Keep the authored parent: settings icons can be nested inside a
            // prefab face, whose children cannot be reparented in the Editor.
            var parent = image.rectTransform.parent as RectTransform;
            if (parent == null) return;
            Rect bounds = rectTransform.rect;
            Vector2 min = parent.InverseTransformPoint(rectTransform.TransformPoint(new Vector3(bounds.xMin + inset, bounds.yMin + inset)));
            Vector2 max = parent.InverseTransformPoint(rectTransform.TransformPoint(new Vector3(bounds.xMax - inset, bounds.yMax - inset)));
            FruitLabStyle.Place(image.rectTransform, parent, min.x - parent.rect.xMin, min.y - parent.rect.yMin,
                max.x - min.x, max.y - min.y);
        }

        public void ApplyLabActionStyle(bool primary)
        {
            useLabActionStyle = true;
            primaryLabAction = primary;
            ApplyLabStyle();
        }

        public void ApplyLabSettingsStyle()
        {
            useLabActionStyle = false; useLabSettingsStyle = true; ApplyLabStyle();
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

        public void SetOnPointerClickCallBack(Action onPointerClick)
        {
            _onPointerClick = onPointerClick;
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button == PointerEventData.InputButton.Left)
                _onPointerClick?.Invoke();
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
        void SetOnPointerClickCallBack(Action onPointerClick);
        void SetButtonDown();
        void SetButtonUp();
        void DestroyObject();
        void SetupTutorialMode();
        void CleanupTutorialMode();
    }
    
}

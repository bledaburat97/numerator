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
        [SerializeField] private MonoBehaviour presentation;
        private Canvas tutorialCanvas;
        private GraphicRaycaster tutorialRaycaster;
        private Vector2 innerBgOffsetMin;
        private Vector2 innerBgOffsetMax;
        private Action _onPointerDown;
        private Action _onPointerUp;
        private Action _onPointerClick;
        public TMP_Text Label=>text;
        public Image Icon=>image;
        public Image OuterSurface=>outerBg;
        public Image PressedSurface=>innerBg;
        public Image Shadow=>shadow;
        public MonoBehaviour Presentation=>presentation;

        public void ConfigureReferences(RectTransform rect,TMP_Text label,Image outer,Image dropShadow,Image face,Image icon)
        {rectTransform=rect;text=label;outerBg=outer;shadow=dropShadow;innerBg=face;image=icon;}
        public void SetPresentation(MonoBehaviour value)
        {
            if(value!=null && !(value is IButtonPresentation))throw new ArgumentException("Button presentation must implement IButtonPresentation.");
            presentation=value;
        }
        public void SetIconReference(Image value)=>image=value;
        private void ResolveReferences(){if(rectTransform==null)rectTransform=transform as RectTransform;}
        public void Init()
        {
            ResolveReferences();transform.localScale=Vector3.one;
            if(presentation!=null)((IButtonPresentation)presentation).Apply();
            if(innerBg==null)return;
            innerBgOffsetMin=innerBg.rectTransform.offsetMin;innerBgOffsetMax=innerBg.rectTransform.offsetMax;
        }

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
            if(text!=null)text.SetText(newText);
        }

        public void SetLocalPosition(Vector2 localPos)
        {
            transform.localPosition = localPos;
        }

        public void SetImageStatus(bool status)
        {
            if(image!=null)image.gameObject.SetActive(status);
        }

        public void SetTextStatus(bool status)
        {
            if(text!=null)text.gameObject.SetActive(status);
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
    
    public interface IButtonPresentation { void Apply(); }

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

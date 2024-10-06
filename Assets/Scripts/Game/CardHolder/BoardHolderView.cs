using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class BoardHolderView : MonoBehaviour, IBoardHolderView
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private GraphicRaycaster tutorialRaycaster;

        private Camera _cam;
        
        public void SetLocalScale()
        {
            transform.localScale = Vector3.one;
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
        
        public Vector3 GetGlobalPosition()
        {
            return transform.position;
        }
        
        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void DestroyObject()
        {
            Destroy(gameObject);
        }
        
        public void SetCamera(Camera cam)
        {
            _cam = cam;
        }

        /*
        public void SetColor()
        {
            frame.color = ConstantValues.BOARD_CARD_HOLDER_COLOR;
        }
        */
        
        public void SetOnClick(Action onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
        
        public Vector3 GetPosition()
        {
            return _cam.WorldToScreenPoint(transform.position);
        }
        
        public void SetHighlightStatus(bool status)
        {
            highlightImage.gameObject.SetActive(status);
        }
        
        public void SetupTutorialMode()
        {
            tutorialCanvas.overrideSorting = true;
            tutorialCanvas.sortingOrder = 2;
        }

        public void CleanupTutorialMode()
        {
            tutorialCanvas.overrideSorting = false;
        }
    }

    public interface IBoardHolderView : IBaseHolderView
    {
        void SetCamera(Camera cam);
        void SetOnClick(Action onClick);
        Vector3 GetPosition();
        void SetHighlightStatus(bool status);
        void SetupTutorialMode();
        void CleanupTutorialMode();
    }
}
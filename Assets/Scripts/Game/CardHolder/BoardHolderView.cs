using System;
using DG.Tweening;
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
        [SerializeField] private RectTransform garden;
        [SerializeField] private Image successFrameImage;
        
        private Camera _cam;

#if UNITY_EDITOR
        [ContextMenu("Fruit Lab/Apply Board Style")]
        public void ApplyLabStyle()
        {
            Transform surface = transform.Find("Image");
            if (surface != null)
            {
                FruitLabStyle.Stretch((RectTransform)surface, 3f);
                FruitLabStyle.RecessSurface(surface.GetComponent<Image>());
                FruitLabStyle.Rim((RectTransform)surface);
            }
            FruitLabStyle.Surface(highlightImage, new Color(0.53f, 0.9f, 0.78f, 0.3f), true);
            FruitLabStyle.Surface(successFrameImage, FruitLabStyle.Correct, true);
            highlightImage.sprite = successFrameImage.sprite = FruitLabStyle.ResultFrame;
            highlightImage.pixelsPerUnitMultiplier = successFrameImage.pixelsPerUnitMultiplier = 1.2f;
            highlightImage.raycastTarget = successFrameImage.raycastTarget = false;
        }
#endif
        
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

        public RectTransform GetGardenRectTransform()
        {
            return garden;
        }
        
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

        public Sequence PlaySuccessFrameAnimation(float delayDuration)
        {
            Sequence sequence = DOTween.Sequence();
            if (successFrameImage == null) return sequence;

            RectTransform frameTransform = successFrameImage.rectTransform;
            Vector3 targetScale = frameTransform.localScale == Vector3.zero ? Vector3.one : frameTransform.localScale;
            Color frameColor = successFrameImage.color;
            frameColor.a = 0f;
            successFrameImage.color = frameColor;
            frameTransform.localScale = targetScale * 0.85f;

            return sequence
                .AppendInterval(delayDuration)
                .AppendCallback(() => successFrameImage.gameObject.SetActive(true))
                .Append(successFrameImage.DOFade(1f, 0.12f))
                .Join(frameTransform.DOScale(targetScale * 1.08f, 0.12f).SetEase(Ease.OutBack))
                .Append(frameTransform.DOScale(targetScale, 0.08f).SetEase(Ease.OutQuad));
        }

        public void SetSuccessFrameStatus(bool status)
        {
            if (successFrameImage == null) return;

            successFrameImage.gameObject.SetActive(status);
            Color frameColor = successFrameImage.color;
            frameColor.a = status ? 1f : 0f;
            successFrameImage.color = frameColor;
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
        RectTransform GetGardenRectTransform();
        Sequence PlaySuccessFrameAnimation(float delayDuration);
        void SetSuccessFrameStatus(bool status);
    }
}

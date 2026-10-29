using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Scripts
{
    public class ResultAreaView : NetworkBehaviour, IResultAreaView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private ResultBlockView resultBlockPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
        [SerializeField] private CanvasGroup canvasGroup;
        private Coroutine _scrollRoutine;
        private TMP_Text _emptyState;

        public void ApplyLabLayout(float spacing)
        {
            FruitLabStyle.Stretch(scrollRect.viewport, 6f);
            Image oldBackground = scrollRect.GetComponent<Image>();
            Mask mask = scrollRect.GetComponent<Mask>();
            if (mask != null)
            {
                // The mask still needs an opaque graphic to write the stencil.
                mask.showMaskGraphic = false;
                if (oldBackground != null) oldBackground.color = Color.white;
            }
            else if (oldBackground != null) oldBackground.color = Color.clear;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
            verticalLayoutGroup.spacing = spacing;
            int padding = Mathf.CeilToInt(spacing);
            verticalLayoutGroup.padding = new RectOffset(0, 0, padding, padding);
            verticalLayoutGroup.childControlWidth = false;
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            ContentSizeFitter fitter = rectTransform.GetComponent<ContentSizeFitter>();
            if (fitter != null) fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rectTransform.ForceUpdateRectTransforms();
            if (_emptyState == null)
            {
                _emptyState = scrollRect.viewport.Find("HistoryHint")?.GetComponent<TMP_Text>();
            }
            if (_emptyState == null)
            {
                _emptyState = FruitLabStyle.CreateText("HistoryHint", scrollRect.viewport, 12f, FruitLabStyle.Muted);
                _emptyState.text = "Tahminlerin burada görünecek";
            }
            FruitLabStyle.Stretch(_emptyState.rectTransform, 8f);
            foreach (ResultBlockView row in rectTransform.GetComponentsInChildren<ResultBlockView>())
                row.Resize(GetWidth(), GetRowHeight());
        }

        public void SetEmptyState(bool empty)
        {
            if (_emptyState != null) _emptyState.gameObject.SetActive(empty);
        }

        public void EnableReview()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            scrollRect.enabled = true;
            scrollRect.vertical = true;
            Image hitArea = scrollRect.GetComponent<Image>();
            if (hitArea != null) hitArea.raycastTarget = true;
        }

        public float GetRowHeight()
        {
            return FruitLabLayoutMetrics.GetHistoryRowHeight(scrollRect.viewport.rect.height,
                Mathf.Ceil(verticalLayoutGroup.spacing), rectTransform.rect.width / 331f);
        }
        
        public CanvasGroup GetCanvasGroup()
        {
            return canvasGroup;
        }

        public float GetWidth()
        {
            return rectTransform.rect.width;
        }

        public void SetScrollPositionToBottom()
        {
            if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
            _scrollRoutine = StartCoroutine(ScrollToBottomCoroutine());
        }
        
        IEnumerator ScrollToBottomCoroutine () {
            yield return new WaitForEndOfFrame ();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
            _scrollRoutine = null;
        }
        
        public IResultBlockView CreateResultBlock()
        {
            SetEmptyState(false);
            return Instantiate(resultBlockPrefab, transform);
        }

        public ResultAreaInfo GetResultAreaInfo()
        {
            return new ResultAreaInfo()
            {
                topPoint = rectTransform.position,
                resultBlockSize = new Vector2(rectTransform.rect.width, GetRowHeight()),
                spacing = verticalLayoutGroup.spacing,
            };
        }
    }

    public interface IResultAreaView
    {
        ResultAreaInfo GetResultAreaInfo();
        IResultBlockView CreateResultBlock();
        void SetScrollPositionToBottom();
        CanvasGroup GetCanvasGroup();
        float GetWidth();
        float GetRowHeight();
        void SetEmptyState(bool empty);
        void EnableReview();
    }

    public struct ResultAreaInfo
    {
        public Vector2 topPoint;
        public Vector2 resultBlockSize;
        public float spacing;
    }
}

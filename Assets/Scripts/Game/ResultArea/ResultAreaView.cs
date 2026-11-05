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
        private TMP_Text _emptyState;
        private IReadOnlyList<ResultBlockModel> _history;
        private readonly Dictionary<int, ResultBlockView> _visible = new Dictionary<int, ResultBlockView>();
        private readonly Stack<ResultBlockView> _pool = new Stack<ResultBlockView>();
        private readonly List<int> _releasedIndexes = new List<int>();
        private readonly HashSet<int> _evidence = new HashSet<int>();
        private float _evidenceUntil;
        public IReadOnlyCollection<int> EvidenceRows => _evidence;
        public void FocusEvidence(IReadOnlyList<int> guessNumbers)
        {
            _evidence.Clear(); int focus = -1;
            foreach (int number in guessNumbers)
                if (number > 0 && number <= HistoryCount) { _evidence.Add(number - 1); focus = Mathf.Max(focus, number - 1); }
            if (focus < 0) { ClearEvidence(); return; }
            _evidenceUntil = Time.unscaledTime + 7;
            _scrollPending = false; scrollRect.StopMovement();
            float offset = verticalLayoutGroup.padding.top + focus * (GetRowHeight() + verticalLayoutGroup.spacing) - scrollRect.viewport.rect.height * .4f;
            rectTransform.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, Mathf.Max(0, rectTransform.rect.height - scrollRect.viewport.rect.height)));
            RefreshWindow(false);
            foreach (var pair in _visible) if (pair.Value != null) pair.Value.SetEvidenceHighlight(_evidence.Contains(pair.Key));
        }
        public void ClearEvidence()
        {
            _evidence.Clear(); _evidenceUntil = 0;
            foreach (var pair in _visible) if (pair.Value != null) pair.Value.SetEvidenceHighlight(false);
        }
        private const int BufferRows = 2;
        private bool _listening, _scrollPending, _refreshing;
        private float _rowHeight, _rowWidth, _viewportHeight;
        private float _clippedHeight;
        public int VisibleRowCount => _visible.Count;
        public int AllocatedRowCount => _visible.Count + _pool.Count;
        public int HistoryCount => _history?.Count ?? 0;
        public ResultBlockView GetVisibleRow(int index) => _visible.TryGetValue(index, out var row) ? row : null;

        public void ApplyLabLayout(float spacing, float clippedHeight = 0f)
        {
            _clippedHeight = Mathf.Max(0f, clippedHeight);
            FruitLabStyle.Stretch(scrollRect.viewport, 6f);
            Image oldBackground = scrollRect.GetComponent<Image>();
            Mask mask = scrollRect.GetComponent<Mask>();
            if (mask != null)
            {
                mask.showMaskGraphic = false;
                if (oldBackground != null) oldBackground.color = Color.white;
            }
            else if (oldBackground != null) oldBackground.color = Color.clear;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(.5f, 1f);
            if (_history == null) rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, rectTransform.sizeDelta.y);
            verticalLayoutGroup.spacing = spacing;
            int padding = Mathf.CeilToInt(spacing);
            verticalLayoutGroup.padding = new RectOffset(0, 0, padding, padding);
            verticalLayoutGroup.childControlWidth = verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childForceExpandWidth = verticalLayoutGroup.childForceExpandHeight = false;
            SetLayoutMode();
            rectTransform.ForceUpdateRectTransforms();
            if (_emptyState == null) _emptyState = scrollRect.viewport.Find("HistoryHint")?.GetComponent<TMP_Text>();
            if (_emptyState == null)
            {
                _emptyState = FruitLabStyle.CreateText("HistoryHint", scrollRect.viewport, 16f, FruitLabStyle.Muted);
                _emptyState.text = "Tahminlerin burada görünecek";
            }
            FruitLabStyle.Stretch(_emptyState.rectTransform, 8f);
            if (_history != null)
            {
                SetEmptyState(_history.Count == 0);
                RefreshWindow(true);
                // Closing a shorter viewport must not leave the content beyond its new end.
                float offset = Mathf.Clamp(rectTransform.anchoredPosition.y, 0f,
                    Mathf.Max(0f, rectTransform.rect.height - scrollRect.viewport.rect.height));
                if (Mathf.Abs(rectTransform.anchoredPosition.y - offset) > .01f)
                {
                    rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, offset);
                    RefreshWindow(false);
                }
            }
            else foreach (ResultBlockView row in rectTransform.GetComponentsInChildren<ResultBlockView>()) row.Resize(GetWidth(), GetRowHeight());
        }

        private void SetLayoutMode()
        {
            // Runtime history positions its bounded set of rows directly. Legacy editor previews use the layout group.
            verticalLayoutGroup.enabled = _history == null;
            ContentSizeFitter fitter = rectTransform.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = _history == null;
                if (_history == null) fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        public void SetHistory(IReadOnlyList<ResultBlockModel> history)
        {
            _history = history;
            SetLayoutMode(); Listen(); HistoryChanged();
        }
        private void Listen()
        {
            if (_listening || scrollRect == null) return;
            scrollRect.onValueChanged.AddListener(OnScroll); _listening = true;
        }
        private void OnEnable() => Listen();
        private void OnDisable()
        {
            if (_listening && scrollRect != null) scrollRect.onValueChanged.RemoveListener(OnScroll);
            _listening = false;
        }
        private void OnScroll(Vector2 position) => RefreshWindow(false);
        private void LateUpdate()
        {
            if (_evidenceUntil > 0 && Time.unscaledTime >= _evidenceUntil) ClearEvidence();
            if (_scrollPending)
            {
                _scrollPending = false;
                ScrollToBottom();
            }
            else if (_history != null && (Mathf.Abs(_viewportHeight - scrollRect.viewport.rect.height) > .01f || Mathf.Abs(_rowWidth - GetWidth()) > .01f))
                RefreshWindow(true);
        }
        public void HistoryChanged()
        {
            SetEmptyState(HistoryCount == 0);
            if (HistoryCount == 0)
            {
                ClearEvidence();
                _scrollPending = false; scrollRect.StopMovement(); rectTransform.anchoredPosition = Vector2.zero;
            }
            RefreshWindow(false);
        }
        public void RefreshRow(int index)
        {
            if (_visible.TryGetValue(index, out var row) && row != null) Bind(row, index);
        }
        public void RefreshVisibleRows()
        {
            foreach (var pair in _visible) if (pair.Value != null) Bind(pair.Value, pair.Key);
        }
        private void Bind(ResultBlockView row, int index)
        {
            row.Bind(_history[index], _rowWidth, _rowHeight);
            row.HistoryIndex = index;
            row.SetEvidenceHighlight(_evidence.Contains(index));
        }
        private void RefreshWindow(bool resize)
        {
            if (_history == null || _refreshing) return;
            _refreshing = true;
            try
            {
                float height = scrollRect.viewport.rect.height, width = GetWidth(), rowHeight = GetRowHeight();
                bool sizeChanged = resize || Mathf.Abs(_rowHeight - rowHeight) > .01f || Mathf.Abs(_rowWidth - width) > .01f;
                _viewportHeight = height; _rowWidth = width; _rowHeight = rowHeight;
                float spacing = verticalLayoutGroup.spacing, padding = verticalLayoutGroup.padding.top;
                float stride = rowHeight + spacing;
                float contentHeight = Mathf.Max(height, padding * 2 + _history.Count * rowHeight + Mathf.Max(0, _history.Count - 1) * spacing);
                if (Mathf.Abs(rectTransform.rect.height - contentHeight) > .01f) rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
                float offset = Mathf.Clamp(rectTransform.anchoredPosition.y, 0, Mathf.Max(0, contentHeight - height));
                int first = Mathf.Max(0, Mathf.FloorToInt((offset - padding) / Mathf.Max(1, stride)) - BufferRows);
                int end = Mathf.Min(_history.Count, Mathf.CeilToInt((offset + height - padding) / Mathf.Max(1, stride)) + BufferRows);
                _releasedIndexes.Clear();
                foreach (var pair in _visible) if (pair.Key < first || pair.Key >= end) _releasedIndexes.Add(pair.Key);
                foreach (int index in _releasedIndexes)
                {
                    var row = _visible[index]; _visible.Remove(index);
                    if (row == null) continue;
                    row.ResetForReuse(); row.gameObject.SetActive(false); _pool.Push(row);
                }
                bool windowChanged = _releasedIndexes.Count > 0;
                for (int index = first; index < end; index++)
                {
                    bool added = !_visible.TryGetValue(index, out var row) || row == null;
                    if (added)
                    {
                        windowChanged = true;
                        do { row = _pool.Count > 0 ? _pool.Pop() : null; } while (row == null && _pool.Count > 0);
                        if (row == null) row = Instantiate(resultBlockPrefab, rectTransform);
                        _visible[index] = row; row.gameObject.SetActive(true);
                        Bind(row, index);
                    }
                    if (added || sizeChanged)
                    {
                        var rect = row.GetRectTransform();
                        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f); rect.pivot = new Vector2(.5f, 1f);
                        row.Resize(width, rowHeight);
                        rect.anchoredPosition = new Vector2(0, -padding - index * stride);
                    }
                }
                if (windowChanged)
                    for (int index = first; index < end; index++) _visible[index].transform.SetAsLastSibling();
            }
            finally { _refreshing = false; }
        }

        public void SetEmptyState(bool empty)
        {
            if (_emptyState != null && _emptyState.gameObject.activeSelf != empty) _emptyState.gameObject.SetActive(empty);
        }
        public void EnableReview()
        {
            canvasGroup.alpha = 1; canvasGroup.interactable = canvasGroup.blocksRaycasts = true;
            scrollRect.enabled = scrollRect.vertical = true;
            Image hitArea = scrollRect.GetComponent<Image>();
            if (hitArea != null) hitArea.raycastTarget = true;
        }
        public float GetRowHeight() => FruitLabLayoutMetrics.GetHistoryRowHeight(scrollRect.viewport.rect.height + _clippedHeight,
            Mathf.Ceil(verticalLayoutGroup.spacing), rectTransform.rect.width / 331f);
        public CanvasGroup GetCanvasGroup() => canvasGroup;
        public float GetWidth() => rectTransform.rect.width;
        public void SetScrollPositionToBottom()
        {
            ScrollToBottom();
            if (Application.isPlaying) _scrollPending = true;
        }
        private void ScrollToBottom()
        {
            if (_history != null) RefreshWindow(false);
            scrollRect.StopMovement();
            if (_history == null) scrollRect.verticalNormalizedPosition = 0;
            else
            {
                rectTransform.anchoredPosition = new Vector2(0, Mathf.Max(0, rectTransform.rect.height - scrollRect.viewport.rect.height));
                RefreshWindow(false);
            }
        }
        public IResultBlockView CreateResultBlock()
        {
            SetEmptyState(false);
            return Instantiate(resultBlockPrefab, rectTransform);
        }
        public ResultAreaInfo GetResultAreaInfo() => new ResultAreaInfo {
            topPoint = rectTransform.position, resultBlockSize = new Vector2(GetWidth(), GetRowHeight()), spacing = verticalLayoutGroup.spacing
        };
    }

    public interface IResultAreaView
    {
        ResultAreaInfo GetResultAreaInfo();
        IResultBlockView CreateResultBlock();
        void SetHistory(IReadOnlyList<ResultBlockModel> history);
        void HistoryChanged();
        void RefreshRow(int index);
        void RefreshVisibleRows();
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

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    public class PossibleHolderIndicatorView : MonoBehaviour, IPossibleHolderIndicatorView, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField] private TMP_Text imageText;
        [SerializeField] private Image crossImage;
        [SerializeField] private RectTransform rectTransform;
        private Image _surface;
        [SerializeField] private FruitLabGlyph _symbol;
        private Action _clicked;
        private Color _numberColor = FruitLabStyle.Text;
        public Color NumberColor => _numberColor;
        public void SetNumberColor(Color color) { _numberColor = color; imageText.color = color; }
        public bool IsExcluded { get; private set; }
        public bool IsConfirmed { get; private set; }
        public int PositionNumber { get; private set; }
        public void Init(PossibleHolderIndicatorModel model) { SetLocalScale(); SetLocalPosition(model.localPosition); }
        public void Configure(int position, Action clicked)
        {
            PositionNumber = position; _clicked = clicked;
            if (crossImage != null) { crossImage.gameObject.SetActive(false); crossImage.raycastTarget = false; }
            var oldInner = transform.Find("InnerImage");
            if (oldInner != null) oldInner.gameObject.SetActive(false);
            _surface = FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Recess); _surface.raycastTarget = true;
            if (_symbol == null) _symbol = FruitLabGlyph.Create(transform, FruitLabGlyph.Symbol.Cross, FruitLabStyle.OpponentPlayer);
            _symbol.raycastTarget = false;
            imageText.transform.SetParent(transform, false); FruitLabStyle.Stretch(imageText.rectTransform);
            imageText.alignment = TextAlignmentOptions.Center; imageText.fontStyle = FontStyles.Bold;
            imageText.enableAutoSizing = false; imageText.raycastTarget = false;
            SetText(position.ToString()); SetDeduction(false, false);
        }
        public void SetDeduction(bool excluded, bool confirmed)
        {
            IsExcluded = excluded; IsConfirmed = confirmed;
            if (_surface == null) return;
            _surface.color = confirmed ? new Color(.08f, .33f, .26f, 1) : excluded ? new Color(.24f, .10f, .12f, 1) : FruitLabStyle.Recess;
            imageText.gameObject.SetActive(!excluded && !confirmed); imageText.color = _numberColor;
            _symbol.gameObject.SetActive(excluded || confirmed);
            _symbol.Type = confirmed ? FruitLabGlyph.Symbol.Check : FruitLabGlyph.Symbol.Cross;
            _symbol.color = confirmed ? FruitLabStyle.Correct : new Color(1, .57f, .57f, 1);
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data == null || data.button == PointerEventData.InputButton.Left) _clicked?.Invoke();
        }
        public void OnPointerDown(PointerEventData data) { } // Keep notebook clicks inside the editing overlay.
        public void SetLocalScale() => transform.localScale = Vector3.one;
        public void SetLocalPosition(Vector2 position) => transform.localPosition = position;
        public void SetText(string value) => imageText.text = value;
        public void SetStatus(bool status)
        {
            if (_surface == null) Configure(int.TryParse(imageText.text, out int number) ? number : 1, _clicked);
            SetDeduction(!status, false);
        }
        public void SetParent(RectTransform parent) => transform.SetParent(parent, false);
        public void DestroyObject() { if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject); }
        public RectTransform GetRectTransform() => rectTransform;
        public void SetEditingSize(Vector2 touchSize)
        {
            // A thin visible control inside a full-height, non-overlapping hit
            // target. The board reserves this space before editing is opened.
            rectTransform.sizeDelta = touchSize;
            var hit = GetComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            _surface = FruitLabStyle.Detail(transform, "EditPlate", FruitLabStyle.Recess);
            FruitLabStyle.Place(_surface.rectTransform, transform, 0, (touchSize.y-20)*.5f, touchSize.x, 20);
            FruitLabStyle.Place(imageText.rectTransform, transform, 0, (touchSize.y-20)*.5f, touchSize.x, 20);
            FruitLabStyle.TextStyle(imageText, 16);
            if (_symbol != null)
            {
                _symbol.rectTransform.anchorMin = _symbol.rectTransform.anchorMax = Vector2.one*.5f;
                _symbol.rectTransform.anchoredPosition=Vector2.zero;
                _symbol.rectTransform.sizeDelta=Vector2.one*17;
            }
            SetDeduction(IsExcluded, IsConfirmed);
            imageText.transform.SetAsLastSibling(); if(_symbol!=null)_symbol.transform.SetAsLastSibling();
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
            imageText.fontSize = Mathf.Clamp(Mathf.Min(size.x * .95f, size.y - 2), 8, 12);
            if (_symbol != null)
            {
                _symbol.rectTransform.anchorMin = _symbol.rectTransform.anchorMax = Vector2.one * .5f;
                _symbol.rectTransform.anchoredPosition = Vector2.zero;
                _symbol.rectTransform.sizeDelta = Vector2.one * Mathf.Max(7, Mathf.Min(size.x - 1, size.y - 2));
            }
        }
    }
    public interface IPossibleHolderIndicatorView
    {
        void Init(PossibleHolderIndicatorModel model);
        void SetStatus(bool status);
        void SetParent(RectTransform parent);
        void DestroyObject();
        void SetLocalScale();
        void SetLocalPosition(Vector2 position);
        void SetText(string text);
        RectTransform GetRectTransform();
        void SetSize(Vector2 size);
    }
}

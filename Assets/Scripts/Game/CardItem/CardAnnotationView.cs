using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Scripts
{
    // Owns the local marking overlay, its layout and restoration of the powerup row.
    public sealed class CardAnnotationView : IDisposable
    {
        private readonly FruitLabGameLayout _layout;
        private readonly Action _dismiss;
        private readonly Action<CardAnnotation> _apply;
        private readonly Action<int> _toggle;
        private readonly Func<int,RectTransform> _boardHolder;
        private readonly IInitialCardAreaPresentation _cards;
        private readonly System.Collections.Generic.List<PossibleHolderIndicatorView> _positions = new System.Collections.Generic.List<PossibleHolderIndicatorView>();
        private PositionDeductionService _state;
        private int _fruit;
        private readonly Button[] _buttons = new Button[3];
        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _overlay, _toolbar;
        private CanvasGroup _powerGroup;
        private float _previousAlpha;
        private bool _previousRaycasts, _previousInteractable;
        public RectTransform Toolbar => _toolbar;
        public Button GetButton(int index) => _buttons[index];

        public CardAnnotationView(FruitLabGameLayout layout, Action dismiss, Action<CardAnnotation> apply,
            IInitialCardAreaPresentation cards=null,Func<int,RectTransform> boardHolder=null,Action<int> toggle=null)
        {
            _layout = layout; _dismiss = dismiss; _apply = apply;
            _cards=cards;_boardHolder=boardHolder;_toggle=toggle;
            if (_layout != null) _layout.LayoutChanged += RefreshLayout;
        }

        public PossibleHolderIndicatorView GetPositionButton(int slot)=>slot>=0 && slot<_positions.Count?_positions[slot]:null;
        public void RenderPositions(PositionDeductionService state,int fruit)
        {
            _state=state;_fruit=fruit;
            if(_overlay==null || state==null || _cards==null)return;
            var source=_cards.GetHolderView(fruit-1);
            while(_positions.Count<state.SlotCount)
            {
                int slot=_positions.Count;
                var item=UnityEngine.Object.Instantiate(source.Indicators[slot],_overlay,false);
                item.name="EditPosition"+(slot+1);item.Configure(slot+1,()=>_toggle?.Invoke(slot));
                _positions.Add(item);
            }
            for(int slot=0;slot<_positions.Count;slot++)
            {
                _positions[slot].gameObject.SetActive(slot<state.SlotCount);
                if(slot<state.SlotCount)
                {
                    _positions[slot].SetNumberColor(source.Indicators[slot].NumberColor);
                    _positions[slot].SetDeduction(!state.IsPossible(fruit,slot),state.FixedSlot(fruit)==slot);
                }
            }
            RefreshLayout();
        }

        public void Show()
        {
            EnsureToolbar();
            _layout.SetCardAnnotationMode(true);
            _powerGroup = _layout.PowerUpView.GetComponent<CanvasGroup>();
            if (_powerGroup == null) _powerGroup = _layout.PowerUpView.gameObject.AddComponent<CanvasGroup>();
            _previousAlpha = _powerGroup.alpha; _previousRaycasts = _powerGroup.blocksRaycasts;
            _previousInteractable = _powerGroup.interactable;
            _powerGroup.alpha = 0; _powerGroup.blocksRaycasts = _powerGroup.interactable = false;
            _overlay.gameObject.SetActive(true); _overlay.SetAsLastSibling();
            RefreshLayout();
        }

        public void Hide()
        {
            if (_overlay != null) _overlay.gameObject.SetActive(false);
            if (_powerGroup != null)
            {
                _powerGroup.alpha = _previousAlpha; _powerGroup.blocksRaycasts = _previousRaycasts;
                _powerGroup.interactable = _previousInteractable; _powerGroup = null;
            }
            if (_layout != null) _layout.SetCardAnnotationMode(false);
        }

        public void RefreshLayout()
        {
            if (_overlay == null || !_overlay.gameObject.activeSelf) return;
            _layout.PowerUpArea.ForceUpdateRectTransforms();
            _layout.PowerUpArea.GetWorldCorners(_corners);
            Vector2 min = _overlay.InverseTransformPoint(_corners[0]), max = _overlay.InverseTransformPoint(_corners[2]);
            FruitLabStyle.Place(_toolbar, _overlay, min.x - _overlay.rect.xMin, min.y - _overlay.rect.yMin, max.x - min.x, max.y - min.y);
            float gap = 8, width = (_toolbar.rect.width - gap * 2) / 3;
            for (int i = 0; i < 3; i++) FruitLabStyle.Place((RectTransform)_buttons[i].transform, _toolbar, i * (width + gap), 0, width, _toolbar.rect.height);
            if(_state==null || _boardHolder==null)return;
            _layout.BoardArea.GetWorldCorners(_corners);
            float bottom=_overlay.InverseTransformPoint(_corners[0]).y-_overlay.rect.yMin;
            float touchHeight=Mathf.Min(48f,new FruitLabLayoutMetrics(_layout.LayoutSize.x,_layout.LayoutSize.y).BoardFooterHeight);
            for(int slot=0;slot<_positions.Count && slot<_state.SlotCount;slot++)
            {
                var holder=_boardHolder(slot);if(holder==null)continue;
                holder.GetWorldCorners(_corners);
                Vector2 left=_overlay.InverseTransformPoint(_corners[0]),right=_overlay.InverseTransformPoint(_corners[2]);
                float cellWidth=Mathf.Max(12,right.x-left.x-4);
                FruitLabStyle.Place(_positions[slot].GetRectTransform(),_overlay,left.x-_overlay.rect.xMin+2,bottom,cellWidth,touchHeight);
                _positions[slot].SetEditingSize(new Vector2(cellWidth,touchHeight));
            }
        }

        private void EnsureToolbar()
        {
            if (_overlay != null) return;
            _overlay = new GameObject("CardAnnotationOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(EventTrigger)).GetComponent<RectTransform>();
            _overlay.SetParent(_layout.transform, false); FruitLabStyle.Stretch(_overlay);
            _overlay.GetComponent<Image>().color = Color.clear;
            var dismiss = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            dismiss.callback.AddListener(_ => _dismiss()); _overlay.GetComponent<EventTrigger>().triggers.Add(dismiss);
            _toolbar = new GameObject("CardAnnotationToolbar", typeof(RectTransform)).GetComponent<RectTransform>();
            _toolbar.SetParent(_overlay, false);
            var colors = new[] { new Color32(210, 100, 112, 255), new Color32(235, 194, 98, 255), new Color32(102, 194, 157, 255) };
            var inks = new[] { new Color32(57, 24, 31, 255), (Color32)FruitLabStyle.Background, new Color32(15, 54, 42, 255) };
            var marks = new[] { CardAnnotation.Excluded, CardAnnotation.None, CardAnnotation.Candidate };
            var names = new[] { "Red", "Yellow", "Green" };
            for (int i = 0; i < 3; i++)
            {
                Color color = colors[i];
                var item = new GameObject(names[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                item.transform.SetParent(_toolbar, false);
                var depth = FruitLabStyle.AddSurface(item, Color.Lerp(color, FruitLabStyle.Background, .38f)); depth.raycastTarget = true;
                var face = FruitLabStyle.Detail(item.transform, "Face", color);
                face.rectTransform.anchorMin = Vector2.zero; face.rectTransform.anchorMax = Vector2.one;
                face.rectTransform.offsetMin = new Vector2(1, 3.5f); face.rectTransform.offsetMax = new Vector2(-1, -1);
                var edge = FruitLabStyle.Detail(face.transform, "Edge", new Color(1, 1, 1, .19f), true);
                FruitLabStyle.Stretch(edge.rectTransform, .5f);
                var light = FruitLabStyle.Detail(face.transform, "TopLight", new Color(1, 1, 1, .16f));
                light.rectTransform.anchorMin = new Vector2(.12f, 1); light.rectTransform.anchorMax = new Vector2(.88f, 1);
                light.rectTransform.sizeDelta = new Vector2(0, 1); light.rectTransform.anchoredPosition = new Vector2(0, -1.5f);
                var button = _buttons[i] = item.GetComponent<Button>(); button.targetGraphic = face;
                var states = button.colors;
                states.normalColor = states.selectedColor = Color.white;
                states.highlightedColor = new Color(.97f, .99f, 1);
                states.pressedColor = new Color(.76f, .82f, .84f);
                states.disabledColor = new Color(.6f, .6f, .6f, .7f); states.fadeDuration = .08f; button.colors = states;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var mark = marks[i]; button.onClick.AddListener(() => _apply(mark));
                if (i == 1) continue;
                var symbol = FruitLabGlyph.Create(face.transform, i == 0 ? FruitLabGlyph.Symbol.Cross : FruitLabGlyph.Symbol.Check, inks[i]);
                symbol.rectTransform.anchorMin = symbol.rectTransform.anchorMax = Vector2.one * .5f;
                symbol.rectTransform.sizeDelta = Vector2.one * 25; symbol.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        public void Dispose()
        {
            Hide();
            if (_layout != null) _layout.LayoutChanged -= RefreshLayout;
            if (_overlay == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_overlay.gameObject);
            else UnityEngine.Object.DestroyImmediate(_overlay.gameObject);
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public sealed class PositionAssistanceView : IPositionAssistanceView
    {
        private readonly IInitialCardAreaPresentation _cards;
        private readonly GameUIView _game;
        private readonly FruitLabGameLayout _layout;
        private readonly ResultAreaView _history;
        private readonly IGameUIController _ui;
        private readonly IHapticController _haptics;
        private readonly Func<Vector2, Camera, bool> _indicatorHitTest;
        private RectTransform _reason;
        private TMP_Text _reasonText;
        private CanvasGroup _powerGroup;
        private float _previousAlpha, _reasonUntil;
        private bool _previousRaycasts, _previousInteractable;
        private bool? _enabled;
        private bool _disposed, _occludersDirty;
        private readonly List<Rect> _occluders = new List<Rect>();
        private readonly Vector3[] _corners = new Vector3[4];
        public bool IsReasonVisible => _reason != null && _reason.gameObject.activeSelf;
        public string ReasonText => _reasonText != null ? _reasonText.text : "";

        public PositionAssistanceView(IInitialCardAreaPresentation cards, GameUIView game, ResultAreaView history,
            IGameUIController ui, IHapticController haptics)
        {
            _cards = cards ?? throw new ArgumentNullException(nameof(cards)); _game = game; _history = history; _ui = ui; _haptics = haptics;
            _layout = game != null ? game.GetComponentInParent<FruitLabGameLayout>() : null;
            _indicatorHitTest = HitTestIndicators;
            if (_game != null && _game.GetPairView() != null) _game.GetPairView().AdditionalControlHitTest = _indicatorHitTest;
            if (_layout != null) _layout.LayoutChanged += OnLayoutChanged;
            _ui.CheckFinalNumbers += CloseFromUI; _ui.ResetNumbers += CloseFromUI; _ui.OpenSettings += CloseFromUI;
        }
        private void OnLayoutChanged() { PositionReason(); _occludersDirty = true; }
        private void UpdateOccluders()
        {
            _occludersDirty = false;
            var graphic = _game != null ? _game.GetPairView()?.Graphic : null;
            if (graphic == null) return;
            _occluders.Clear();
            for (int i = 0; _enabled == true && _cards != null && i < _cards.GetCardCount(); i++)
            {
                var holder = _cards.GetHolderView(i);
                if (holder == null || !holder.IndicatorStrip.gameObject.activeInHierarchy) continue;
                holder.IndicatorStrip.GetWorldCorners(_corners);
                Vector2 min = graphic.rectTransform.InverseTransformPoint(_corners[0]), max = graphic.rectTransform.InverseTransformPoint(_corners[2]);
                _occluders.Add(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
            }
            graphic.SetControlOccluders(_occluders);
        }

        private bool HitTestIndicators(Vector2 screen, Camera camera)
        {
            if (_disposed || _enabled != true || _cards == null) return false;
            for (int i = 0; i < _cards.GetCardCount(); i++)
            {
                var holder = _cards.GetHolderView(i);
                if (holder != null && holder.IndicatorStrip.gameObject.activeInHierarchy
                    && RectTransformUtility.RectangleContainsScreenPoint(holder.IndicatorStrip, screen, camera)) return true;
            }
            return false;
        }

        public void SetEnabled(bool enabled)
        {
            if (_enabled == enabled) return;
            _enabled = enabled;
            if (!enabled) CloseReason();
            _cards?.SetPositionIndicators(enabled); _game?.SetPositionIndicators(enabled);
            _cards?.RefreshLayout();
        }
        public void Render(PositionDeductionService state, Action<PositionDeductionReason> explain)
        {
            if (_cards == null) return;
            for (int fruit = 1; fruit <= _cards.GetCardCount(); fruit++)
            {
                int id = fruit;
                var holder = _cards.GetHolderView(fruit - 1);
                if (holder == null) continue;
                holder.ConfigureIndicators(state?.SlotCount ?? 0, slot =>
                {
                    var reason = state != null && slot < state.SlotCount ? state.Reason(id, slot) : null;
                    if (reason != null) explain?.Invoke(reason);
                });
                if (state != null)
                    for (int slot = 0; slot < state.SlotCount; slot++)
                        holder.Indicators[slot].SetDeduction(!state.IsPossible(fruit, slot), state.FixedSlot(fruit) == slot);
                _cards.SetCardPresence(fruit - 1, state?.Presence(fruit) ?? FruitPresence.Unknown);
            }
            UpdateOccluders();
        }
        public void Explain(PositionDeductionReason reason)
        {
            if (_layout == null || reason == null) return;
            CloseReason(); EnsureReason();
            _layout.SetEvidenceReasonMode(true);
            _powerGroup = _layout.PowerUpView.GetComponent<CanvasGroup>();
            if (_powerGroup == null) _powerGroup = _layout.PowerUpView.gameObject.AddComponent<CanvasGroup>();
            _previousAlpha = _powerGroup.alpha; _previousInteractable = _powerGroup.interactable; _previousRaycasts = _powerGroup.blocksRaycasts;
            _powerGroup.alpha = 0; _powerGroup.interactable = _powerGroup.blocksRaycasts = false;
            _reasonText.text = Describe(reason); _reason.gameObject.SetActive(true); _reason.SetAsLastSibling();
            _reasonUntil = Time.unscaledTime + 7; PositionReason(); _history?.FocusEvidence(reason.Guesses);
            _haptics?.Vibrate(HapticType.ButtonClick);
        }
        public void CloseReason()
        {
            if (_reason != null) _reason.gameObject.SetActive(false);
            if (_powerGroup != null)
            {
                _powerGroup.alpha = _previousAlpha; _powerGroup.interactable = _previousInteractable; _powerGroup.blocksRaycasts = _previousRaycasts;
                _powerGroup = null;
            }
            _reasonUntil = 0; if (_history != null) _history.ClearEvidence(); if (_layout != null && !_disposed) _layout.SetEvidenceReasonMode(false);
        }
        public void Tick()
        {
            if (_occludersDirty) UpdateOccluders();
            if (IsReasonVisible && Time.unscaledTime >= _reasonUntil) CloseReason();
        }
        private void CloseFromUI(object sender, EventArgs args) => CloseReason();
        private void EnsureReason()
        {
            if (_reason != null) return;
            _reason = new GameObject("PositionReason", typeof(RectTransform)).GetComponent<RectTransform>();
            _reason.SetParent(_layout.transform, false); FruitLabStyle.AddSurface(_reason.gameObject, FruitLabStyle.Panel).raycastTarget = false;
            FruitLabStyle.Rim(_reason); _reasonText = FruitLabStyle.CreateText("Explanation", _reason, 13, FruitLabStyle.Text);
            _reasonText.raycastTarget = false; _reasonText.enableAutoSizing = true; _reasonText.fontSizeMin = 10; _reasonText.fontSizeMax = 13;
            _reasonText.alignment = TextAlignmentOptions.MidlineLeft;
            var close = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            close.transform.SetParent(_reason, false); close.GetComponent<Image>().color = Color.clear;
            close.GetComponent<Button>().onClick.AddListener(CloseReason);
            var glyph = FruitLabGlyph.Create(close.transform, FruitLabGlyph.Symbol.Cross, FruitLabStyle.Text);
            FruitLabStyle.Stretch(glyph.rectTransform, 6); glyph.raycastTarget = false;
        }
        private void PositionReason()
        {
            if (!IsReasonVisible || _layout == null) return;
            _layout.PowerUpArea.ForceUpdateRectTransforms(); _layout.PowerUpArea.GetWorldCorners(_corners);
            Vector2 min = _layout.transform.InverseTransformPoint(_corners[0]), max = _layout.transform.InverseTransformPoint(_corners[2]);
            var root = (RectTransform)_layout.transform;
            FruitLabStyle.Place(_reason, root, min.x - root.rect.xMin + 3, min.y - root.rect.yMin, max.x - min.x - 6, max.y - min.y);
            FruitLabStyle.Place(_reasonText.rectTransform, _reason, 9, 2, _reason.rect.width - 42, _reason.rect.height - 4);
            FruitLabStyle.Place((RectTransform)_reason.Find("Close"), _reason, _reason.rect.width - 30, (_reason.rect.height - 28) / 2, 28, 28);
        }
        public static string Describe(PositionDeductionReason reason)
        {
            int number = reason.Guesses.Count == 1 ? reason.Guesses[0] : 0;
            string prefix = number > 0 ? number + ". tahminde " : "Tahminlere göre ";
            string message;
            switch (reason.Kind)
            {
                case PositionDeductionKind.NoCorrect: message = prefix + "doğru yerde meyve yoktu."; break;
                case PositionDeductionKind.Manual: message = "Bu konumu sen işaretledin."; break;
                case PositionDeductionKind.NoMisplaced: message = prefix + "yanlış yer yoktu; bu meyve başka alanda olamaz."; break;
                case PositionDeductionKind.NoMatches: message = prefix + "hiçbir meyve cevapta yoktu."; break;
                case PositionDeductionKind.CompleteSet: message = prefix + "cevaptaki tüm meyveler bulundu; diğerleri elendi."; break;
                case PositionDeductionKind.OnlyPosition: message = "Bu meyve için yalnızca " + (reason.Slot + 1) + ". alan kaldı."; break;
                case PositionDeductionKind.OnlyCandidate: message = (reason.Slot + 1) + ". alanda başka aday kalmadı."; break;
                case PositionDeductionKind.Occupied: message = (reason.Slot + 1) + ". alan başka bir meyve için kesinleşti."; break;
                case PositionDeductionKind.FixedElsewhere: message = "Bu meyvenin yeri " + (reason.Slot + 1) + ". alan olarak kesinleşti."; break;
                case PositionDeductionKind.RevealedPosition: message = (reason.Slot + 1) + ". alandaki meyve bir ipucuyla açıldı."; break;
                case PositionDeductionKind.EliminatedFruit: message = "Bu meyve bir kristal ipucuyla elendi."; break;
                case PositionDeductionKind.DetailedCorrect: message = prefix + "ayrıntılı ipucu bu konumu doğruladı."; break;
                case PositionDeductionKind.DetailedMisplaced: message = prefix + "bu meyvenin yanlış yerde olduğu gösterildi."; break;
                case PositionDeductionKind.DetailedAbsent: message = prefix + "bu meyvenin cevapta olmadığı gösterildi."; break;
                default: message = "Bu meyvenin bütün alanları elendi."; break;
            }
            if (number == 0 && reason.Guesses.Count > 0)
            {
                string sources = string.Join(", ", reason.Guesses.Take(3).Select(value => value + "."));
                message += "\nDayanak: " + sources + (reason.Guesses.Count > 3 ? " ve " + (reason.Guesses.Count - 3) + " diğer tahmin." : " tahmin.");
            }
            return message;
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            if (_game != null && _game.GetPairView() != null && _game.GetPairView().AdditionalControlHitTest == _indicatorHitTest)
            {
                _game.GetPairView().AdditionalControlHitTest = null;
                if (_game.GetPairView().Graphic != null) _game.GetPairView().Graphic.SetControlOccluders(null);
            }
            _ui.CheckFinalNumbers -= CloseFromUI; _ui.ResetNumbers -= CloseFromUI; _ui.OpenSettings -= CloseFromUI;
            if (_layout != null) _layout.LayoutChanged -= OnLayoutChanged;
            CloseReason();
            if (_reason != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_reason.gameObject); else UnityEngine.Object.DestroyImmediate(_reason.gameObject);
            }
        }
    }
}

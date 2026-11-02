using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class InitialHolderView : MonoBehaviour, IInitialHolderView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform possibleHolderIndicatorHolderTransform;
        [SerializeField] private PossibleHolderIndicatorView possibleHolderIndicatorPrefab;
        [SerializeField] private TMP_Text text;
        private Image _fruitGhost;
        private readonly List<PossibleHolderIndicatorView> _indicators = new List<PossibleHolderIndicatorView>();
        public const float IndicatorHeight = 15f;
        public const float IndicatorReserve = IndicatorHeight + 3f;
        public IReadOnlyList<PossibleHolderIndicatorView> Indicators => _indicators;
        public RectTransform IndicatorStrip => possibleHolderIndicatorHolderTransform;
        private bool _indicatorsVisible;
        private int _indicatorCount;
        private Action<int> _indicatorPressed;
        private Color _indicatorColor = FruitLabStyle.Text;
        public void SetIndicatorColor(Color fruitColor)
        {
            // Keep the fruit hue while lifting small digits above the dark background.
            _indicatorColor = Color.Lerp(fruitColor, FruitLabStyle.Text, .35f);
            _indicatorColor.a = 1;
            foreach (var indicator in _indicators) indicator.SetNumberColor(_indicatorColor);
        }

        private void Awake() => ApplyLabStyle();

        public void ApplyLabStyle()
        {
            Transform surface = transform.Find("Image");
            if (surface != null) FruitLabStyle.RecessSurface(surface.GetComponent<Image>());
            if (text != null) text.gameObject.SetActive(false);
        }
        
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
            LayoutIndicators();
        }
        
        public void ConfigureIndicators(int count, Action<int> clicked)
        {
            _indicatorCount = count; _indicatorsVisible = count > 0; _indicatorPressed = clicked;
            while (_indicators.Count < count)
            {
                var indicator = (PossibleHolderIndicatorView)CreatePossibleHolderIndicatorView();
                int index = _indicators.Count;
                indicator.Configure(index + 1, () => _indicatorPressed?.Invoke(index));
                indicator.SetNumberColor(_indicatorColor);
                _indicators.Add(indicator);
            }
            possibleHolderIndicatorHolderTransform.gameObject.SetActive(_indicatorsVisible);
            for (int i = 0; i < _indicators.Count; i++) _indicators[i].gameObject.SetActive(i < count);
            LayoutIndicators();
        }
        private void LayoutIndicators()
        {
            if (!_indicatorsVisible || _indicatorCount == 0) return;
            float width = rectTransform.rect.width, gap = .8f;
            float cellWidth = (width - gap * (_indicatorCount - 1)) / _indicatorCount;
            var holder = possibleHolderIndicatorHolderTransform;
            holder.anchorMin = holder.anchorMax = new Vector2(.5f, 0); holder.pivot = new Vector2(.5f, 1);
            holder.anchoredPosition = new Vector2(0, -2); holder.sizeDelta = new Vector2(width, IndicatorHeight);
            for (int i = 0; i < _indicatorCount; i++)
            {
                var indicator = _indicators[i];
                FruitLabStyle.Place(indicator.GetRectTransform(), holder, i * (cellWidth + gap), 0, cellWidth, IndicatorHeight);
                indicator.SetSize(new Vector2(cellWidth, IndicatorHeight));
            }
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
        
        public void SetFruit(Sprite fruit)
        {
            if (text != null) text.gameObject.SetActive(false);
            _fruitGhost = FruitLabStyle.Detail(transform, "FruitGhost", new Color(1f, 1f, 1f, .44f));
            _fruitGhost.sprite = fruit;
            _fruitGhost.type = Image.Type.Simple;
            _fruitGhost.preserveAspect = true;
            _fruitGhost.material = Resources.Load<Material>("Art/FruitLabGhost");
            _fruitGhost.rectTransform.anchorMin = new Vector2(.13f, .13f);
            _fruitGhost.rectTransform.anchorMax = new Vector2(.87f, .87f);
            _fruitGhost.rectTransform.offsetMin = _fruitGhost.rectTransform.offsetMax = Vector2.zero;
            // Keep the stamp behind the movable card, including after reapplying a style.
            _fruitGhost.transform.SetSiblingIndex(1);
        }
        
        public IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView()
        {
            return Instantiate(possibleHolderIndicatorPrefab, possibleHolderIndicatorHolderTransform);
        }

        public RectTransform GetInitialHolderRectTransform()
        {
            return rectTransform;
        }

        public Vector2 GetSizeOfHolderIndicatorPrefab()
        {
            return possibleHolderIndicatorPrefab.GetRectTransform().sizeDelta;
        }
        
    }

    public interface IInitialHolderView : IBaseHolderView
    {
        void SetFruit(Sprite fruit);
        void SetIndicatorColor(Color fruitColor);
        IPossibleHolderIndicatorView CreatePossibleHolderIndicatorView();
        RectTransform GetInitialHolderRectTransform();
        Vector2 GetSizeOfHolderIndicatorPrefab();
    }
}

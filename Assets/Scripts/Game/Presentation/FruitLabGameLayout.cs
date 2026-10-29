using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    [DefaultExecutionOrder(-11000)]
    public class FruitLabGameLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform middleArea;
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private RectTransform initialArea;
        [SerializeField] private RectTransform actionArea;
        [SerializeField] private RectTransform powerUpArea;
        [SerializeField] private RectTransform historyArea;
        [SerializeField] private GameUIView gameView;
        [SerializeField] private PowerUpListHolderView powerUpView;
        [SerializeField] private LifeBarView lifeBar;
        [SerializeField] private ResultAreaView results;

        private Vector2 _lastSize;
        public event Action LayoutChanged;

        private void Awake()
        {
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(375f, 667f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
            }

            // Run before Zenject measures the board. CrystalFramework's SafeArea
            // component subsequently maintains the same device-safe anchors.
            if (Screen.width > 0 && Screen.height > 0)
            {
                Rect area = Screen.safeArea;
                safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            }

            FruitLabStyle.Font = gameView.GetUserText().font;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Background);
            Canvas.ForceUpdateCanvases();
            ApplyLayout();
        }

        private void Start() => ApplyLayout();

        private void LateUpdate()
        {
            if ((safeArea.rect.size - _lastSize).sqrMagnitude < 0.01f) return;
            ApplyLayout();
            LayoutChanged?.Invoke();
        }

        public void ApplyLayout()
        {
            PrepareSceneVisuals();
            safeArea.ForceUpdateRectTransforms();
            _lastSize = safeArea.rect.size;
            FruitLabLayoutMetrics layout = new FruitLabLayoutMetrics(_lastSize.x, _lastSize.y);
            float width = layout.Width - layout.Padding * 2f;

            FruitLabStyle.Stretch(middleArea);
            FruitLabStyle.AddSurface(middleArea.gameObject, FruitLabStyle.Background);
            FruitLabStyle.Atmosphere(middleArea);
            FruitLabStyle.Place(header, safeArea, layout.Padding,
                layout.Height - layout.TopPadding - layout.HeaderHeight, width, layout.HeaderHeight);
            Image oldHeader = header.GetComponent<Image>();
            if (oldHeader != null) oldHeader.enabled = false;

            FruitLabStyle.Place(gameArea, middleArea, layout.Padding, layout.BottomPadding, width, layout.GameAreaHeight);
            FruitLabStyle.AddSurface(gameArea.gameObject, FruitLabStyle.Panel);
            FruitLabStyle.Rim(gameArea);
            float y = 0f;
            FruitLabStyle.Place(powerUpArea, gameArea, 0f, y, width, layout.PowerUpHeight);
            y += layout.PowerUpHeight + layout.Gap;
            FruitLabStyle.Place(initialArea, gameArea, layout.ActionWidth + layout.Gap, y, layout.ChoicesWidth, layout.ChoicesHeight);
            FruitLabStyle.Place(actionArea, gameArea, 0f, y, width, layout.ChoicesHeight);
            y += layout.ChoicesHeight + layout.Gap;
            FruitLabStyle.Place(boardArea, gameArea, 0f, y, width, layout.BoardHeight);
            FruitLabStyle.AddSurface(boardArea.gameObject, FruitLabStyle.Tile);
            FruitLabStyle.Rim(boardArea);

            FruitLabStyle.Place(historyArea, middleArea, layout.Padding,
                layout.BottomPadding + layout.GameAreaHeight + layout.Gap, width, layout.HistoryHeight);
            FruitLabStyle.AddSurface(historyArea.gameObject, FruitLabStyle.Panel);
            FruitLabStyle.Rim(historyArea, .25f);

            gameView.ApplyLabLayout(header, actionArea, width, layout.HeaderHeight, layout.ActionWidth, layout.ChoicesHeight);
            powerUpView.ApplyLabLayout(width, layout.PowerUpHeight);
            lifeBar.ApplyLabLayout(header, width, layout.HeaderHeight);
            results.ApplyLabLayout(layout.HistorySpacing);
        }

        private void PrepareSceneVisuals()
        {
            FruitLabStyle.Font = gameView.GetUserText().font;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Background);
            // MiddleArea is a full-screen sibling. Its background must draw before the header.
            int first = Mathf.Min(middleArea.GetSiblingIndex(), header.GetSiblingIndex());
            middleArea.SetSiblingIndex(first);
            header.SetSiblingIndex(first + 1);
            HideDecoration(historyArea, "Background");
            HideDecoration(initialArea, "Background");
            HideDecoration(header, "Border");
        }

        private static void HideDecoration(Transform parent, string name)
        {
            Transform decoration = parent.Find(name);
            if (decoration != null) decoration.gameObject.SetActive(false);
        }
    }
}

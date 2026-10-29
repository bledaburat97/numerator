using Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    [DefaultExecutionOrder(-11000)]
    public class FruitLabMenuLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform middleArea;
        [SerializeField] private MenuHeaderView header;
        [SerializeField] private MenuUIView menu;
        [SerializeField] private PowerUpListHolderView powers;
        [SerializeField] private VerticalCrystalProgressView crystals;
        private Vector2 _lastSize;

        private void Awake()
        {
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(375f, 667f);
            scaler.matchWidthOrHeight = 0f;
            if (Screen.width > 0 && Screen.height > 0)
            {
                Rect area = Screen.safeArea;
                safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            }
            Canvas.ForceUpdateCanvases();
            ApplyLayout();
        }

        private void LateUpdate()
        {
            if ((safeArea.rect.size - _lastSize).sqrMagnitude > .01f) ApplyLayout();
        }

        public void ApplyLayout()
        {
            _lastSize = safeArea.rect.size;
            float width = _lastSize.x - 24f, height = _lastSize.y;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Background);
            FruitLabStyle.Stretch(middleArea);
            FruitLabStyle.AddSurface(middleArea.gameObject, FruitLabStyle.Background);
            FruitLabStyle.Atmosphere(middleArea);
            int first = Mathf.Min(middleArea.GetSiblingIndex(), header.transform.GetSiblingIndex());
            middleArea.SetSiblingIndex(first);
            header.transform.SetSiblingIndex(first + 1);
            FruitLabStyle.Place((RectTransform)header.transform, safeArea, 12f, height - 68f, width, 60f);
            header.ApplyLabLayout(width);
            FruitLabStyle.Place(crystals.GetRectTransform(), middleArea, 12f, height - 184f, width, 106f);
            crystals.ApplyLabStyle();
            FruitLabStyle.Place(powers.GetRectTransform(), middleArea, 12f, height - 240f, width, 44f);
            powers.ApplyLabLayout(width, 44f);
            RectTransform actions = (RectTransform)menu.transform;
            FruitLabStyle.Place(actions, middleArea, 24f, Mathf.Max(28f, (height - 352f) * .4f), width - 24f, 112f);
            BaseButtonView play = (BaseButtonView)menu.GetSinglePlayerButton();
            BaseButtonView multi = (BaseButtonView)menu.GetMultiplayerButton();
            FruitLabStyle.Place(play.GetButtonRectTransform(), actions, 0f, 56f, width - 24f, 56f);
            FruitLabStyle.Place(multi.GetButtonRectTransform(), actions, 0f, 0f, width - 24f, 44f);
            play.ApplyLabActionStyle(true);
            multi.ApplyLabActionStyle(false);
        }
    }
}

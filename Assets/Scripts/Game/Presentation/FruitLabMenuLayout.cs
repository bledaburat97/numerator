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
        private RectTransform _homeContent;
        public RectTransform HomeContent { get { EnsureHomeContent(); return _homeContent; } }
        public void SetHomeVisible(bool visible) { EnsureHomeContent(); _homeContent.gameObject.SetActive(visible); }
        private void EnsureHomeContent()
        {
            if (_homeContent == null) _homeContent = middleArea.Find("HomeContent") as RectTransform;
            if (_homeContent == null) _homeContent = FruitLabOnlineLayout.Rect("HomeContent", middleArea);
            FruitLabStyle.Stretch(_homeContent);
            crystals.transform.SetParent(_homeContent, false); powers.transform.SetParent(_homeContent, false); menu.transform.SetParent(_homeContent, false);
        }

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
            EnsureHomeContent();
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
            FruitLabStyle.Place(crystals.GetRectTransform(), _homeContent, 12f, height - 184f, width, 106f);
            crystals.ApplyLabStyle();
            FruitLabStyle.Place(powers.GetRectTransform(), _homeContent, 12f, height - 316f, width, 120f);
            powers.ApplyLabLayout(width, 120f, 2);
            RectTransform actions = (RectTransform)menu.transform;
            FruitLabStyle.Place(actions, _homeContent, 24f, Mathf.Max(MainNavigationView.BarHeight + 24, (height - 430) * .4f), width - 24f, 64f);
            BaseButtonView play = (BaseButtonView)menu.GetSinglePlayerButton();
            FruitLabStyle.Place(play.GetButtonRectTransform(), actions, 0, 0, width - 24f, 64);
            foreach (var old in new[] { menu.GetMultiplayerButton(), menu.GetCollectionButton(), menu.GetAchievementsButton() })
                if (old != null) ((Component)old).gameObject.SetActive(false);
            var privacy = actions.Find("PrivacyButton"); if (privacy != null) privacy.gameObject.SetActive(false);
            play.ApplyLabActionStyle(true);
            var goal=ClosestBadgeView.Ensure(_homeContent);
            FruitLabStyle.Place((RectTransform)goal.transform,_homeContent,36f,actions.anchoredPosition.y+44f,width-48f,60f);
            goal.ApplyLayout();

        }
    }
}

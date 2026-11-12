using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // Menu-only surfaces. Puzzle themes and their HUD retain their own styling.
    public static class MenuChrome
    {
        public static readonly Color Sky = new Color32(148, 211, 236, 255);
        public static readonly Color Counter = new Color32(222, 242, 250, 255);
        public static readonly Color Ink = new Color32(37, 76, 91, 255);
        public static Color HeaderColor(MainTab tab) => tab==MainTab.Garden ? new Color32(167,202,157,255) : Sky;
        public static Color CounterColor(MainTab tab) => tab==MainTab.Garden ? new Color32(230,241,221,255) : Counter;
        public const float HeaderHeight = 56;
        public const float SettingsSize = 44;

        public static Vector2 SafeTopAndSides(Canvas canvas)
        {
            if (!Application.isPlaying || canvas.renderMode == RenderMode.WorldSpace || Screen.height <= 0 || Screen.width <= 0)
                return Vector2.zero;
            var size = ((RectTransform)canvas.transform).rect.size;
            return new Vector2(Screen.safeArea.xMin / Screen.width * size.x,
                (Screen.height - Screen.safeArea.yMax) / Screen.height * size.y);
        }

        public static void ApplyBackdrop(Canvas canvas,MainTab tab=MainTab.Home)
        {
            var root = (RectTransform)canvas.transform;
            var surface = Scripts.FruitLabStyle.AddSurface(canvas.gameObject, Sky);
            surface.sprite = null; surface.type = Image.Type.Simple;
            var art = Scripts.FruitLabStyle.Detail(root, "OrchardBackdrop", Color.white);
            art.sprite = Resources.Load<Sprite>(tab==MainTab.Collection?"Art/Menu/BadgeGarden":"Art/Menu/OrchardHome"); art.type = Image.Type.Simple;
            art.preserveAspect = false; art.raycastTarget = false;
            if (art.sprite == null) return;
            var source = art.sprite.rect.size;
            float scale = Mathf.Max(root.rect.width / source.x, root.rect.height / source.y);
            var rect = art.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = source * scale; rect.localScale = Vector3.one;
            art.transform.SetAsFirstSibling();
        }

        public static Sprite TabIcon(MainTab tab) => tab == MainTab.Collection
            ? Scripts.FruitLabStyle.Icon("Badge") : Resources.Load<Sprite>("Art/Menu/Tab" + tab);
    }
}

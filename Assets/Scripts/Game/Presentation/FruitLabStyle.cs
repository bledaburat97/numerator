using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public static class FruitLabStyle
    {
        public static readonly Color Background = Hex("13272F");
        public static readonly Color Panel = Hex("1D3841");
        public static readonly Color Tile = Hex("2C4A52");
        public static readonly Color Recess = Hex("132C34");
        public static readonly Color Border = Hex("5C7B7A");
        public static readonly Color Text = Hex("F5F3E8");
        public static readonly Color Muted = Hex("9AB5B4");
        public static readonly Color Mint = Hex("87E6C6");
        public static readonly Color Gold = Hex("F3BB4E");
        public static readonly Color Pink = Hex("EF6CA3");
        public static readonly Color Correct = Hex("5BFFA0");
        public static readonly Color Misplaced = Hex("FFD35D");
        public static readonly Color LocalPlayer = Hex("8DD5FF");
        public static readonly Color OpponentPlayer = Hex("EF7178");
        public static TMP_FontAsset Font;

        private static Sprite _rounded, _frame, _inset, _resultFrame;
        private static Dictionary<string, Sprite> _icons;
        public static Sprite Rounded => _rounded != null ? _rounded : (_rounded = LoadSurface("FruitLabSatinSurface", false));
        public static Sprite Frame => _frame != null ? _frame : (_frame = LoadSurface("FruitLabFrame", true));
        public static Sprite ResultFrame => _resultFrame != null ? _resultFrame : (_resultFrame = LoadSurface("FruitLabResultFrame", true));
        public static Sprite Inset => _inset != null ? _inset : (_inset = LoadSurface("FruitLabInsetSurface", false));

        public static void ResetCachedAssets() { _icons = null; _rounded = _frame = _inset = _resultFrame = null; }

        private static Sprite LoadSurface(string name, bool frame)
        {
            Sprite asset = Resources.Load<Sprite>("Art/" + name);
            return asset != null ? asset : CreateRounded(frame);
        }

        public static Sprite Icon(string name)
        {
            if (_icons == null)
            {
                _icons = new Dictionary<string, Sprite>();
                Sprite[] sprites = Resources.LoadAll<Sprite>("Art/FruitLabIconsV2");
                if (sprites.Length == 0) sprites = Resources.LoadAll<Sprite>("Art/FruitLabIcons");
                foreach (Sprite sprite in sprites)
                    _icons[sprite.name] = sprite;
            }
            return _icons.TryGetValue(name, out Sprite icon) ? icon : null;
        }

        public static void Surface(Image image, Color color, bool frame = false)
        {
            if (image == null) return;
            image.sprite = frame ? Frame : Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.5f;
            image.color = color;
        }

        public static Image AddSurface(GameObject target, Color color)
        {
            Image image = target.GetComponent<Image>();
            if (image == null) image = target.AddComponent<Image>();
            Surface(image, color);
            image.raycastTarget = false;
            return image;
        }

        public static void RecessSurface(Image image)
        {
            Surface(image, Recess);
            if (image != null) image.sprite = Inset;
        }

        public static Image Detail(Transform parent, string name, Color tint, bool frame = false)
        {
            Image detail = parent.Find(name)?.GetComponent<Image>();
            if (detail == null)
            {
                GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                item.transform.SetParent(parent, false); detail = item.GetComponent<Image>();
            }
            Surface(detail, tint, frame); detail.raycastTarget = false;
            return detail;
        }

        public static void Rim(RectTransform target, float opacity = .45f)
        {
            Image rim = Detail(target, "LabRim", new Color(Border.r, Border.g, Border.b, opacity), true);
            Stretch(rim.rectTransform, 1f); rim.transform.SetAsFirstSibling();
        }

        public static void Atmosphere(RectTransform area)
        {
            Sprite room = Resources.Load<Sprite>("Art/FruitLabRoomV1");
            Image art = null;
            if (room != null)
            {
                art = Detail(area, "LabRoom", Color.white);
                art.sprite = room; art.type = Image.Type.Simple; art.preserveAspect = false;
                Stretch(art.rectTransform); art.transform.SetAsFirstSibling();
            }
            FruitLabBackdrop backdrop = area.Find("LabBackdrop")?.GetComponent<FruitLabBackdrop>();
            if (backdrop == null)
            {
                GameObject item = new GameObject("LabBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(FruitLabBackdrop));
                item.transform.SetParent(area, false); backdrop = item.GetComponent<FruitLabBackdrop>();
            }
            backdrop.raycastTarget = false;
            backdrop.UseRoomArt = room != null;
            Stretch(backdrop.rectTransform);
            if (art != null) backdrop.transform.SetSiblingIndex(1); else backdrop.transform.SetAsFirstSibling();
            backdrop.SetVerticesDirty();
        }

        public static void SetIcon(Image image, string name)
        {
            if (image == null) return;
            Sprite icon = Icon(name);
            if (icon != null) image.sprite = icon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        public static void Place(RectTransform rect, Transform parent, float x, float y, float width, float height)
        {
            if (rect.parent != parent) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = new Vector2(x + width * 0.5f, y + height * 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
            rect.localScale = Vector3.one;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, float size, Color color)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
            text.font = Font != null ? Font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Midline;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        private static Sprite CreateRounded(bool frame)
        {
            // A shared 64px UI primitive replaces large generated button/frame images.
            const int size = 64;
            const float radius = 12f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius);
                float qy = Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius);
                float distance = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                                 + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float alpha = 1f - Mathf.Clamp01(distance + 0.5f);
                if (frame) alpha -= 1f - Mathf.Clamp01(distance + 2.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            }
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = frame ? "FruitLabFrame" : "FruitLabSurface",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            Sprite result = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f,
                100f, 0, SpriteMeshType.FullRect, new Vector4(14f, 14f, 14f, 14f));
            result.hideFlags = HideFlags.HideAndDontSave;
            return result;
        }
    }
}

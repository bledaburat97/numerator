using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    internal static class FriendUi
    {
        public static void Section(RectTransform root)
        {
            var image=FruitLabStyle.AddSurface(root.gameObject,Color.white);
            image.sprite=Resources.Load<Sprite>("Art/Multiplayer/FriendPanel");
            image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=8f;image.raycastTarget=false;
            root.Find("LabRim")?.gameObject.SetActive(false);
        }
        public static TMP_Text Text(Transform parent, string name, string value, float size = 18)
        {
            var text = parent.Find(name)?.GetComponent<TMP_Text>();
            if (text == null) text = FruitLabStyle.CreateText(name, parent, size, FruitLabStyle.Text);
            FruitLabStyle.TextStyle(text, size); text.richText = false; text.text = value;
            text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
            return text;
        }
        public static Button Button(Transform parent, string name, string label, Action action, bool primary = false)
        {
            var root = parent.Find(name) as RectTransform;
            if (root == null) root = FruitLabOnlineLayout.Rect(name, parent);
            var image = FruitLabStyle.AddSurface(root.gameObject, primary ? FruitLabStyle.Mint : FruitLabStyle.Tile); image.raycastTarget = true;image.enabled=true;
            var button = root.GetComponent<Button>(); if (button == null) button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.onClick.RemoveAllListeners(); if (action != null) button.onClick.AddListener(action.Invoke);
            var text = Text(root, "Label", label); text.color = primary ? FruitLabStyle.Background : FruitLabStyle.Text;
            FruitLabStyle.Stretch(text.rectTransform, 4);
            return button;
        }
        public static RectTransform Overlay(Transform parent, string name)
        {
            var root = parent.Find(name) as RectTransform ?? FruitLabOnlineLayout.Rect(name, parent);
            FruitLabStyle.Stretch(root);
            var image = FruitLabStyle.AddSurface(root.gameObject, new Color(0, 0, 0, .75f)); image.raycastTarget = true;
            root.SetAsLastSibling(); return root;
        }
    }
}

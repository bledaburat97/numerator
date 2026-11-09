using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // The six fruit medallions reuse the game's artwork, surfaces and selection rim.
    public sealed class PlayerAvatarView : MonoBehaviour
    {
        private static readonly string[] Fruits = { "Apple", "Strawberry", "Pear", "Blueberry", "Orange", "Grape" };
        private static readonly string[] Names = { "Elma", "Çilek", "Armut", "Yaban mersini", "Portakal", "Üzüm" };
        public int Avatar { get; private set; }
        public static string AvatarName(int avatar) => Names[Mathf.Clamp(avatar, 0, Fruits.Length - 1)];
        public static PlayerAvatarView Ensure(Transform parent, string name = "Avatar")
        {
            var existing = parent.Find(name)?.GetComponent<PlayerAvatarView>();
            if (existing != null) return existing;
            var rect = FruitLabOnlineLayout.Rect(name, parent);
            return rect.gameObject.AddComponent<PlayerAvatarView>();
        }
        public void Render(int avatar, bool selected = false)
        {
            Avatar = Mathf.Clamp(avatar, 0, Fruits.Length - 1);
            FruitLabStyle.AddSurface(gameObject, selected ? Color.Lerp(FruitLabStyle.Tile, FruitLabStyle.Mint, .18f) : FruitLabStyle.Recess);
            var frame = FruitLabStyle.Detail(transform, "AvatarFrame", selected ? FruitLabStyle.Mint : FruitLabStyle.Border, true);
            FruitLabStyle.Stretch(frame.rectTransform, 1);
            var art = FruitLabStyle.Detail(transform, "Portrait", Color.white);
            art.sprite = FruitLabStyle.Icon(Fruits[Avatar]); art.type = Image.Type.Simple; art.preserveAspect = true;
            FruitLabStyle.Stretch(art.rectTransform, 9);
        }
    }
}

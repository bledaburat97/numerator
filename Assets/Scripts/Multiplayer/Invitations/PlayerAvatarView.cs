using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // Shared portraits keep the same player identity throughout invitations and matches.
    public sealed class PlayerAvatarView : MonoBehaviour
    {
        private static readonly string[] Names = { "Bahçıvan 1", "Bahçıvan 2", "Bahçıvan 3", "Bahçıvan 4" };
        public int Avatar { get; private set; }
        public static string AvatarName(int avatar) => Names[Mathf.Clamp(avatar, 0, PlayerIdentityStore.AvatarCount - 1)];
        public static PlayerAvatarView Ensure(Transform parent, string name = "Avatar")
        {
            var existing = parent.Find(name)?.GetComponent<PlayerAvatarView>();
            if (existing != null) return existing;
            var rect = FruitLabOnlineLayout.Rect(name, parent);
            return rect.gameObject.AddComponent<PlayerAvatarView>();
        }
        public void Render(int avatar, bool selected = false)
        {
            Avatar = Mathf.Clamp(avatar, 0, PlayerIdentityStore.AvatarCount - 1);
            FruitLabStyle.AddSurface(gameObject, selected ? Color.Lerp(FruitLabStyle.Tile, FruitLabStyle.Mint, .18f) : FruitLabStyle.Recess);
            var frame = FruitLabStyle.Detail(transform, "AvatarFrame", selected ? FruitLabStyle.Mint : FruitLabStyle.Border, true);
            FruitLabStyle.Stretch(frame.rectTransform, 1);
            var art = FruitLabStyle.Detail(transform, "Portrait", Color.white);
            art.sprite = Resources.Load<Sprite>("Art/Multiplayer/Avatars/Gardener"+(Avatar+1).ToString("00")); art.type = Image.Type.Simple; art.preserveAspect = true;
            FruitLabStyle.Stretch(art.rectTransform, 4);
        }
    }
}

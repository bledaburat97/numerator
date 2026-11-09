using UnityEngine;

namespace Scripts
{
    [CreateAssetMenu(menuName = "Game/Friend invitations")]
    public sealed class FriendInviteSettings : ScriptableObject
    {
        [Tooltip("HTTPS origin only. Leave empty until a domain is owned and configured.")]
        public string httpsOrigin = "";
        public string urlScheme = "fruitfriends";
        public string iosStoreUrl = "";
        public string androidStoreUrl = "";
        [Range(10, 120)] public int lifetimeMinutes = 30;
        public static FriendInviteSettings Load() => Resources.Load<FriendInviteSettings>("Config/FriendInvites");
    }
}

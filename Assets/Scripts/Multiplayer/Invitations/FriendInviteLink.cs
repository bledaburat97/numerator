using System;
using System.Security.Cryptography;
using System.Text;

namespace Scripts
{
    public sealed class FriendInvitation
    {
        public string Token { get; }
        public string Code => Token.Substring(0, 6);
        public FriendInvitation(string token)
        {
            if (!IsToken(token)) throw new ArgumentException("Invalid invitation");
            Token = token;
        }
        public static string NewToken(string code)
        {
            if (!PlayerLobby.IsValidRoomCode(code)) throw new ArgumentException("Invalid room code");
            var bytes = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return code + "-" + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
        public static bool IsToken(string value)
        {
            if (value == null || value.Length != 29 || value[6] != '-' || !PlayerLobby.IsValidRoomCode(value.Substring(0, 6))) return false;
            for (int i = 7; i < value.Length; i++)
                if (!(value[i] >= 'A' && value[i] <= 'Z') && !(value[i] >= 'a' && value[i] <= 'z')
                    && !(value[i] >= '0' && value[i] <= '9') && value[i] != '-' && value[i] != '_') return false;
            return true;
        }
        public static string Lookup(string token)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("invite-v1:" + token))).Replace("-", "");
        }
        public string Url(FriendInviteSettings settings)
        {
            string origin = settings != null ? (settings.httpsOrigin ?? "").TrimEnd('/') : "";
            if (IsOrigin(origin)) return origin + "/i/" + Token;
            string scheme = settings != null ? settings.urlScheme : "fruitfriends";
            return scheme + "://invite/" + Token;
        }
        public static bool IsOrigin(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == "https" && uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0
            && uri.UserInfo.Length == 0 && uri.IsDefaultPort;
        public static bool TryParse(string raw, FriendInviteSettings settings, out FriendInvitation invite)
        {
            invite = null;
            if (string.IsNullOrEmpty(raw) || raw.Length > 512 || !Uri.TryCreate(raw, UriKind.Absolute, out var uri)
                || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) return false;
            string scheme = settings != null ? settings.urlScheme : "fruitfriends";
            bool custom = uri.Scheme == scheme && uri.Host == "invite" && uri.IsDefaultPort;
            bool web = settings != null && IsOrigin(settings.httpsOrigin) && uri.Scheme == "https"
                && uri.Authority.Equals(new Uri(settings.httpsOrigin).Authority, StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath.StartsWith("/i/", StringComparison.Ordinal);
            if (!custom && !web) return false;
            string prefix = custom ? scheme + "://invite/" : settings.httpsOrigin.TrimEnd('/') + "/i/";
            if (!raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || raw.Length != prefix.Length + 29) return false;
            // Uri canonicalizes dot segments; validate the original path too.
            string token = raw.Substring(prefix.Length);
            if (!IsToken(token)) return false;
            invite = new FriendInvitation(token); return true;
        }
    }
    public sealed class FriendInvitePreview
    {
        public readonly FriendInvitation Invite;
        public readonly string HostName;
        public readonly int Avatar;
        public readonly Difficulty Difficulty;
        public FriendInvitePreview(FriendInvitation invite, string name, int avatar, Difficulty difficulty)
        { Invite = invite; HostName = PlayerIdentityStore.NormalizeName(name); if (HostName.Length == 0) HostName = "Arkadaşın"; Avatar = avatar; Difficulty = difficulty; }
    }
}

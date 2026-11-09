using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    [Serializable]
    public sealed class PlayerIdentity
    {
        public string id;
        public string name;
        public int avatar;
    }

    // Local identity is independent of a temporary Netcode client ID and the economy save.
    public static class PlayerIdentityStore
    {
        private const string Key = "player_identity_v1";
        public const int AvatarCount = 6;
        public static event Action Changed;
        private static PlayerIdentity _current;
#if UNITY_EDITOR
        private static IDictionary<string, string> _testStorage;
        public static IDisposable UseForTests(IDictionary<string, string> storage) => new TestScope(storage);
        private sealed class TestScope : IDisposable
        {
            private readonly PlayerIdentity _previousIdentity;
            private readonly IDictionary<string, string> _previousStorage;
            public TestScope(IDictionary<string, string> storage)
            {
                _previousIdentity = _current; _previousStorage = _testStorage;
                _current = null; _testStorage = storage ?? throw new ArgumentNullException(nameof(storage));
            }
            public void Dispose() { _current = _previousIdentity; _testStorage = _previousStorage; }
        }
#endif
        private static string Read(string key, string fallback = "")
        {
#if UNITY_EDITOR
            if (_testStorage != null) return _testStorage.TryGetValue(key, out var value) ? value : fallback;
#endif
            return PlayerPrefs.GetString(key, fallback);
        }
        public static PlayerIdentity Current
        {
            get
            {
                if (_current != null) return _current;
                try { _current = JsonUtility.FromJson<PlayerIdentity>(Read(Key)); }
                catch (ArgumentException) { }
                if (_current == null) _current = new PlayerIdentity();
                if (!Guid.TryParseExact(_current.id, "N", out _)) _current.id = Guid.NewGuid().ToString("N");
                _current.name = NormalizeName(_current.name);
                if (string.IsNullOrWhiteSpace(_current.name)) _current.name = NormalizeName(Read("player_name_multiplayer"));
                if (string.IsNullOrEmpty(_current.name)) _current.name = GuestName(_current.id);
                _current.avatar = Mathf.Clamp(_current.avatar, 0, AvatarCount - 1);
                Save();
                return _current;
            }
        }
        public static string NormalizeName(string name)
        {
            var result = new StringBuilder();
            foreach (char ch in (name ?? "").Trim())
            {
                if (!char.IsControl(ch) && !char.IsSurrogate(ch) && ch != '<' && ch != '>') result.Append(ch);
                if (result.Length == 16) break;
            }
            return result.ToString().Trim();
        }
        public static string GuestName(string id) => "Misafir " + id.Substring(0, Math.Min(4, id.Length)).ToUpperInvariant();
        public static void Set(string name, int avatar)
        {
            var current = Current;
            current.name = NormalizeName(name);
            if (current.name.Length == 0) current.name = GuestName(current.id);
            current.avatar = Mathf.Clamp(avatar, 0, AvatarCount - 1);
            Save(); Changed?.Invoke();
        }
        private static void Save()
        {
#if UNITY_EDITOR
            if (_testStorage != null)
            {
                _testStorage[Key] = JsonUtility.ToJson(_current); _testStorage["player_name_multiplayer"] = _current.name; return;
            }
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) return;
#endif
            if (!Application.isPlaying) return;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(_current));
            PlayerPrefs.SetString("player_name_multiplayer", _current.name);
            PlayerPrefs.Save();
        }
    }
}

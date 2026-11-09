using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    // Captures both cold-start and already-running links. Never leaves an active game automatically.
    public sealed class FriendInviteInbox : MonoBehaviour
    {
        private const string PendingKey = "friend_pending_invite_v1";
        public static FriendInviteInbox Instance { get; private set; }
        public FriendInvitation Pending { get; private set; }
        public event Action Changed;
        private bool _routing;
        private static bool IsProbe
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return MultiplayerNetworkProbe.IsRunning;
#else
                return false;
#endif
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (MultiplayerNetworkProbe.IsRunning) return;
#endif
            if (Instance == null) new GameObject("FriendInvitations").AddComponent<FriendInviteInbox>();
        }
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Application.deepLinkActivated += Receive;
            SceneManager.sceneLoaded += Loaded;
            string saved = IsProbe ? "" : PlayerPrefs.GetString(PendingKey, "");
            string[] parts = saved.Split('|');
            if (parts.Length == 2 && FriendInvitation.IsToken(parts[0]) && long.TryParse(parts[1], out var deadline)
                && DateTimeOffset.UtcNow.ToUnixTimeSeconds() < deadline) Pending = new FriendInvitation(parts[0]);
            if (!string.IsNullOrEmpty(Application.absoluteURL)) Receive(Application.absoluteURL);
        }
        public void Receive(string url)
        {
            if (!FriendInvitation.TryParse(url, FriendInviteSettings.Load(), out var invite)) return;
            if (Pending?.Token == invite.Token) { Changed?.Invoke(); Route(); return; }
            Pending = invite;
            if (!IsProbe)
            {
                PlayerPrefs.SetString(PendingKey, invite.Token + "|" + DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds()); PlayerPrefs.Save();
            }
            Changed?.Invoke(); Route();
        }
        public void Clear()
        {
            Pending = null;
            if (!IsProbe) { PlayerPrefs.DeleteKey(PendingKey); PlayerPrefs.Save(); }
            Changed?.Invoke();
        }
        private void Start() => Route();
        private void Loaded(Scene scene, LoadSceneMode mode)
        {
            _routing = false;
            if (scene.name == "WaitingScene" && Pending != null && PlayerLobby.Instance?.GetLobbyCode() == Pending.Code) Clear();
            Route();
        }
        public void Route()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (Pending == null || _routing || scene != "Menu") return;
            var tracker = FindFirstObjectByType<LevelTracker>();
            if (tracker == null) return;
            _routing = true; StartCoroutine(OpenLobbyNextFrame());
        }
        private IEnumerator OpenLobbyNextFrame()
        {
            // Menu.CleanUp destroys old networking objects at the end of its first frame.
            // The Lobby scene creates fresh managers; they cannot be required in Menu.
            yield return null;
            if (Pending != null && SceneManager.GetActiveScene().name == "Menu")
            {
                var tracker = FindFirstObjectByType<LevelTracker>();
                if (tracker != null) { tracker.SetGameOption(GameOption.MultiPlayer); SceneManager.LoadScene("Lobby"); }
            }
            _routing = false;
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Application.deepLinkActivated -= Receive; SceneManager.sceneLoaded -= Loaded; Instance = null;
        }
    }
}

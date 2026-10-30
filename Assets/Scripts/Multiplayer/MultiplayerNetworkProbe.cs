#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using Game;
using Menu;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    // Opt-in local integration runner. Excluded from release builds and bypasses all player progress writes.
    public sealed class MultiplayerNetworkProbe : MonoBehaviour
    {
        public static bool IsRunning => Option("-fruitLabMultiplayerProbe") != null;
        public static bool UseRelay => Option("-fruitLabProbeRelay") == "true";
        private static string Scenario => Option("-fruitLabProbeScenario") ?? "game";
        private static string Role => Option("-fruitLabMultiplayerProbe");
        public static string AuthenticationProfile => Option("-fruitLabProfile");
        public static string LocalPlayerName => Role == "host" ? "Test sunucu" : "Test misafir";
#if UNITY_EDITOR
        public const string EditorConfigPath = "Library/FruitLabEditor/editor-multiplayer-probe.json";
        private static Dictionary<string, string> _editorOptions;
#endif
        private bool _finished;
        private string _output;
        private int _rounds, _wins, _completedOutcomes;
        private MultiplayerOutcome _lastOutcome;
        private readonly int[] _expectedRewards = new int[3];
        private readonly List<string> _checks = new List<string>();
        private static string Option(string name)
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (_editorOptions == null && File.Exists(EditorConfigPath))
                    _editorOptions = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(EditorConfigPath));
                if (_editorOptions != null && _editorOptions.TryGetValue(name, out string value)) return value;
            }
#endif
            string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
        public static Difficulty SelectedDifficulty => (Difficulty)int.Parse(Option("-fruitLabProbeDifficulty") ?? "0");
        private static MultiplayerSettings Settings => MultiplayerRules.For(SelectedDifficulty);
        public static int[] Answer(int round) => (round % 2 == 1 ? new[] { 4, 1, 7, 2, 5 } : new[] { 2, 5, 6, 3, 8 }).Take(Settings.BoardCount).ToArray();
        private static int[] WrongGuess => Enumerable.Range(1, Settings.BoardCount).ToArray();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (!IsRunning) return;
            var root = new GameObject("MultiplayerIntegrationProbe"); DontDestroyOnLoad(root);
            root.AddComponent<MultiplayerNetworkProbe>();
        }
        private void Awake()
        {
            _output = Option("-fruitLabProbeOutput");
            Application.runInBackground = true; Application.targetFrameRate = 60;
            Application.logMessageReceived += OnLog;
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Finish(false, message + "\n" + stack);
        }
        private IEnumerator Start()
        {
            yield return null; yield return null;
            Check(SceneManager.GetActiveScene().name == "Menu", "The player starts in the real Menu scene");
            var play = (BaseButtonView)FindFirstObjectByType<MenuUIView>().GetMultiplayerButton();
            play.OnPointerDown(null);
            yield return null;
            Check(SceneManager.GetActiveScene().name == "Menu", "Navigation waits for pointer release");
            play.OnPointerUp(null); play.OnPointerClick(null);
            yield return new WaitForSecondsRealtime(.5f);
            Check(SceneManager.GetActiveScene().name == "Lobby", "Friend-play enters Lobby and stays there after the click");
            var network = NetworkManager.Singleton;
            Check(network != null && MultiplayerManager.Instance != null, "Lobby scene provides its real network managers");
            bool host = Role == "host";
            if (UseRelay)
            {
                yield return WaitFor(() => PlayerLobby.Instance.IsReady || PlayerLobby.Instance.LastError != null, 35, "Services initialize");
                Check(PlayerLobby.Instance.IsReady, "Unity services ready: " + PlayerLobby.Instance.LastError);
                string roomFile = Option("-fruitLabProbeRoomFile");
                if (host)
                {
                    var oldRooms = CleanupValidationRooms();
                    yield return WaitFor(() => oldRooms.IsCompleted, 30, "Previous private validation rooms are cleaned");
                    SelectMode(host);
                    yield return WaitFor(() => SceneManager.GetActiveScene().name == "WaitingScene" || PlayerLobby.Instance.LastError != null, 35, "Relay allocation and host startup");
                    Check(PlayerLobby.Instance.LastError == null, "Relay host created: " + PlayerLobby.Instance.LastError);
                    string roomCode = PlayerLobby.Instance.GetLobbyCode();
                    Check(!string.IsNullOrWhiteSpace(roomCode), "The created room retains its join code after Relay setup");
                    File.WriteAllText(roomFile + ".tmp", roomCode); File.Move(roomFile + ".tmp", roomFile);
                }
                else
                {
                    yield return WaitFor(() => File.Exists(roomFile) && new FileInfo(roomFile).Length > 0, 60, "A private test room is published");
                    var entry = FindFirstObjectByType<LobbyUIView>();
                    Click((BaseButtonView)entry.GetJoinWithCodeButton());
                    Check(entry.CodeInput.gameObject.activeInHierarchy, "Join button reveals the code input");
                    entry.CodeInput.text = File.ReadAllText(roomFile); entry.CodeInput.onSubmit.Invoke(entry.CodeInput.text);
                }
            }
            else
            {
                SelectMode(host);
                ushort port = ushort.Parse(Option("-fruitLabProbePort") ?? "17877");
                network.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", port, "127.0.0.1");
                Check(host ? MultiplayerManager.Instance.StartHost() : MultiplayerManager.Instance.StartClient(), "Transport starts");
                if (host) network.SceneManager.LoadScene("WaitingScene", LoadSceneMode.Single);
            }
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "WaitingScene" || (UseRelay && PlayerLobby.Instance.LastError != null), 70, "Waiting scene is synchronized");
            Check(SceneManager.GetActiveScene().name == "WaitingScene", "Join succeeds: " + PlayerLobby.Instance.LastError);
            if (UseRelay) Check(PlayerLobby.Instance.RoomDifficulty == SelectedDifficulty, "Lobby metadata synchronizes the host difficulty");
            yield return null; yield return null;
            var ready = FindFirstObjectByType<UserReady>();
            yield return WaitFor(() => ready != null && ready.IsSpawned, 15, "Readiness object spawns");
            yield return WaitFor(() => MultiplayerManager.Instance.IsSpawned && MultiplayerManager.Instance.IsPlayerIndexConnected(1)
                && MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(0).playerName.ToString() == "Test sunucu"
                && MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(1).playerName.ToString() == "Test misafir", 60,
                "The persistent manager spawns and both distinct names synchronize");
            yield return null;
            var names = FindObjectsByType<PlayerNameView>(FindObjectsSortMode.None);
            Check(names.Length == 2 && names.Any(row => row.DisplayedName == "Test sunucu") && names.Any(row => row.DisplayedName == "Test misafir"),
                "Both waiting-room name labels display their synchronized players");
            if (Scenario == "kick") { yield return KickScenario(names, network, ready, host); yield break; }
            if (host)
            {
                yield return WaitFor(() => ready.IsPlayerReady(network.ConnectedClientsIds.First(id => id != Unity.Netcode.NetworkManager.ServerClientId)), 20,
                    "The host receives the guest ready state before it is ready itself");
                Check(names[1].IsReadyVisible || names[0].IsReadyVisible, "The guest ready indicator updates on the host UI");
                yield return new WaitForSecondsRealtime(.4f);
            }
            var readyButton = (BaseButtonView)FindFirstObjectByType<WaitingSceneUIView>().GetReadyButton();
            readyButton.OnPointerDown(null); readyButton.OnPointerUp(null); readyButton.OnPointerClick(null);
            if (!host)
            {
                yield return WaitFor(() => ready.IsPlayerReady(network.LocalClientId), 10, "The guest ready button receives server confirmation");
                Check(names.Any(row => row.DisplayedName == "Test misafir" && row.IsReadyVisible), "The guest also sees its confirmed ready indicator");
            }
            yield return WaitFor(() => SceneManager.GetActiveScene().name == "Game", 75, "Both ready players enter Game");
            var match = FindFirstObjectByType<MultiplayerGameController>();
            yield return WaitFor(() => match != null && match.State.Round == 1 && match.State.Phase == MultiplayerPhase.Playing, 30, "Both boards acknowledge initialization");
            match.MatchEnded += (_, outcome) => { if (!outcome.Disconnected) { _lastOutcome = outcome; _completedOutcomes++; } };
            var ui = FindFirstObjectByType<GameUIView>();
            CheckMultiplayerHud(ui);
            Check(!ui.GetComponentInParent<Canvas>().GetComponentsInChildren<Transform>(true).Any(t => t.name == "YourTurnNotification"), "No turn notice covers the result history");
            int seconds = int.Parse(ui.MultiplayerTimerText.Split(' ')[0]);
            yield return new WaitForSecondsRealtime(1.05f);
            Check(int.Parse(ui.MultiplayerTimerText.Split(' ')[0]) < seconds, "The visible timer counts down using the shared network deadline");
            Check(FindFirstObjectByType<TargetNumberCreator>().GetTargetCardsList().Count == 0, "The live UI has no hidden answer before completion");
            var context = FindFirstObjectByType<GamePlayContext>();
            var initial = (IInitialCardAreaController)typeof(GamePlayContext).GetField("_initialCardAreaController", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(context);
            Check(initial.GetCardCount() == Settings.FruitCount && FindFirstObjectByType<BoardAreaView>().GetComponentsInChildren<BoardHolderView>().Length == Settings.BoardCount,
                "The real game creates the host's selected slots and fruit choices");
            // Probe commands deliberately bypass UI validation; the host must still reject them.
            match.SendProbeCommand(1, 1, host ? new[] { 1, 2 } : Answer(1));
            yield return new WaitForSecondsRealtime(.3f);
            Check(match.State.Turn == 1 && FindFirstObjectByType<ResultAreaView>().GetComponentsInChildren<ResultBlockView>().Length == 0,
                "Wrong-length and out-of-turn network commands are rejected");
            if (host) yield return Guess(initial, WrongGuess);
            else { yield return WaitFor(() => match.IsLocalTurn, 15, "Guest receives the next turn"); Check(ui.MultiplayerStatusText == "Sıra sende", "The header reflects the new local turn"); yield return Guess(initial, Answer(1)); }
            yield return EndOfRound(match, 1, !host);
            Check(FindFirstObjectByType<ResultAreaView>().GetComponentsInChildren<ResultBlockView>().Length == 2, "Both peers receive exactly two result rows");
            if (Scenario == "peer-leave") { yield return FinishedPeerLeave(match, host); yield break; }
            yield return Rematch(match, 2);
            if (!host) yield return Guess(initial, WrongGuess);
            else { yield return WaitFor(() => match.IsLocalTurn, 15, "Host receives round-two turn"); yield return Guess(initial, Answer(2)); }
            yield return EndOfRound(match, 2, host);
            yield return Rematch(match, 3);
            yield return WaitFor(() => match.State.Turn >= 2, 18, "The host advances an expired turn using its own deadline");
            if (!host) yield return Guess(initial, Answer(3));
            yield return EndOfRound(match, 3, !host);
            yield return Rematch(match, 4);
            if (host)
            {
                yield return new WaitForSecondsRealtime(.7f);
                if (UseRelay)
                {
                    var cleanup = PlayerLobby.Instance.LeaveLobbyAsync();
                    yield return WaitFor(() => cleanup.IsCompleted, 15, "Private validation lobby is deleted");
                }
                MultiplayerConnection.Shutdown(); yield return new WaitForSecondsRealtime(.2f);
                Finish(true, null);
            }
            else
            {
                yield return WaitFor(() => match.State.Phase == MultiplayerPhase.Disconnected, 15, "Host departure ends the match");
                yield return new WaitForSecondsRealtime(.7f);
                var popup = FindFirstObjectByType<LevelFinishPopupView>();
                Check(popup != null && !((Component)popup.GetButton(LevelFinishButtonType.Game)).gameObject.activeSelf,
                    "A disconnected match offers no broken rematch action");
                Check(FindFirstObjectByType<GameUIView>().MultiplayerStatusText == "Rakiple bağlantı kesildi", "A peer departure is shown in the game header");
                Check(PowerUpTypeUtility.OrderedRewardTypes.Sum(type => FindFirstObjectByType<LevelTracker>().GetPowerUpCount(type)) == _wins,
                    "Disconnect does not grant a powerup reward");
                Finish(true, null);
            }
        }
        private IEnumerator KickScenario(PlayerNameView[] names, NetworkManager network, UserReady ready, bool host)
        {
            if (host)
            {
                yield return new WaitForSecondsRealtime(.5f);
                var row = names.Single(item => item.DisplayedName == "Test misafir");
                var button = (BaseButtonView)row.GetKickButton();
                var canvas = button.GetComponentInParent<Canvas>();
                Canvas.ForceUpdateCanvases();
                var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                pointer.position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    button.GetButtonRectTransform().TransformPoint(button.GetButtonRectTransform().rect.center));
                var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Count > 0 && UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
                    "The actual kick button receives UI raycasts");
                Click(button);
                yield return WaitFor(() => !MultiplayerManager.Instance.IsPlayerIndexConnected(1) && network.ConnectedClientsIds.Count == 1, 15,
                    "Kicking removes the guest from transport and roster");
                Check(network.IsHost && network.IsListening && !ready.IsPlayerReady(Unity.Netcode.NetworkManager.ServerClientId), "Kicking keeps the host in the open waiting room with readiness reset");
                yield return new WaitForSecondsRealtime(1f);
                if (UseRelay)
                {
                    var task = LobbyService.Instance.GetLobbyAsync(PlayerLobby.Instance.GetLobby().Id);
                    yield return WaitFor(() => task.IsCompleted, 15, "Lobby membership is inspected after kicking");
                    Check(task.Status == TaskStatus.RanToCompletion && task.Result.Players.Count == 1, "Kicking releases the service lobby slot too");
                }
                yield return new WaitForSecondsRealtime(.4f); Finish(true, null);
            }
            else
            {
                yield return WaitFor(() => FindFirstObjectByType<DisconnectionPopupView>() != null, 20, "A kicked guest receives a disconnect popup");
                var popup = FindFirstObjectByType<DisconnectionPopupView>();
                Check(popup.DisplayedMessage == "Düellodan çıkarıldın.", "The guest is told it was removed from the duel");
                Click((BaseButtonView)popup.GetMenuButtonView());
                yield return WaitFor(() => SceneManager.GetActiveScene().name == "Menu", 10, "A kicked guest can return to Menu");
                Finish(true, null);
            }
        }
        private IEnumerator FinishedPeerLeave(MultiplayerGameController match, bool host)
        {
            var popup = FindFirstObjectByType<LevelFinishPopupView>();
            if (host)
            {
                Click(((Component)popup.GetButton(LevelFinishButtonType.Game)).GetComponent<BaseButtonView>());
                yield return WaitFor(() => match.State.Phase == MultiplayerPhase.Disconnected, 20, "A peer leaving a pending rematch closes the session");
                yield return new WaitForSecondsRealtime(.6f);
                Check(FindFirstObjectByType<GameUIView>().MultiplayerStatusText == "Rakiple bağlantı kesildi", "Leaving a pending rematch is explained in the header");
                Check(popup.GetText().text == "Rakibin kazandı" && !((Component)popup.GetButton(LevelFinishButtonType.Game)).gameObject.activeSelf,
                    "The completed result remains visible and rematch is removed when the peer leaves");
                Finish(true, null);
            }
            else
            {
                yield return WaitFor(() => match.State.RematchVotes != 0, 10, "The winning guest receives a new-game offer");
                Check(FindFirstObjectByType<GameUIView>().MultiplayerStatusText == "Rakibin yeni oyun teklif ediyor", "A pending offer is visible before the guest leaves");
                yield return new WaitForSecondsRealtime(.5f);
                ((Component)popup.GetButton(LevelFinishButtonType.Menu)).GetComponent<BaseButtonView>().OnPointerDown(null);
                yield return WaitFor(() => SceneManager.GetActiveScene().name == "Menu", 10, "The peer deliberately leaves through Menu");
                Check(FindFirstObjectByType<LevelTracker>().GetRevealingPowerUpCount() == 1, "Leaving after victory preserves the awarded powerup");
                Finish(true, null);
            }
        }
        private void CheckMultiplayerHud(GameUIView ui)
        {
            Check(ui.IsMultiplayer, "The multiplayer HUD is enabled");
            if (FindFirstObjectByType<MultiplayerGameController>().State.Phase == MultiplayerPhase.Playing)
                Check(!string.IsNullOrEmpty(ui.MultiplayerTimerText), "The multiplayer HUD displays a countdown during play");
            else Check(string.IsNullOrEmpty(ui.MultiplayerTimerText), "The timer stops after the match");
            var canvas = ui.GetComponentInParent<Canvas>();
            Check(!canvas.GetComponentsInChildren<LifeBarView>(true).Any(view => view.gameObject.activeInHierarchy), "Multiplayer has no guess/life bar");
            Check(!canvas.GetComponentsInChildren<VerticalCrystalProgressView>(true).Any(view => view.gameObject.activeInHierarchy), "Multiplayer has no crystal progress bar");
            var coin = (Component)typeof(GameUIView).GetField("coinCountHolder", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
            Check(!coin.gameObject.activeInHierarchy, "Multiplayer has no coin counter");
        }
        private void SelectMode(bool host)
        {
            var view = FindFirstObjectByType<LobbyUIView>();
            Click((BaseButtonView)view.GetCreateLobbyButton());
            Check(((Component)view.GetDifficultyButton(0)).gameObject.activeInHierarchy, "Create button reveals the three difficulty buttons");
            if (UseRelay) Click((BaseButtonView)view.GetDifficultyButton((int)SelectedDifficulty));
            else PlayerLobby.Instance.SelectDifficulty(SelectedDifficulty);
            Check(PlayerLobby.Instance.RoomDifficulty == SelectedDifficulty, "The difficulty button creates the selected duel");
        }
        private static void Click(BaseButtonView button)
        { button.OnPointerDown(null); button.OnPointerUp(null); button.OnPointerClick(null); }
        private async Task CleanupValidationRooms()
        {
            try
            {
                foreach (string id in await LobbyService.Instance.GetJoinedLobbiesAsync())
                {
                    try
                    {
                        var room = await LobbyService.Instance.GetLobbyAsync(id);
                        if ((room.Name == "Fruit Lab doğrulama" || room.Name == "Düello") && room.HostId == AuthenticationService.Instance.PlayerId)
                            await LobbyService.Instance.DeleteLobbyAsync(id);
                    }
                    catch (LobbyServiceException) { }
                }
            }
            catch (LobbyServiceException) { }
        }
        private IEnumerator Guess(IInitialCardAreaController initial, int[] answer)
        {
            foreach (int fruit in answer)
            {
                Check(initial.TryGetCardItem(fruit - 1, out var card) && !card.GetCardMoveHandler().IsMovementLocked(), "The active player's card can be moved");
                card.GetCardMoveHandler().OnPointerDown(null); card.GetCardMoveHandler().OnPointerUp(null);
                yield return new WaitForSecondsRealtime(.4f);
            }
            var button = (BaseButtonView)FindFirstObjectByType<GameUIView>().GetCheckButton();
            button.OnPointerDown(null); button.OnPointerUp(null); button.OnPointerClick(null);
        }
        private IEnumerator EndOfRound(MultiplayerGameController match, int round, bool won)
        {
            yield return WaitFor(() => match.State.Round == round && match.State.Phase == MultiplayerPhase.Finished, 18, "Round completes on both peers");
            yield return new WaitForSecondsRealtime(.8f);
            var popup = FindFirstObjectByType<LevelFinishPopupView>();
            Check(popup != null && popup.GetText().text == (won ? "Kazandın!" : "Rakibin kazandı"), "The real popup shows the correct winner");
            Check(FindFirstObjectByType<TargetNumberCreator>().GetTargetCardsList().SequenceEqual(Answer(round)), "The answer is revealed only at the end");
            Check(FindFirstObjectByType<LevelTracker>().GetLevelId() == 1 && FindFirstObjectByType<LevelTracker>().GetCoinCount() == 0,
                "Multiplayer leaves single-player progress and balances unchanged");
            CheckMultiplayerHud(FindFirstObjectByType<GameUIView>());
            Check(_completedOutcomes == round && _lastOutcome.Reward.HasValue, "The host announces one validated win and reward per round");
            if (won) { _wins++; _expectedRewards[(int)_lastOutcome.Reward.Value]++; }
            var tracker = FindFirstObjectByType<LevelTracker>();
            foreach (RewardType type in PowerUpTypeUtility.OrderedRewardTypes)
                Check(tracker.GetPowerUpCount(type) == _expectedRewards[(int)type], "Only the winner receives exactly one of the announced powerups");
            if (won) Check(popup.transform.Find("ResultSubtitle").GetComponent<TMPro.TMP_Text>().text.Contains(PowerUpTypeUtility.Name(_lastOutcome.Reward.Value)), "The victory panel names the granted powerup");
            foreach (var row in FindFirstObjectByType<ResultAreaView>().GetComponentsInChildren<ResultBlockView>())
            {
                bool local = round == 3 ? won : (row.transform.GetSiblingIndex() == 0 ? !won : won);
                var stripe = row.transform.Find("PlayerStripe").GetComponent<UnityEngine.UI.Image>();
                Check(row.IsLocalGuess == local && stripe.gameObject.activeInHierarchy && stripe.color == (local ? FruitLabStyle.LocalPlayer : FruitLabStyle.OpponentPlayer) && !stripe.raycastTarget,
                    "Shared rows use blue for local guesses and red for opponent guesses from each player's perspective");
            }
            if (Role == "host") match.RepeatProbeOutcome();
            yield return new WaitForSecondsRealtime(.2f);
            Check(_completedOutcomes == round && PowerUpTypeUtility.OrderedRewardTypes.Sum(tracker.GetPowerUpCount) == _wins,
                "Replayed finish packets cannot grant a second reward or show a second outcome");
            _rounds++;
        }
        private IEnumerator Rematch(MultiplayerGameController match, int next)
        {
            var popup = FindFirstObjectByType<LevelFinishPopupView>();
            var button = ((Component)popup.GetButton(LevelFinishButtonType.Game)).GetComponent<BaseButtonView>();
            var ui = FindFirstObjectByType<GameUIView>();
            if (Role == "host")
            {
                Click(button);
                yield return WaitFor(() => match.LocalRematchRequested, 5, "The sender receives its rematch vote confirmation");
                Check(ui.MultiplayerStatusText == "Rakibin yanıtı bekleniyor…", "The sender sees that its offer is awaiting a reply");
                yield return new WaitForSecondsRealtime(.6f);
                Check(match.State.Round == next - 1 && match.State.Phase == MultiplayerPhase.Finished, "A rematch never starts without the opponent accepting");
            }
            else
            {
                yield return WaitFor(() => match.State.RematchVotes != 0, 8, "The opponent receives the rematch offer");
                Check(ui.MultiplayerStatusText == "Rakibin yeni oyun teklif ediyor", "The rematch offer appears in the header");
                yield return new WaitForSecondsRealtime(1.1f); Click(button);
            }
            yield return WaitFor(() => match.State.Round == next && match.State.Phase == MultiplayerPhase.Playing, 20, "Two rematch votes rebuild a clean shared round");
            Check(FindFirstObjectByType<ResultAreaView>().GetComponentsInChildren<ResultBlockView>().Length == 0, "Rematch clears previous result rows");
            Check(FindFirstObjectByType<LevelTracker>().GetNumberOfBoardCardsInMultiplayer() == Settings.BoardCount,
                "Rematch retains the host difficulty on both peers");
            Check(FindFirstObjectByType<TargetNumberCreator>().GetTargetCardsList().Count == 0, "Rematch hides the new secret again");
        }
        private IEnumerator WaitFor(Func<bool> condition, double timeout, string label)
        {
            double end = Time.realtimeSinceStartupAsDouble + timeout;
            while (!condition() && Time.realtimeSinceStartupAsDouble < end && !_finished) yield return null;
            Check(!_finished && condition(), label);
        }
        private void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Multiplayer integration: " + label);
            _checks.Add(label);
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_output)) File.WriteAllText(_output + ".progress", Newtonsoft.Json.JsonConvert.SerializeObject(new { role = Role, check = label, scene = SceneManager.GetActiveScene().name, screen = new[] { Screen.width, Screen.height } }));
#endif
        }
        private void Finish(bool success, string error)
        {
            if (_finished) return; _finished = true;
            var network = NetworkManager.Singleton;
            string relayStatus = "not-started";
            if (network != null && UseRelay)
            {
                var transport = network.GetComponent<UnityTransport>();
                var field = typeof(UnityTransport).GetField("m_Driver", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null && transport != null)
                {
                    var driver = (NetworkDriver)field.GetValue(transport);
                    if (driver.IsCreated) relayStatus = driver.GetRelayConnectionStatus().ToString();
                }
            }
            var diagnostic = new { scene = SceneManager.GetActiveScene().name, relay_status = relayStatus,
                listening = network != null && network.IsListening, connected = network != null && network.IsConnectedClient,
                services_ready = PlayerLobby.Instance != null && PlayerLobby.Instance.IsReady,
                connection_error = PlayerLobby.Instance != null ? PlayerLobby.Instance.LastError : null };
            if (!string.IsNullOrEmpty(_output))
                File.WriteAllText(_output, Newtonsoft.Json.JsonConvert.SerializeObject(new { success, relay = UseRelay, scenario = Scenario, difficulty = SelectedDifficulty.ToString(), slots = Settings.BoardCount, fruits = Settings.FruitCount, role = Role, rounds = _rounds, wins = _wins, reward_counts = _expectedRewards, checks = _checks, error, diagnostic }, Newtonsoft.Json.Formatting.Indented));
            StartCoroutine(FinishCleanup(success));
        }
        private IEnumerator FinishCleanup(bool success)
        {
            if (UseRelay && PlayerLobby.Instance != null)
            {
                var cleanup = PlayerLobby.Instance.LeaveLobbyAsync();
                double until = Time.realtimeSinceStartupAsDouble + 8;
                while (!cleanup.IsCompleted && Time.realtimeSinceStartupAsDouble < until) yield return null;
            }
            MultiplayerConnection.Shutdown(); yield return null;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(success ? 0 : 1);
#endif
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
    }
}
#endif

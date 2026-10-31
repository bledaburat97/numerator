using System;
using Unity.Netcode;
using Zenject;

namespace Scripts
{
    public class PlayerNameAreaController : IPlayerNameAreaController
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        private IPlayerNameView[] _playerNameViewList = new IPlayerNameView[MultiplayerManager.MAX_NUM_OF_USERS];
        private IPlayerNameAreaView _view;
        private IUserReady _userReady;
        public PlayerNameAreaController(IPlayerNameAreaView view)
        {
            _view = view;
        }
        
        public void Initialize(IUserReady userReady)
        {
            _userReady = userReady;
            _view.Init(UnsubscribeEvents);
            for (int i = 0; i < MultiplayerManager.MAX_NUM_OF_USERS; i++)
            {
                _playerNameViewList[i] = null;
            }
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;
            _userReady.OnReadyChanged += OnReadyChanged;
            UpdatePlayers();
        }

        private void OnReadyChanged(object sender, EventArgs e)
        {
            UpdatePlayers();
        }

        private void OnPlayerDataNetworkListChanged(object sender, EventArgs args)
        {
            UpdatePlayers();
        }

        private void UpdatePlayers()
        {
            for (int playerIndex = 0; playerIndex < MultiplayerManager.MAX_NUM_OF_USERS; playerIndex++)
            {
                if (MultiplayerManager.Instance.IsPlayerIndexConnected(playerIndex))
                {
                    if (_playerNameViewList[playerIndex] == null)
                    {
                        IPlayerNameView playerNameView = _view.CreatePlayerNameView();
                        playerNameView.Init(playerIndex, OnKickButtonClicked);

                        int currentPlayerIndex = playerIndex;
                        IBaseButtonController kickButtonController =
                            _baseButtonControllerFactory.Create(playerNameView.GetKickButton(), () => OnKickButtonClicked(currentPlayerIndex));
                        
                        kickButtonController.SetClickOnRelease(true);
                        _playerNameViewList[playerIndex] = playerNameView;
                    }

                    PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
                    _playerNameViewList[playerIndex].SetRole(playerData.clientId == NetworkManager.ServerClientId,
                        playerData.clientId == NetworkManager.Singleton.LocalClientId,
                        NetworkManager.Singleton.IsServer && playerData.clientId != NetworkManager.ServerClientId);
                    _playerNameViewList[playerIndex].SetReadyStatus(_userReady.IsPlayerReady(playerData.clientId));
                    string name = playerData.playerName.ToString();
                    _playerNameViewList[playerIndex].SetPlayerName(string.IsNullOrWhiteSpace(name) ? "Bağlanıyor…" : name);
                }

                else
                {
                    if (_playerNameViewList[playerIndex] != null)
                    {
                        IPlayerNameView playerNameView = _playerNameViewList[playerIndex];
                        _playerNameViewList[playerIndex] = null;
                        playerNameView.Destroy();

                    }
                }
            }
        }

        private void OnKickButtonClicked(int playerIndex)
        {
            var manager = MultiplayerManager.Instance;
            if (manager == null || !manager.IsServer || !manager.IsPlayerIndexConnected(playerIndex)) return;
            PlayerData playerData = manager.GetPlayerDataFromPlayerIndex(playerIndex);
            manager.KickPlayer(playerData.clientId);
        }

        private void UnsubscribeEvents()
        {
            if (MultiplayerManager.Instance != null) MultiplayerManager.Instance.OnPlayerDataNetworkListChanged -= OnPlayerDataNetworkListChanged;
            _userReady.OnReadyChanged -= OnReadyChanged;
        }
    }

    public interface IPlayerNameAreaController
    {
        void Initialize(IUserReady userReady);
    }
}

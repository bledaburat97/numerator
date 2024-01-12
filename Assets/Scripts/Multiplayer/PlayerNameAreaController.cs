using System;

namespace Scripts
{
    public class PlayerNameAreaController : IPlayerNameAreaController
    {
        private IPlayerNameView[] _playerNameViewList = new IPlayerNameView[MultiplayerManager.MAX_NUM_OF_USERS];
        private IPlayerNameAreaView _view;
        public PlayerNameAreaController(IPlayerNameAreaView view)
        {
            _view = view;
        }
        
        public void Initialize()
        {
            for (int i = 0; i < MultiplayerManager.MAX_NUM_OF_USERS; i++)
            {
                _playerNameViewList[i] = null;
            }
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;
            UserReady.Instance.OnReadyChanged += OnReadyChanged;
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
                        _playerNameViewList[playerIndex] = playerNameView;
                    }

                    PlayerData playerData = MultiplayerManager.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
                    _playerNameViewList[playerIndex].SetReadyStatus(UserReady.Instance.IsPlayerReady(playerData.clientId));
                    _playerNameViewList[playerIndex].SetPlayerName(playerData.playerName.ToString());
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
            PlayerData playerData = MultiplayerManager.Instance
                .GetPlayerDataFromPlayerIndex(playerIndex);
            PlayerLobby.Instance.KickPlayerFromLobby(playerData.playerId.ToString());
            MultiplayerManager.Instance.KickPlayer(playerData.clientId);
        }
    }

    public interface IPlayerNameAreaController
    {
        void Initialize();
    }
}
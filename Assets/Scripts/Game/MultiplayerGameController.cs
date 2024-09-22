using System;
using System.Collections.Generic;
using Scripts;
using Unity.Netcode;
using Zenject;

namespace Game
{
    public class MultiplayerGameController : NetworkBehaviour, IMultiplayerGameController
    {
        [Inject] private IGamePopupCreator _gamePopupCreator;
        [Inject] private IUserReady _userReady;
        [Inject] private ITurnOrderDeterminer _turnOrderDeterminer;

        private NetworkVariable<bool> _isGameEnd = new NetworkVariable<bool>(false);
        private NetworkVariable<bool> _isAnyReady = new NetworkVariable<bool>(false);
        private Dictionary<ulong, bool> _playerSuccessDictionary;
        private bool _isLocalGameEnd = false;
        private Dictionary<ulong, bool> _playerReadyDictionary;
        private bool _isLocalReady;

        public void Initialize()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            //_levelManager.MultiplayerLevelEndEvent += OnMultiplayerLevelEnd;
            _turnOrderDeterminer.LocalTurnEvent += ChangeLocalTurn;
            _isLocalReady = false;
            _playerSuccessDictionary = new Dictionary<ulong, bool>();
            _playerReadyDictionary = new Dictionary<ulong, bool>();
        }
        
        public override void OnNetworkSpawn()
        {
            _isGameEnd.OnValueChanged += CreateMultiplayerLevelLostPopup;
            _isAnyReady.OnValueChanged += CheckNewGameOfferPopup;
        }
        
        private void ChangeLocalTurn(object sender, bool isLocalTurn)
        {
            if (isLocalTurn)
            {
                _gamePopupCreator.CloseNotAbleToMovePopup();
                _gamePopupCreator.CreateAbleToMovePopup();
            }
            
            else
            {
                _gamePopupCreator.CloseAbleToMovePopup();
            }
        }
        
        private void CheckNewGameOfferPopup(bool previousValue, bool newValue)
        {
            if (!_isLocalReady && _isAnyReady.Value)
            {
                _gamePopupCreator.CreateNewGameOfferPopup();
            }

            if (!_isAnyReady.Value)
            {
                _gamePopupCreator.CloseNewGameOfferPopup();
            }
        }
        
        private void CreateMultiplayerLevelLostPopup(bool previousValue, bool newValue)
        {
            if (!_isLocalGameEnd)
            {
                _gamePopupCreator.CreateMultiplayerLevelEnd(false, _userReady, OnPlayerReady);
            }
        }
        
        private void OnMultiplayerLevelEnd(object sender, EventArgs args)
        {
            _isLocalGameEnd = true;
            ChangeMultiplayerLevelEndStateServerRpc();
            _gamePopupCreator.CreateMultiplayerLevelEnd(true, _userReady, OnPlayerReady);
        }

        private void OnPlayerReady()
        {
            _isLocalReady = true;
            if (_isAnyReady.Value) return;
            _gamePopupCreator.CreateWaitingOpponentPopup(OnPlayerUnready);
            TryChangeReadinessStatusServerRpc(true);
        }

        private void OnPlayerUnready()
        {
            _userReady.SetPlayerUnready();
            _isLocalReady = false;
            TryChangeReadinessStatusServerRpc(false);
        }
        
        [ServerRpc (RequireOwnership = false)]
        private void ChangeMultiplayerLevelEndStateServerRpc(ServerRpcParams serverRpcParams = default)
        {
            _playerSuccessDictionary[serverRpcParams.Receive.SenderClientId] = true;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (_playerSuccessDictionary.ContainsKey(clientId) && _playerSuccessDictionary[clientId])
                {
                    _isGameEnd.Value = true;
                    return;
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void TryChangeReadinessStatusServerRpc(bool readinessStatus, ServerRpcParams serverRpcParams = default)
        {
            _playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = readinessStatus;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (_playerReadyDictionary.ContainsKey(clientId) && _playerReadyDictionary[clientId])
                {
                    _isAnyReady.Value = true;
                    return;
                }
            }

            _isAnyReady.Value = false;
        }
        
        private void OnClientDisconnectCallback(ulong clientId)
        {
            if((IsHost && clientId != 0) || (!IsHost && clientId == 0))
            {
                _gamePopupCreator.CreateDisconnectionPopup();
            }
        }
        
        private new void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }
    }
    
    public class MultiplayerLevelFinishEventArgs : EventArgs
    {
        public bool isSuccess;
        public int currentRewardStarCount;
    }

    public interface IMultiplayerGameController
    {
        void Initialize();
    }
}
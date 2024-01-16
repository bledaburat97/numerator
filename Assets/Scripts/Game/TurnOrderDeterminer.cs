using System;
using System.Collections.Generic;
using Scripts;
using Unity.Netcode;

namespace Game
{
    public class TurnOrderDeterminer : NetworkBehaviour, ITurnOrderDeterminer
    {
        private bool _isLocalTurn;
        private IGameClockController _gameClockController;
        private IResultManager _resultManager;

        public void Initialize(IGameClockController gameClockController, IResultManager resultManager)
        {
            _gameClockController = gameClockController;
            _isLocalTurn = false;
            _resultManager = resultManager;
            if (IsHost)
            {
                StartLocalPlayerTurn();
            }
        }

        private void StartLocalPlayerTurn()
        {
            if (!_isLocalTurn)
            {
                _isLocalTurn = true;
                _gameClockController.StartTimer(ChangeTurns);
                _resultManager.ResultBlockAddition += OnGuessNumber;
            }
            else
            {
                _isLocalTurn = false;
                _resultManager.ResultBlockAddition -= OnGuessNumber;
            }
        }

        private void OnGuessNumber(object sender, ResultBlockModel model)
        {
            ChangeTurns();
        }
        
        private void ChangeTurns()
        {
            _gameClockController.RemoveTimer();
            ChangeTurnsServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChangeTurnsServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ChangeTurnsClientRpc();
            /*
            _playerActivenessDictionary[serverRpcParams.Receive.SenderClientId] = false;
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (_playerActivenessDictionary.ContainsKey(clientId) && !_playerActivenessDictionary[clientId]
                    && serverRpcParams.Receive.SenderClientId != clientId)
                {
                    _playerActivenessDictionary[clientId] = true;
                    _activeClientId.Value = clientId;
                    return;
                }
            }
            */
        }

        [ClientRpc]
        private void ChangeTurnsClientRpc()
        {
            StartLocalPlayerTurn();
        }
    }

    public interface ITurnOrderDeterminer
    {
        void Initialize(IGameClockController gameClockController, IResultManager resultManager);
    }
}
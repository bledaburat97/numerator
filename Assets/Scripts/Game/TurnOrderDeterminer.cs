using System;
using Scripts;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class TurnOrderDeterminer : NetworkBehaviour, ITurnOrderDeterminer
    {
        [Inject] private IHapticController _hapticController;
        private bool _isLocalTurn;
        private IGameClockController _gameClockController;
        private IResultManager _resultManager;
        public event EventHandler<bool> LocalTurnEvent;

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
                Debug.Log("Turn vibration");
                _hapticController.Vibrate(HapticType.CardRelease);
                _gameClockController.StartTimer(ChangeTurns);
                _resultManager.NumberGuessed += OnGuessNumber;
                LocalTurnEvent?.Invoke(this, true);
            }
            else
            {
                _isLocalTurn = false;
                _resultManager.NumberGuessed -= OnGuessNumber;
                LocalTurnEvent?.Invoke(this, false);
            }
        }

        public bool IsLocalTurn()
        {
            return _isLocalTurn;
        }

        private void OnGuessNumber(object sender, NumberGuessedEventArgs args)
        {
            if(!args.isGuessRight) ChangeTurns();
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
        bool IsLocalTurn();
        event EventHandler<bool> LocalTurnEvent;
    }
}
using System;
using Scripts;
using Unity.Netcode;

namespace Game
{
    public class TurnOrderDeterminer : NetworkBehaviour, ITurnOrderDeterminer
    {
        private bool _isLocalTurn;
        private IGameClockController _gameClockController;
        private IResultManager _resultManager;
        public event EventHandler AbleToMove;

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
                _resultManager.NumberGuessed += OnGuessNumber;
                AbleToMove?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _isLocalTurn = false;
                _resultManager.NumberGuessed -= OnGuessNumber;
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
        event EventHandler AbleToMove;
    }
}
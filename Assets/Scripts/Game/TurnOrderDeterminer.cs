using System;
using Game;
using Unity.Netcode;
using Zenject;

namespace Scripts
{
    public class TurnOrderDeterminer : NetworkBehaviour, ITurnOrderDeterminer
    {
        [Inject] private IMultiplayerGameController _match;
        public event EventHandler<bool> LocalTurnEvent;
        public void Initialize(IGameClockController clock, IResultManager results)
        {
            clock.RemoveTimer();
            _match.StateChanged -= OnState; _match.StateChanged += OnState;
        }
        private void OnState(object sender, MultiplayerPublicState state) => LocalTurnEvent?.Invoke(this, IsLocalTurn());
        public bool IsLocalTurn() => _match != null && _match.IsLocalTurn;
        public override void OnDestroy() { if (_match != null) _match.StateChanged -= OnState; base.OnDestroy(); }
    }
    public interface ITurnOrderDeterminer
    {
        void Initialize(IGameClockController gameClockController, IResultManager resultManager);
        bool IsLocalTurn();
        event EventHandler<bool> LocalTurnEvent;
    }
}

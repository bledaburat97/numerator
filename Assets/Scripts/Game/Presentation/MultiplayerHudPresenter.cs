using System;

namespace Scripts
{
    // Uses the server deadline on every tick; only changed display values reach the view.
    public sealed class MultiplayerHudPresenter
    {
        private readonly Func<double> _now;
        private readonly Action<int, bool> _render;
        private double _deadline;
        private int _lastSeconds = -1;
        private bool _lastVisible;
        private bool _configured;

        public MultiplayerHudPresenter(Func<double> now, Action<int, bool> render)
        { _now = now; _render = render; }

        public void SetDeadline(double deadline)
        { _deadline = deadline; _configured = true; Tick(); }

        public void Tick()
        {
            if (!_configured) return;
            bool visible = _deadline > 0;
            int seconds = visible ? (int)Math.Ceiling(Math.Max(0, _deadline - _now())) : 0;
            if (seconds == _lastSeconds && visible == _lastVisible) return;
            _lastSeconds = seconds; _lastVisible = visible;
            _render(seconds, visible);
        }
    }
}

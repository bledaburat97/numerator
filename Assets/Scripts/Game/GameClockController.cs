using System;

namespace Scripts
{
    public class GameClockController : IGameClockController
    {
        private IGameClockView _view;
        public GameClockController(IGameClockView view)
        {
            _view = view;
        }

        public void StartTimer(Action onTimerEnd)
        {
            _view.StartTimer(onTimerEnd);
        }

        public void RemoveTimer()
        {
            _view.RemoveTimer();
        }
    }

    public interface IGameClockController
    {
        void StartTimer(Action onTimerEnd);
        void RemoveTimer();
    }
}
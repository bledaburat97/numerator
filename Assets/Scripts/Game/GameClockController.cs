using System;
using Zenject;

namespace Scripts
{
    public class GameClockController : IGameClockController
    {
        [Inject] private IHapticController _hapticController;
        private IGameClockView _view;
        private ILevelManager _levelManager;
        public GameClockController(IGameClockView view)
        {
            _view = view;
        }

        public void Initialize(IResultManager resultManager)
        {
            _view.Init(() => _hapticController.Vibrate(HapticType.CardGrab));
            RemoveTimer();
            resultManager.CorrectCardsBackFlipped += OnLevelEnd;
        }

        private void OnLevelEnd(object sender, EventArgs e)
        {
            RemoveTimer();
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
        void Initialize(IResultManager resultManager);
    }
}
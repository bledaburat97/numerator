using System;
using Game;
using Zenject;

namespace Scripts
{
    public class GameClockController : IGameClockController
    {
        [Inject] private IHapticController _hapticController;
        [Inject] private IGuessManager _guessManager;
        
        private IGameClockView _view;
        public GameClockController(IGameClockView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            _view.Init(() => _hapticController.Vibrate(HapticType.CardGrab));
            RemoveTimer();
            //levelManager.CardsBackFlipped += OnLevelEnd;
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
        void Initialize();
    }
}
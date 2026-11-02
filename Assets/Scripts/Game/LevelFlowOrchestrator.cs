using System;
using Zenject;

namespace Game
{
    public interface ILevelSession : IDisposable
    {
        LevelFlowState State { get; }
        bool IsGameOver { get; }
        void Begin(LevelEntryMode mode);
    }

    public sealed class LevelFlowOrchestrator : ILevelFlowOrchestrator, IDisposable
    {
        private readonly ILevelSession _session;
        [Inject] public LevelFlowOrchestrator(ILevelSession session) => _session = session;
        public void Begin(LevelEntryMode mode) => _session.Begin(mode);
        public void Retry() { if (_session is SinglePlayerSession single) single.Retry(); }
        public void GoToNextLevel() { if (_session is SinglePlayerSession single) single.GoToNextLevel(); }
        public bool IsGameOver() => _session.IsGameOver;
        public bool IsTransitioning() => _session.State != LevelFlowState.Idle && _session.State != LevelFlowState.Playing;
        public LevelFlowState GetState() => _session.State;
        public void Dispose() => _session.Dispose();
    }

    public interface ILevelFlowOrchestrator
    {
        void Begin(LevelEntryMode mode);
        void Retry();
        void GoToNextLevel();
        bool IsGameOver();
        bool IsTransitioning();
        LevelFlowState GetState();
    }

    public enum LevelEntryMode
    {
        New,
        Resume,
        Retry,
        NextLevel
    }

    public enum LevelFlowState
    {
        Idle,
        Building,
        IntroPlaying,
        Playing,
        EndingSuccess,
        EndingFail,
        Transitioning
    }
}

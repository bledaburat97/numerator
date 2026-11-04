using System;

namespace Game
{
    public interface ILevelSession : IDisposable
    {
        LevelFlowState State { get; }
        bool IsGameOver { get; }
        void Begin(LevelEntryMode mode);
    }

    public interface ISinglePlayerProgression
    {
        void Retry();
        void GoToNextLevel();
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

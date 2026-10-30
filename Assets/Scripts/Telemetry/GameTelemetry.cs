using System;
using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Scripts
{
    // Only bounded gameplay values enter analytics. No names, codes, answers or log messages.
    public static class GameTelemetry
    {
        public const string EventName = "fruitlabGameplay";
        private sealed class Attempt
        {
            public string Mode, Difficulty, Entry;
            public int Level, Slots, Fruits, Round, Guesses;
            public double Started, PausedAt = -1, PausedSeconds;
        }
        private static Attempt _attempt;
        private static Action<Dictionary<string, object>> _testRecorder;
        private static Func<double> _clock = () => Time.realtimeSinceStartupAsDouble;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _attempt = null; _testRecorder = null; _clock = () => Time.realtimeSinceStartupAsDouble; }

        public static void BeginSingle(int level, int slots, int fruits, LevelEntryMode entry, int guesses)
        {
            Abandon("restart");
            _attempt = new Attempt { Mode = "single", Level = level, Slots = slots, Fruits = fruits,
                Entry = entry.ToString().ToLowerInvariant(), Guesses = guesses, Started = _clock() };
            EmitAttempt("level_started");
        }
        public static void BeginDuel(int round, MultiplayerSettings settings)
        {
            Abandon("rematch");
            _attempt = new Attempt { Mode = "duel", Slots = settings.BoardCount, Fruits = settings.FruitCount,
                Difficulty = settings.Difficulty.ToString().ToLowerInvariant(), Round = round, Started = _clock() };
            EmitAttempt("duel_started");
        }
        public static void GuessAccepted(int guesses) { if (_attempt != null) _attempt.Guesses = guesses; }
        public static void Finish(string outcome, int guesses, int coins = 0, int crystals = 0)
        {
            if (_attempt == null) return;
            _attempt.Guesses = guesses;
            EmitAttempt(_attempt.Mode == "single" ? "level_finished" : "duel_finished", outcome, coins, crystals);
            _attempt = null;
        }
        public static void Abandon(string reason)
        {
            if (_attempt == null) return;
            EmitAttempt(_attempt.Mode == "single" ? "level_abandoned" : "duel_abandoned", reason);
            _attempt = null;
        }
        public static void Pause(bool paused)
        {
            if (_attempt == null) return;
            if (paused && _attempt.PausedAt < 0) _attempt.PausedAt = _clock();
            else if (!paused && _attempt.PausedAt >= 0)
            {
                _attempt.PausedSeconds += _clock() - _attempt.PausedAt; _attempt.PausedAt = -1;
            }
        }
        private static void EmitAttempt(string action, string outcome = null, int coins = 0, int crystals = 0)
        {
            var a = _attempt;
            var data = Context(action);
            data["flMode"] = a.Mode; data["flLevel"] = a.Level; data["flSlots"] = a.Slots;
            data["flFruits"] = a.Fruits; data["flGuesses"] = a.Guesses;
            double end = a.PausedAt < 0 ? _clock() : a.PausedAt;
            data["flDuration"] = Math.Max(0, end - a.Started - a.PausedSeconds);
            if (a.Difficulty != null) data["flDifficulty"] = a.Difficulty;
            if (a.Entry != null) data["flEntry"] = a.Entry;
            if (a.Mode == "duel") data["flRound"] = a.Round;
            if (outcome != null) data["flOutcome"] = outcome;
            if (action == "level_finished") { data["flCoins"] = coins; data["flCrystals"] = crystals; }
            Send(data);
        }
        public static void Tutorial(string action, string tutorial, int step = -1)
        {
            var data = Context(action); data["flTutorial"] = tutorial;
            if (step >= 0) data["flStep"] = step;
            Send(data);
        }
        public static void Powerup(RewardType type, bool earned, string source)
        {
            var data = Context(earned ? "powerup_earned" : "powerup_used");
            data["flPowerup"] = type.ToString().ToLowerInvariant(); data["flSource"] = source;
            Send(data);
        }
        public static void Connection(string operation, string outcome, Difficulty difficulty)
        {
            var data = Context("duel_connection"); data["flOperation"] = operation;
            data["flOutcome"] = outcome; data["flDifficulty"] = difficulty.ToString().ToLowerInvariant();
            Send(data);
        }
        public static void SessionStarted() => Send(Context("session_started"));
        private static Dictionary<string, object> Context(string action)
        {
            var data = new Dictionary<string, object> { ["flAction"] = action };
            if (_attempt != null) { data["flMode"] = _attempt.Mode; data["flLevel"] = _attempt.Level; }
            return data;
        }
        private static void Send(Dictionary<string, object> data)
        {
            if (_testRecorder != null) { _testRecorder(data); return; }
            TelemetryRuntime.Instance?.Record(data);
        }
#if UNITY_EDITOR
        public static IDisposable CaptureForTests(Action<Dictionary<string, object>> recorder, Func<double> clock)
        {
            var previous = _testRecorder; var previousClock = _clock; var previousAttempt = _attempt;
            _testRecorder = recorder; _clock = clock; _attempt = null;
            return new Restore(() => { _testRecorder = previous; _clock = previousClock; _attempt = previousAttempt; });
        }
        private sealed class Restore : IDisposable
        {
            private Action _restore;
            public Restore(Action restore) => _restore = restore;
            public void Dispose() { _restore?.Invoke(); _restore = null; }
        }
#endif
    }
}

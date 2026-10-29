using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public static class TutorialProgress
    {
        public const string First = "first_level_tutorial_completed";
        public const string LegacySkipped = "first_level_tutorial_skipped";
        public const string Bomb = "tutorial_bomb_seen";
        public const string Reveal = "tutorial_reveal_seen";
        public const string Life = "tutorial_life_seen";
        public const string Currency = "tutorial_currency_seen";
        public const string CrystalHints = "tutorial_crystal_hints_seen";
        public static readonly string[] Keys = { First, LegacySkipped, Bomb, Reveal, Life, Currency, CrystalHints };
#if UNITY_EDITOR
        private static IDictionary<string, int> _testValues;
        public static IDisposable UseTestValues(IDictionary<string, int> values)
        {
            var previous = _testValues; _testValues = values;
            return new Restore(() => _testValues = previous);
        }
        private sealed class Restore : IDisposable
        {
            private Action _restore;
            public Restore(Action restore) => _restore = restore;
            public void Dispose() { _restore?.Invoke(); _restore = null; }
        }
#endif
        public static bool Seen(string key)
        {
#if UNITY_EDITOR
            if (_testValues != null) return _testValues.TryGetValue(key, out int value) && value != 0;
#endif
            return PlayerPrefs.GetInt(key, 0) != 0;
        }
        public static void Mark(string key)
        {
#if UNITY_EDITOR
            if (_testValues != null) { _testValues[key] = 1; return; }
#endif
            PlayerPrefs.SetInt(key, 1); PlayerPrefs.Save();
        }
        public static void CompleteFirst() => Mark(First);
        public static void RepairIncompleteFirstLevel(int levelId)
        {
            if (levelId != 0) return;
#if UNITY_EDITOR
            if (_testValues != null) { _testValues.Remove(First); _testValues.Remove(LegacySkipped); return; }
#endif
            if (!PlayerPrefs.HasKey(First) && !PlayerPrefs.HasKey(LegacySkipped)) return;
            PlayerPrefs.DeleteKey(First); PlayerPrefs.DeleteKey(LegacySkipped); PlayerPrefs.Save();
        }
    }
    public static class TutorialObjectUtility
    {
        public static void Destroy(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

using System;
using System.Collections.Generic;
using Game;
using Newtonsoft.Json;
using UnityEngine;

namespace Scripts
{
    [Serializable]
    public sealed class LevelPlaytestSettings
    {
        public bool Enabled;
        public int StartLevel = 5;
        public int Revision = 1;
        public List<LevelData> Cycle = new List<LevelData>();
    }

    // Temporary content policy, separate from the ordinary level tables.
    public static class LevelPlaytestSchedule
    {
        private static LevelPlaytestSettings _settings;
        public static LevelPlaytestSettings Settings
        {
            get
            {
                if (_settings != null) return _settings;
                var asset = Resources.Load<TextAsset>("LevelPlaytestConfig");
                _settings = asset == null ? new LevelPlaytestSettings()
                    : JsonConvert.DeserializeObject<LevelPlaytestSettings>(asset.text);
                if (_settings == null || _settings.StartLevel < 2 || _settings.Revision < 1 || _settings.Cycle == null
                    || (_settings.Enabled && _settings.Cycle.Count == 0))
                    throw new InvalidOperationException("Invalid level playtest configuration.");
                foreach (var step in _settings.Cycle)
                    if (step.NumOfBoardHolders < 2 || step.NumOfBoardHolders > 5 || step.NumOfCards < 5 || step.NumOfCards > 9
                        || step.NumOfBoardHolders > step.NumOfCards || step.MaxNumOfTries < 4
                        || LevelMechanicRules.Normalize(step.Mechanic) != step.Mechanic
                        || (step.Mechanic == LevelMechanic.Split && step.NumOfBoardHolders < 4)
                        || (step.Mechanic==LevelMechanic.Ice && step.MaxNumOfTries%2!=1)
                        || (step.Mechanic == LevelMechanic.Growing && step.NumOfBoardHolders != GrowingBoardState.InitialCount)
                        || (step.BonusReward.HasValue && !Enum.IsDefined(typeof(RewardType),step.BonusReward.Value))
                        || (step.TrialPowerUp.HasValue && !Enum.IsDefined(typeof(RewardType), step.TrialPowerUp.Value)))
                        throw new InvalidOperationException("Invalid step in level playtest cycle.");
                return _settings;
            }
        }

        public static bool IsActive(int levelId) => Settings.Enabled && levelId >= Settings.StartLevel - 1;

        public static bool TryCreate(int levelId, out LevelData definition)
        {
            definition = null;
            if (!IsActive(levelId)) return false;
            int step = (levelId - (Settings.StartLevel - 1)) % Settings.Cycle.Count;
            definition = Settings.Cycle[step].Copy();
            definition.LevelId = levelId;
            definition.PlaytestRevision = Settings.Revision;
            definition.PlaytestStep = step + 1;
            definition.PlaytestStepCount = Settings.Cycle.Count;
            return true;
        }

        public static bool CanResume(int levelId, LevelData saved)
        {
            if (!TryCreate(levelId, out var expected)) return saved == null || !saved.IsPlaytest;
            // Appending a trial must not discard a compatible in-progress round.
            // Validate its original cycle position and definition, then finish it.
            if (saved == null || saved.PlaytestRevision != expected.PlaytestRevision || saved.PlaytestStepCount < 1
                || saved.PlaytestStepCount > Settings.Cycle.Count || saved.PlaytestStep < 1 || saved.PlaytestStep > saved.PlaytestStepCount
                || saved.PlaytestStep != (levelId-(Settings.StartLevel-1))%saved.PlaytestStepCount+1) return false;
            var original = Settings.Cycle[saved.PlaytestStep-1];
            return saved.NumOfBoardHolders == original.NumOfBoardHolders && saved.NumOfCards == original.NumOfCards
                && saved.MaxNumOfTries == original.MaxNumOfTries && saved.Mechanic == original.Mechanic
                && saved.TrialPowerUp == original.TrialPowerUp && saved.BonusReward == original.BonusReward;
        }

        public static void PrepareTrialStock(ILevelTracker tracker, ILevelSaveDataManager saves, LevelData definition)
        {
            if (tracker.GetGameOption() != GameOption.SinglePlayer || !definition.IsPlaytest || !definition.TrialPowerUp.HasValue) return;
            var round = saves.GetLevelSaveData();
            if (round.TrialStockPrepared) return;
            tracker.EnsurePlaytestPowerUp(definition.TrialPowerUp.Value);
            round.TrialStockPrepared = true;
        }

        public static string Status(LevelData definition)
        {
            string content = definition.Mechanic == LevelMechanic.Helper ? "Bonus İpucu"
                : definition.Mechanic == LevelMechanic.Pair ? "Ardışık Bağlı Meyveler"
                : definition.Mechanic == LevelMechanic.Ice ? "Buzlu Tahta"
                : definition.Mechanic == LevelMechanic.Dynamite ? "Dinamitli Tahta"
                : definition.Mechanic == LevelMechanic.Growing ? "Büyüyen Tahta"
                : definition.Mechanic == LevelMechanic.Split ? "Bölünmüş Tahta" : "Normal bölüm";
            if (definition.TrialPowerUp.HasValue)
            {
                var type = definition.TrialPowerUp.Value;
                string instruction = type == RewardType.Bomb ? "ilk tahminden önce"
                    : type == RewardType.Life ? PowerUpDefinition.Get(type).ExtraGuesses+" hak harcadıktan sonra"
                    : type == RewardType.Helper ? "bir tahminden sonra" : "denemeye hazır";
                content = PowerUpTypeUtility.Name(type) + ": " + instruction;
            }
            return "Test " + definition.PlaytestStep + "/" + definition.PlaytestStepCount + " · " + content;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => _settings = null;
#if UNITY_EDITOR
        public static IDisposable UseForTests(LevelPlaytestSettings settings)
        {
            var previous = _settings; _settings = settings;
            return new Restore(() => _settings = previous);
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

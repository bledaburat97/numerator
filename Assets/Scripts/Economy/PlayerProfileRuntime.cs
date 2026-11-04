using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Scripts.Economy
{
    // Unity composition boundary; profile and inventory rules do not depend on Unity.
    public static class PlayerProfileRuntime
    {
        private static IPlayerProfileRepository _persistent;
#if UNITY_EDITOR
        private static IPlayerProfileRepository _test;
#endif
        public static IPlayerProfileRepository Current
        {
            get
            {
#if UNITY_EDITOR
                if(_test!=null)return _test;
#endif
                if(_persistent==null)
                {
                    var storage=new AtomicPlayerProfileStorage(Path.Combine(Application.persistentDataPath,"player-profile.json"),Debug.LogWarning);
                    var repository=new PlayerProfileRepository(storage,ReadLegacy,observerError:Debug.LogException);
                    repository.Changed+=()=>MirrorLegacy(repository.Snapshot());
                    _persistent=repository;
                }
                return _persistent;
            }
        }
        public static PlayerProfileData ReadLegacy()=>LegacyProfileMigration.Create(PlayerPrefs.GetInt);
        public static EconomySettings LoadSettings()
        {
            var asset=Resources.Load<TextAsset>("EconomyConfig");
            if(asset==null)throw new InvalidOperationException("Missing EconomyConfig.json.");
            var settings=JsonConvert.DeserializeObject<EconomySettings>(asset.text);
            if(settings==null)throw new InvalidOperationException("Invalid EconomyConfig.json.");
            settings.Validate();return settings;
        }

        public static void MirrorLegacy(PlayerProfileData profile)
        {
#if UNITY_EDITOR
            if(_test!=null)return;
#endif
            // Compatibility mirror only. The committed profile is always authoritative.
            try
            {
                PlayerPrefs.SetInt("level_id",profile.Progress.LevelId);
                PlayerPrefs.SetInt("star_count",profile.Wallet.Coins);
                PlayerPrefs.SetInt("gift_star_count",profile.Wallet.CrystalProgress);
                PlayerPrefs.SetInt("reward_type",(int)profile.Wallet.NextReward);
                foreach(var power in profile.Wallet.PowerUps)PlayerPrefs.SetInt(LegacyProfileMigration.PowerUpKey(power.Key),power.Value);
                foreach(var type in profile.Wallet.IntroductionGrants)PlayerPrefs.SetInt(LegacyProfileMigration.IntroductionKey(type),1);
                PlayerPrefs.Save();
            }
            catch(Exception error) { Debug.LogWarning("Legacy profile mirror could not be saved: "+error.Message); }
        }

#if UNITY_EDITOR
        // Explicit editor audits use a disposable in-memory profile, never the player's file.
        public static IDisposable UseForTests(IPlayerProfileRepository repository=null)
        {
            var previous=_test;
            _test=repository ?? new PlayerProfileRepository(new MemoryPlayerProfileStorage(),ReadLegacy);
            return new TestScope(()=>_test=previous);
        }
        private sealed class TestScope : IDisposable
        {
            private Action _restore;
            public TestScope(Action restore) { _restore=restore; }
            public void Dispose() { var restore=_restore;_restore=null;restore?.Invoke(); }
        }
#endif
    }
}

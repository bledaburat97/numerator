using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Game;
using Newtonsoft.Json;
using Scripts;
using Scripts.Economy;
using UnityEngine;
using Zenject;
using Object=UnityEngine.Object;

public static class FruitLabEconomyEditorTools
{
    public static string ReviewFoundation()
    {
        string folder="Library/FruitLabEditor/EconomyTests/"+Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(folder);
        var checks=new List<string>();
        string before=LegacySnapshot();
        string persistentBefore=PersistentFiles();
        using(GameTelemetry.CaptureForTests(_=>{},()=>0))
        {
            MigrationAndInventory(folder,checks);
            AtomicityAndRecovery(folder,checks);
            TrackerCompatibility(folder,checks);
            ValidateGlobalBindings(checks);
            var settings=PlayerProfileRuntime.LoadSettings();
            Require(settings.SeedUnitCoinPrice==1 && settings.RequiredGrowthVisits==4,"Central initial economy settings");
            Expect<InvalidOperationException>(()=>new EconomySettings {SeedUnitCoinPrice=0}.Validate());
            checks.Add("Central settings validated; automatic garden processing is not connected");
        }
        Require(before==LegacySnapshot(),"Real PlayerPrefs are unchanged");
        Require(persistentBefore==PersistentFiles(),"Real profile files are unchanged");
        checks.Add("Real PlayerPrefs and the player's persistent profile were not written");
        string report=JsonConvert.SerializeObject(new {passed=true,checks,artifacts=folder},Formatting.Indented);
        File.WriteAllText("Library/FruitLabEditor/economy-foundation-review.json",report);
        return report;
    }

    private static void MigrationAndInventory(string folder,List<string> checks)
    {
        string path=Path.Combine(folder,"inventory.json");
        var legacy=new Dictionary<string,int> {{"level_id",24},{"star_count",137},{"gift_star_count",5},{"reward_type",(int)RewardType.Pair}};
        foreach(RewardType type in Enum.GetValues(typeof(RewardType)))legacy[LegacyProfileMigration.PowerUpKey(type)]=(int)type+2;
        legacy[LegacyProfileMigration.IntroductionKey(RewardType.Bomb)]=1;
        var seed=LegacyProfileMigration.Create((key,fallback)=>legacy.TryGetValue(key,out int value)?value:fallback);
        var repository=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>seed.Copy());
        var migrated=repository.Snapshot();
        Require(migrated.Progress.LevelId==24 && migrated.Wallet.Coins==137 && migrated.Wallet.CrystalProgress==5
            && migrated.Wallet.NextReward==RewardType.Pair && migrated.Wallet.IntroductionGrants.Contains(RewardType.Bomb),"Migration preserves level, coins, crystals, reward order and introduction grants");
        foreach(RewardType type in Enum.GetValues(typeof(RewardType)))Require(migrated.Wallet.PowerUps[type]==(int)type+2,"All six existing powerup stocks survive migration");
        Require(migrated.Collection.Count==9 && migrated.Collection.Values.All(fruit=>fruit.Current==0 && fruit.LifetimeEarned==0),"All nine fruits start at zero; past wins are not invented");
        var sanitized=LegacyProfileMigration.Create((key,fallback)=>key=="reward_type"?999:-2);
        PlayerProfileValidation.Validate(sanitized);
        Require(sanitized.Wallet.Coins==0 && sanitized.Wallet.PowerUps.Values.All(amount=>amount==0) && sanitized.Wallet.NextReward==RewardType.Revealing,"Invalid legacy values are sanitized");
        checks.Add("One-time legacy migration preserves progress, all six powerups, crystals, reward order and introduction flags; nine zero fruit balances");

        var inventory=new FruitInventoryService(repository);
        Require(inventory.Grant("single:round-a",FruitGainSource.SinglePlayer,Amounts(2,3))==ProfileCommitResult.Applied,"Game grant");
        Require(inventory.Grant("harvest:batch-a",FruitGainSource.Garden,Amounts(3,4))==ProfileCommitResult.Applied,"Garden grant");
        Require(inventory.Grant("duel:match-a:round-1",FruitGainSource.Duel,Amounts(1,0))==ProfileCommitResult.Applied,"Duel grant");
        long revision=repository.Snapshot().Revision;
        Require(inventory.Grant("single:round-a",FruitGainSource.SinglePlayer,Amounts(2,3))==ProfileCommitResult.AlreadyApplied
            && repository.Snapshot().Revision==revision,"A repeated result cannot grant twice or write another revision");
        Expect<InvalidOperationException>(()=>inventory.Grant("single:round-a",FruitGainSource.SinglePlayer,Amounts(3,3)));
        var apple=inventory.GetCollection()[FruitId.Apple];
        Require(apple.Current==6 && apple.LifetimeEarned==6 && apple.EarnedBySource[FruitGainSource.SinglePlayer]==2
            && apple.EarnedBySource[FruitGainSource.Garden]==3 && apple.EarnedBySource[FruitGainSource.Duel]==1,"Sources share one inventory and remain distinguishable");
        Require(inventory.Spend("spend:one",Amounts(2,0))==ProfileCommitResult.Applied,"Inventory spend");
        apple=inventory.GetCollection()[FruitId.Apple];
        Require(apple.Current==4 && apple.LifetimeEarned==6,"Spending never decreases lifetime/source totals");
        Require(inventory.Spend("spend:too-many",Amounts(1000,0))==ProfileCommitResult.Rejected
            && !repository.Snapshot().Transactions.ContainsKey("spend:too-many"),"Rejected operations do not spend or create successful receipts");
        Expect<ArgumentOutOfRangeException>(()=>inventory.Grant("bad",FruitGainSource.Garden,new Dictionary<FruitId,long>{{FruitId.Apple,-1}}));
        var external=repository.Snapshot();external.Wallet.Coins=0;external.Collection[FruitId.Apple].Current=0;
        Require(repository.Snapshot().Wallet.Coins==137 && inventory.GetCollection()[FruitId.Apple].Current==4,"Readers cannot mutate committed data through snapshots");
        var reloaded=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("Must not migrate twice"));
        var again=new FruitInventoryService(reloaded);
        Require(again.GetCollection()[FruitId.Apple].Current==4 && again.Grant("single:round-a",FruitGainSource.SinglePlayer,Amounts(2,3))==ProfileCommitResult.AlreadyApplied,"Balances and duplicate protection survive a fresh repository");
        checks.Add("Shared fruit inventory: game/duel/garden sources, current/lifetime totals, spend, rejection, snapshot isolation, duplicate ids and restart");

        repository.Commit("batch-data","foundation_test","batch-and-achievement",profile=>{
            profile.Garden.Add("strawberry-yesterday",new PlantingBatch {Fruit=FruitId.Strawberry,Quantity=1000,PlantedUtcDay=20000,LastGrowthUtcDay=20001,Stage=GardenGrowthStage.Sprout});
            profile.Garden.Add("strawberry-today",new PlantingBatch {Fruit=FruitId.Strawberry,Quantity=10,PlantedUtcDay=20001,LastGrowthUtcDay=20001,Stage=GardenGrowthStage.Seed});
            profile.Achievements.Add("first-harvest",new AchievementProgress {Progress=1,Unlocked=true,UnlockedAtUtc=DateTime.UtcNow,RewardGranted=true,RewardTransactionId="batch-data"});
            return true;
        });
        var saved=new AtomicPlayerProfileStorage(path).Load();
        Require(saved.Garden.Count==2 && saved.Garden["strawberry-yesterday"].Quantity==1000
            && saved.Garden["strawberry-today"].Stage==GardenGrowthStage.Seed && saved.Achievements["first-harvest"].RewardGranted,"Quantity batches and achievement reward receipts persist");
        Expect<InvalidOperationException>(()=>repository.Update(_=>repository.Update(profile=>profile.Wallet.Coins++)));
        var notifications=new List<Exception>();
        var notificationRepository=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>seed.Copy(),observerError:notifications.Add);
        notificationRepository.Changed+=()=>throw new Exception("Broken UI observer");
        Require(notificationRepository.Commit("notify","test","one",profile=>{profile.Wallet.Coins++;return true;})==ProfileCommitResult.Applied
            && notifications.Count==1 && notificationRepository.Snapshot().Transactions.ContainsKey("notify"),"Observer errors do not turn a committed transaction into a failed award");
        checks.Add("Independent quantity batches, achievement reward receipt persistence, nested transaction rejection and observer error isolation");
    }

    private static void AtomicityAndRecovery(string folder,List<string> checks)
    {
        string path=Path.Combine(folder,"atomic.json");
        var initial=new PlayerProfileData();initial.Wallet.Coins=100;
        var good=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>initial.Copy());good.Snapshot();
        bool fail=true;
        var storage=new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("Simulated interruption before atomic publish");});
        var repository=new PlayerProfileRepository(storage,()=>initial.Copy());
        Func<PlayerProfileData,bool> joint=profile=>{
            profile.Wallet.Coins-=10;
            profile.Garden.Add("ten-seeds",new PlantingBatch {Fruit=FruitId.Strawberry,Quantity=10,PlantedUtcDay=20001,LastGrowthUtcDay=20001});
            return true;
        };
        Expect<IOException>(()=>repository.Commit("purchase:one","foundation_test","ten-seeds-for-ten-coins",joint));
        Require(repository.Snapshot().Wallet.Coins==100 && repository.Snapshot().Garden.Count==0
            && new AtomicPlayerProfileStorage(path).Load().Wallet.Coins==100,"A failed write leaves both memory and disk unchanged");
        fail=false;
        Require(repository.Commit("purchase:one","foundation_test","ten-seeds-for-ten-coins",joint)==ProfileCommitResult.Applied,"An interrupted transaction can be retried");
        var loaded=new AtomicPlayerProfileStorage(path).Load();
        Require(loaded.Wallet.Coins==90 && loaded.Garden["ten-seeds"].Quantity==10 && loaded.Transactions.ContainsKey("purchase:one"),"Coins, seeds and receipt are published together");
        Require(repository.Commit("purchase:one","foundation_test","ten-seeds-for-ten-coins",joint)==ProfileCommitResult.AlreadyApplied,"Immediate duplicate retry is harmless");
        File.WriteAllText(path+".interrupted.pending",Encode(initial));
        Require(new AtomicPlayerProfileStorage(path).Load().Wallet.Coins==90,"Unpublished temporary data is ignored");
        var saved=repository.Snapshot();
        Expect<InvalidDataException>(()=>repository.Update(profile=>profile.Wallet.Coins=-1));
        Require(repository.Snapshot().Revision==saved.Revision,"Invalid state cannot replace valid data");
        checks.Add("Real file I/O: interrupted write, safe retry, atomic coin/batch/receipt commit, duplicate retry, orphan temporary file and invalid balance rejection");

        File.WriteAllText(path,"truncated");int recoveries=0;
        var recovered=new AtomicPlayerProfileStorage(path,_=>recoveries++).Load();
        Require(recovered.Wallet.Coins==100 && recovered.Garden.Count==0 && recoveries==1,"Corrupt primary recovers the previous valid complete snapshot");
        File.WriteAllText(path+".backup","also truncated");
        Expect<InvalidDataException>(()=>new AtomicPlayerProfileStorage(path).Load());
        Require(File.ReadAllText(path)=="truncated","Unreadable data is preserved rather than silently reset");
        var future=initial.Copy();future.Version=99;File.WriteAllText(path,Encode(future));
        File.WriteAllText(path+".backup",Encode(initial));
        Expect<UnsupportedProfileVersionException>(()=>new AtomicPlayerProfileStorage(path).Load());
        checks.Add("Checksum validation, last-valid backup recovery, fail-closed corrupt saves and future-version protection");
    }

    private static void TrackerCompatibility(string folder,List<string> checks)
    {
        string path=Path.Combine(folder,"tracker.json");bool fail=false;
        var initial=new PlayerProfileData();initial.Progress.LevelId=24;initial.Wallet.Coins=50;initial.Wallet.CrystalProgress=5;
        foreach(RewardType type in Enum.GetValues(typeof(RewardType)))initial.Wallet.PowerUps[type]=2;
        var repository=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("write failed");}),()=>initial.Copy());
        using(PlayerProfileRuntime.UseForTests(repository))
        {
            var root=new GameObject("EconomyFoundationTracker");root.SetActive(false);
            try
            {
                var tracker=root.AddComponent<LevelTracker>();
                typeof(LevelTracker).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tracker,null);
                Require(tracker.GetCoinCount()==50 && tracker.GetCrystalProgressCount()==5 && tracker.GetLevelId()==24,"Tracker reads migrated profile");
                var inventory=new FruitInventoryService(repository);inventory.Grant("collection-before-win",FruitGainSource.Garden,Amounts(3,0));
                tracker.IncrementLevelIdWithRewards(2,1);
                Require(repository.Snapshot().Progress.LevelId==25 && tracker.GetCoinCount()==52 && tracker.GetCrystalProgressCount()==0
                    && repository.Snapshot().Collection[FruitId.Apple].Current==3,"Existing win/crystal conversion preserves collection");
                int stock=tracker.GetPowerUpCount(RewardType.Revealing);
                Require(tracker.TryConsumePowerUp(RewardType.Revealing) && repository.Snapshot().Wallet.PowerUps[RewardType.Revealing]==stock-1,"Existing powerup use saves through the shared profile");
                repository.Update(profile=>{profile.Wallet.Coins-=7;profile.Garden.Add("external-batch",new PlantingBatch {Fruit=FruitId.Apple,Quantity=7});});
                Require(tracker.GetCoinCount()==45,"External transactions refresh the tracker wallet cache");
                tracker.SavePlayerPrefs();
                Require(repository.Snapshot().Garden.Count==1 && repository.Snapshot().Wallet.Coins==45,"Save-on-quit cannot discard external garden changes");
                tracker.GrantIntroducedPowerUps();var introduced=repository.Snapshot();tracker.GrantIntroducedPowerUps();
                Require(repository.Snapshot().Revision==introduced.Revision,"Introduced gifts and grant flags commit together and never repeat");
                fail=true;stock=tracker.GetPowerUpCount(RewardType.Revealing);
                Expect<IOException>(()=>tracker.TryConsumePowerUp(RewardType.Revealing));
                Require(tracker.GetPowerUpCount(RewardType.Revealing)==stock,"A failed save rolls the tracker back to its committed wallet");
                fail=false;
            }
            finally {Object.DestroyImmediate(root);}
        }
        var reloaded=new AtomicPlayerProfileStorage(path).Load();
        Require(reloaded.Progress.LevelId==25 && reloaded.Wallet.Coins==45 && reloaded.Collection[FruitId.Apple].Current==3
            && reloaded.Garden.Count==1,"Existing and new sections survive restart together");
        checks.Add("Real LevelTracker: load, win, crystal conversion, stock use, external wallet updates, save-on-quit, introduction gifts and write-failure rollback");
    }

    private static Dictionary<FruitId,long> Amounts(long apples,long strawberries)
    {
        var result=new Dictionary<FruitId,long>();
        if(apples>0)result.Add(FruitId.Apple,apples);
        if(strawberries>0)result.Add(FruitId.Strawberry,strawberries);
        return result;
    }
    private static void ValidateGlobalBindings(List<string> checks)
    {
        var repository=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>new PlayerProfileData());
        using(PlayerProfileRuntime.UseForTests(repository))
        {
            var root=new GameObject("EconomyBindingAudit");root.SetActive(false);
            try
            {
                var installer=root.AddComponent<GlobalMonoInstaller>();var container=new DiContainer();
                typeof(MonoInstallerBase).GetProperty("Container",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(installer,container);
                installer.InstallBindings();
                Require(ReferenceEquals(repository,container.Resolve<IPlayerProfileRepository>()),"The global installer aliases the same live profile");
                var inventory=container.Resolve<IFruitInventoryService>();
                inventory.Grant("binding-grant",FruitGainSource.SinglePlayer,Amounts(1,0));
                Require(repository.Snapshot().Collection[FruitId.Apple].Current==1
                    && ReferenceEquals(inventory,container.Resolve<IFruitInventoryService>()),"The inventory singleton writes to that shared profile");
                container.Resolve<EconomySettings>().Validate();
            }
            finally {Object.DestroyImmediate(root);}
        }
        checks.Add("Real global installer: shared profile, singleton inventory and central settings resolve together");
    }
    private static string Encode(PlayerProfileData data)
    {
        string payload=JsonConvert.SerializeObject(data);
        using(var sha=SHA256.Create())return JsonConvert.SerializeObject(new {Payload=payload,Checksum=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)))});
    }
    private static string LegacySnapshot()
    {
        var keys=new[]{"level_id","star_count","gift_star_count","reward_type"}.Concat(TutorialProgress.Keys)
            .Concat(Enum.GetValues(typeof(RewardType)).Cast<RewardType>().Select(LegacyProfileMigration.PowerUpKey))
            .Concat(Enum.GetValues(typeof(RewardType)).Cast<RewardType>().Select(LegacyProfileMigration.IntroductionKey));
        return JsonConvert.SerializeObject(new {ints=keys.ToDictionary(key=>key,key=>new {exists=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetInt(key)}),checkpoint=PlayerPrefs.GetString("saved_game_key")});
    }
    private static string PersistentFiles()
    {
        string path=Path.Combine(Application.persistentDataPath,"player-profile.json");
        return JsonConvert.SerializeObject(new[]{path,path+".backup"}.Select(file=>new {
            file,exists=File.Exists(file),contents=File.Exists(file)?Convert.ToBase64String(File.ReadAllBytes(file)):null
        }));
    }
    private static void Expect<T>(Action action) where T:Exception
    {try {action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    private static void Require(bool condition,string message)
    {if(!condition)throw new Exception("Economy foundation: "+message);}
}

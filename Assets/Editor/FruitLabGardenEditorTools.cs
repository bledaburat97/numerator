using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game;
using Menu;
using Newtonsoft.Json;
using Scripts;
using Scripts.Economy;
using UnityEngine;
using UnityEditor.SceneManagement;
using Zenject;
using Object=UnityEngine.Object;

public static class FruitLabGardenEditorTools
{
    public static string ReviewLifecycle()
    {
        string before=PlayerFingerprint();var clock=new Clock();var profiles=Profiles(20);
        var garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));garden.PurchaseSeeds(Id(),FruitId.Apple,5);clock.Day(1);
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");
        var root=new GameObject("GardenProductionComposition");root.SetActive(false);
        using(PlayerProfileRuntime.UseForTests(profiles))
        {
            try
            {
                var container=new DiContainer();var global=root.AddComponent<GlobalMonoInstaller>();
                typeof(MonoInstallerBase).GetProperty("Container",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(global,container);global.InstallBindings();
                container.Rebind<IGardenClock>().FromInstance(clock);
                var tracker=root.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;
                typeof(LevelTracker).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tracker,null);
                container.Rebind<ILevelTracker>().FromInstance(tracker);
                var app=root.AddComponent<GlobalContext>();container.Inject(app);
                typeof(GlobalContext).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);
                Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Sprout,"Production GlobalContext grows a saved garden on startup before its screen exists");
                var installer=scene.GetRootGameObjects().SelectMany(item=>item.GetComponentsInChildren<MenuSceneMonoInstaller>(true)).Single();
                typeof(MonoInstallerBase).GetProperty("Container",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(installer,container);installer.InstallBindings();
                var menuContext=scene.GetRootGameObjects().SelectMany(item=>item.GetComponentsInChildren<MenuSceneContext>(true)).Single();
                container.Inject(menuContext);
                typeof(MenuSceneContext).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(menuContext,null);
                var presenter=container.Resolve<IFruitCollectionPresenter>();
                var menu=scene.GetRootGameObjects().SelectMany(item=>item.GetComponentsInChildren<MenuUIView>(true)).Single();
                Require(menu.GetGardenNoticeView().IsVisible,"Actual menu DI displays the startup notice after it becomes ready");
                long revision=profiles.Snapshot().Revision;menu.GetGardenNoticeView().Hide();presenter.ShowPendingNotice();
                Require(!menu.GetGardenNoticeView().IsVisible && profiles.Snapshot().Revision==revision,"Menu readiness and view construction never become another visit");
                foreach(var disposable in container.ResolveAll<IDisposable>())disposable.Dispose();
            }
            finally {Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
        }

        string path="Library/FruitLabEditor/GardenTests/lifecycle-"+Id()+".json";bool fail=false;
        clock=new Clock();profiles=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("visit save failed");}),()=>Initial(20));
        garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));garden.PurchaseSeeds(Id(),FruitId.Kiwi,5);clock.Day(1);
        var coordinator=new GardenVisitCoordinator(garden);int failures=0;coordinator.Failed+=_=>failures++;
        fail=true;coordinator.Initialize();
        Require(failures==1 && coordinator.TakeNotice()==null && garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Seed,"A startup write error is nonfatal and publishes no false progress");
        fail=false;coordinator.SetPaused(true);coordinator.SetPaused(false);
        Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Sprout && coordinator.TakeNotice()!=null,"The next foreground retries the uncommitted startup visit");
        coordinator.SetFocused(false);coordinator.SetPaused(true);coordinator.SetFocused(true);coordinator.SetPaused(false);
        Require(coordinator.TakeNotice()==null,"Paired lifecycle events and same-day returns never repeat the notice");
        clock=new Clock();var backgroundProfiles=Profiles(10);var backgroundGarden=new GardenService(backgroundProfiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        backgroundGarden.PurchaseSeeds(Id(),FruitId.Banana,5);clock.Day(1);
        var backgroundStartup=new GardenVisitCoordinator(backgroundGarden);backgroundStartup.SetPaused(true);backgroundStartup.Initialize();
        Require(backgroundGarden.GetBatches().Single().Value.Stage==GardenGrowthStage.Seed,"A startup still in background waits for foreground");
        backgroundStartup.SetPaused(false);
        Require(backgroundGarden.GetBatches().Single().Value.Stage==GardenGrowthStage.Sprout,"First foreground completes a backgrounded startup visit");
        Require(before==PlayerFingerprint(),"Lifecycle checks preserve real player data");
        string report=JsonConvert.SerializeObject(new {passed=true,production_global_and_menu_di=true,startup_growth=true,
            startup_notice=true,safe_failure_retry=true,paired_events_once=true,real_player_unchanged=true},Formatting.Indented);
        File.WriteAllText("Library/FruitLabEditor/garden-lifecycle-review.json",report);return report;
    }

    public static string Review()
    {
        string folder="Library/FruitLabEditor/GardenTests/"+Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(folder);
        string before=PlayerFingerprint();
        var checks=new List<string>();
        PurchaseAndBatches(checks);
        VisitsAndHarvest(checks);
        LimitsAndClock(checks);
        DurableFailureAndRetry(folder,checks);
        SharedProfileBindings(checks);
        Require(before==PlayerFingerprint(),"Real player progress, wallet, notes and profile files are untouched");
        checks.Add("Real player data unchanged; domain rules tested independently from application callbacks");
        string report=JsonConvert.SerializeObject(new {passed=true,checks,artifacts=folder},Formatting.Indented);
        File.WriteAllText("Library/FruitLabEditor/garden-review.json",report);
        return report;
    }

    private static void PurchaseAndBatches(List<string> checks)
    {
        var clock=new Clock();var settings=new EconomySettings();var profiles=Profiles(100);
        var garden=new GardenService(profiles,settings,clock,new AchievementRules(settings));
        var quote=garden.QuoteSeeds(FruitId.Strawberry,10);
        Require(quote.TotalCoinPrice==2 && quote.SeedsPerPack==5 && quote.CanPurchase,"Two five-seed packs cost two coins");
        string id=Id();var first=garden.PurchaseSeeds(id,FruitId.Strawberry,10);
        Require(first.CoinsAfter==98 && first.CellIndex==0,"First planting uses top-left cell");
        Require(garden.PurchaseSeeds(id.ToUpperInvariant(),FruitId.Strawberry,10).Status==ProfileCommitResult.AlreadyApplied && profiles.Snapshot().Wallet.Coins==98,"Duplicate and mixed-case request debits once");
        var merge=garden.PurchaseSeeds(Id(),FruitId.Strawberry,5);
        Require(first.BatchId==merge.BatchId && garden.GetBatches()[first.BatchId].Quantity==15 && merge.CellIndex==0,"Same-day seeds merge without another cell");
        garden.PurchaseSeeds(Id(),FruitId.Apple,5);long revision=profiles.Snapshot().Revision;
        Require(!garden.ProcessVisit().HasProgress && profiles.Snapshot().Revision==revision,"Planting day is excluded");
        settings.SeedPackCoinPrice=2;clock.Day(1);
        var retry=garden.PurchaseSeeds(id,FruitId.Strawberry,10);
        Require(retry.Status==ProfileCommitResult.AlreadyApplied && retry.PackCoinPrice==1 && retry.CoinsAfter==98 && profiles.Snapshot().Revision==revision,"Receipt preserves original price/day/result across settings changes");
        Expect<InvalidOperationException>(()=>garden.PurchaseSeeds(id,FruitId.Apple,10));
        garden.ProcessVisit();var fresh=garden.PurchaseSeeds(Id(),FruitId.Strawberry,5);
        Require(fresh.BatchId!=first.BatchId && fresh.CellIndex==1,"Different-day planting uses next cell");
        Require(garden.ProcessVisit().IsReplay && garden.GetBatches()[fresh.BatchId].Stage==GardenGrowthStage.Seed,"Reopening on same day does not grow new seeds");
        clock.Day(2);garden.ProcessVisit();var batches=garden.GetBatches();
        Require(batches[first.BatchId].Stage==GardenGrowthStage.Leaves && batches[fresh.BatchId].Stage==GardenGrowthStage.Sprout && batches[fresh.BatchId].CellIndex==1,"Ages advance independently in stable cells");
        batches[first.BatchId].Quantity=999;Require(garden.GetBatches()[first.BatchId].Quantity==15,"Snapshots cannot change committed plants");
        checks.Add("Five-seed packs, stable slots, same-day merge, independent ages, duplicate requests and original price receipts");
    }

    private static void VisitsAndHarvest(List<string> checks)
    {
        var clock=new Clock();var profiles=Profiles(100);var rules=new AchievementRules(new EconomySettings());
        var garden=new GardenService(profiles,new EconomySettings(),clock,rules);
        new FruitInventoryService(profiles,rules).Grant("game-fruit",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,2}});
        var purchase=garden.PurchaseSeeds(Id(),FruitId.Strawberry,10);
        foreach(int day in new[]{1,40,41,42})
        {
            clock.Day(day);var visit=garden.ProcessVisit();
            Require(visit.HasProgress && visit.Harvested.Count==0,"Visit advances one step without collecting");
            long revision=profiles.Snapshot().Revision;garden.ProcessVisit();Require(profiles.Snapshot().Revision==revision,"Repeated same-day return is a no-op");
        }
        Require(garden.GetBatches()[purchase.BatchId].Stage==GardenGrowthStage.Fruit && profiles.Snapshot().Collection[FruitId.Strawberry].Current==2,"Mature crop remains uncollected");
        clock.Day(100);Require(!garden.ProcessVisit().HasProgress && garden.GetBatches().Count==1,"Ripe crop waits indefinitely");
        var harvest=garden.Harvest(purchase.BatchId);var balance=profiles.Snapshot().Collection[FruitId.Strawberry];
        Require(harvest.Status==ProfileCommitResult.Applied && harvest.Quantity==10 && garden.GetBatches().Count==0 && balance.Current==12 && balance.EarnedBySource[FruitGainSource.Garden]==10,"Tap collects the entire batch through shared inventory");
        long saved=profiles.Snapshot().Revision;
        Require(garden.Harvest(purchase.BatchId).Status==ProfileCommitResult.AlreadyApplied && profiles.Snapshot().Revision==saved,"Repeated tap cannot collect twice");
        clock=new Clock();profiles=Profiles(100);garden=new GardenService(profiles,new EconomySettings(),clock,rules);
        var first=garden.PurchaseSeeds(Id(),FruitId.Kiwi,10);clock.Day(1);var second=garden.PurchaseSeeds(Id(),FruitId.Kiwi,5);
        foreach(int day in new[]{2,3,4,5}){clock.Day(day);garden.ProcessVisit();}
        garden.Harvest(first.BatchId);
        Require(garden.GetBatches()[second.BatchId].CellIndex==1,"Harvest never shifts another batch into the emptied slot");
        garden.Harvest(second.BatchId);Require(profiles.Snapshot().Collection[FruitId.Kiwi].Current==15,"Separate batch taps give their quantities exactly once");
        checks.Add("Four visited days without catch-up, mature waiting, manual whole-batch harvest, stable cells and idempotent collection");
    }

    private static void LimitsAndClock(List<string> checks)
    {
        var clock=new Clock();var settings=new EconomySettings();var profiles=Profiles(1);
        var garden=new GardenService(profiles,settings,clock,new AchievementRules(settings));string id=Id();
        Require(garden.PurchaseSeeds(id,FruitId.Banana,10).Failure==SeedPurchaseFailure.InsufficientCoins && profiles.Snapshot().Transactions.Count==0,"Insufficient balance neither debits nor records success");
        profiles.Update(p=>p.Wallet.Coins=2);Require(garden.PurchaseSeeds(id,FruitId.Banana,10).Status==ProfileCommitResult.Applied,"Rejected intent may be retried after funds arrive");
        Require(garden.QuoteSeeds(FruitId.Apple,7).Failure==SeedPurchaseFailure.InvalidQuantity,"Non-pack quantities cannot be bought");
        Expect<ArgumentOutOfRangeException>(()=>garden.PurchaseSeeds(Id(),FruitId.Apple,0));
        Expect<ArgumentOutOfRangeException>(()=>garden.PurchaseSeeds(Id(),(FruitId)999,5));
        Expect<ArgumentException>(()=>garden.PurchaseSeeds("",FruitId.Apple,5));
        settings.SeedPackCoinPrice=int.MaxValue;Expect<ArgumentOutOfRangeException>(()=>garden.QuoteSeeds(FruitId.Apple,long.MaxValue/5*5));
        settings.SeedPackCoinPrice=0;Expect<InvalidOperationException>(()=>garden.QuoteSeeds(FruitId.Apple,5));
        clock=new Clock();profiles=Profiles(int.MaxValue);garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        long amount=(long)int.MaxValue*5;var bulk=garden.PurchaseSeeds(Id(),FruitId.Watermelon,amount);
        Require(bulk.CoinsAfter==0 && garden.GetBatches().Single().Value.Quantity==amount,"Ten billion seeds remain one quantity record");
        for(int day=1;day<=4;day++){clock.Day(day);garden.ProcessVisit();}garden.Harvest(bulk.BatchId);
        Require(profiles.Snapshot().Collection[FruitId.Watermelon].Current==amount,"Bulk manual harvest preserves all quantities");
        clock=new Clock();profiles=Profiles(10);garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        var first=garden.PurchaseSeeds(Id(),FruitId.Apple,5);clock.Day(1);garden.ProcessVisit();clock.Day(0);
        Require(!garden.ProcessVisit().HasProgress,"Clock rollback cannot advance a batch");
        var rollback=garden.PurchaseSeeds(Id(),FruitId.Apple,5);Require(rollback.BatchId!=first.BatchId,"Clock rollback cannot merge new seeds into a sprout");
        clock.Day(1);Require(!garden.ProcessVisit().HasProgress,"Processed date cannot advance again");clock.Day(2);garden.ProcessVisit();
        Require(garden.GetBatches()[rollback.BatchId].Stage==GardenGrowthStage.Sprout,"Rollback seeds grow on a new entry day");
        clock=new Clock {UtcNow=new DateTime(2026,11,5,23,59,59,DateTimeKind.Utc)};
        garden=new GardenService(Profiles(10),new EconomySettings(),clock,new AchievementRules(new EconomySettings()));garden.PurchaseSeeds(Id(),FruitId.Grape,5);
        Require(!garden.ProcessVisit().HasProgress,"Planting day before midnight");clock.UtcNow=clock.UtcNow.AddSeconds(1);Require(garden.ProcessVisit().HasProgress,"UTC boundary starts next entry day");
        clock.UtcNow=DateTime.SpecifyKind(clock.UtcNow,DateTimeKind.Unspecified);Expect<ArgumentException>(()=>garden.ProcessVisit());
        var concurrentProfiles=Profiles(1000);clock=new Clock();garden=new GardenService(concurrentProfiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        id=Id();var statuses=new ProfileCommitResult[8];
        System.Threading.Tasks.Parallel.For(0,8,i=>statuses[i]=garden.PurchaseSeeds(id,FruitId.Apple,100).Status);
        Require(statuses.Count(status=>status==ProfileCommitResult.Applied)==1 && concurrentProfiles.Snapshot().Wallet.Coins==980,"Eight concurrent purchase retries commit once");
        string batch=garden.GetBatches().Single().Key;for(int day=1;day<=4;day++){clock.Day(day);garden.ProcessVisit();}
        System.Threading.Tasks.Parallel.For(0,8,i=>statuses[i]=garden.Harvest(batch).Status);
        Require(statuses.Count(status=>status==ProfileCommitResult.Applied)==1 && concurrentProfiles.Snapshot().Collection[FruitId.Apple].Current==100,"Eight concurrent harvest retries collect once");
        checks.Add("Invalid packs, insufficient funds, checked price arithmetic, huge quantities, UTC/rollback and concurrent purchase/harvest retries");
    }

    private static void DurableFailureAndRetry(string folder,List<string> checks)
    {
        string path=Path.Combine(folder,"garden.json");bool fail=false;var clock=new Clock();var settings=new EconomySettings();
        var profiles=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("interrupted publish");}),()=>Initial(100));
        profiles.Snapshot();var garden=new GardenService(profiles,settings,clock,new AchievementRules(settings));string id=Id();
        fail=true;Expect<IOException>(()=>garden.PurchaseSeeds(id,FruitId.Orange,10));
        Require(profiles.Snapshot().Wallet.Coins==100 && profiles.Snapshot().Garden.Count==0 && Reopen(path).Snapshot().Garden.Count==0,"Failed purchase changes neither memory nor disk");
        fail=false;var purchase=garden.PurchaseSeeds(id,FruitId.Orange,10);
        Require(Reopen(path).Snapshot().Wallet.Coins==98,"Purchase price survives reload");
        for(int day=1;day<=3;day++){clock.Day(day);garden.ProcessVisit();}clock.Day(4);fail=true;
        Expect<IOException>(()=>garden.ProcessVisit());Require(garden.GetBatches()[purchase.BatchId].Stage==GardenGrowthStage.Flower,"Failed growth retains previous stage");
        fail=false;garden.ProcessVisit();byte[] before=File.ReadAllBytes(path);fail=true;
        Expect<IOException>(()=>garden.Harvest(purchase.BatchId));
        Require(before.SequenceEqual(File.ReadAllBytes(path)) && garden.GetBatches()[purchase.BatchId].Stage==GardenGrowthStage.Fruit && profiles.Snapshot().Collection[FruitId.Orange].Current==0,"Failed harvest preserves fruit, cell, inventory and disk atomically");
        fail=false;garden.Harvest(purchase.BatchId);var reopened=Reopen(path);var fresh=new GardenService(reopened,settings,clock,new AchievementRules(settings));
        Require(fresh.Harvest(purchase.BatchId).Status==ProfileCommitResult.AlreadyApplied && reopened.Snapshot().Collection[FruitId.Orange].Current==10 && reopened.Snapshot().Garden.Count==0,"Harvest receipt, inventory and freed cell survive restart and retry");
        Require(!Directory.GetFiles(folder,"*.pending").Any(),"Interrupted writes leave no pending files");
        CapacityAndMigration(checks);
        checks.Add("Real atomic storage: interrupted planting/growth/harvest, reload, receipts, rollback and successful retry");
    }
    private static void CapacityAndMigration(List<string> checks)
    {
        var clock=new Clock();var profiles=Profiles(100);var settings=new EconomySettings();var garden=new GardenService(profiles,settings,clock,new AchievementRules(settings));
        var ids=new List<string>();
        for(int day=0;day<4;day++){clock.Day(day);garden.ProcessVisit();ids.Add(garden.PurchaseSeeds(Id(),FruitId.Apple,5).BatchId);}
        var merged=garden.PurchaseSeeds(Id(),FruitId.Apple,5);Require(merged.BatchId==ids[3] && garden.GetBatches().Count==4,"Full plot still accepts a same-day merge");
        clock.Day(4);garden.ProcessVisit();long revision=profiles.Snapshot().Revision;int coins=profiles.Snapshot().Wallet.Coins;
        Require(garden.PurchaseSeeds(Id(),FruitId.Apple,5).Failure==SeedPurchaseFailure.PlotFull && profiles.Snapshot().Revision==revision && profiles.Snapshot().Wallet.Coins==coins,"Ripe crops occupy their cells; full plot rejects without spending");
        garden.Harvest(ids[0]);var replacement=garden.PurchaseSeeds(Id(),FruitId.Apple,5);
        Require(replacement.CellIndex==0 && garden.GetBatches()[ids[1]].CellIndex==1 && garden.Harvest(ids[0]).Status==ProfileCommitResult.AlreadyApplied,"First free cell is reused; an old tap cannot collect the new occupant");
        var old=Initial(137);old.Version=4;
        for(int i=0;i<7;i++)old.Garden["old:"+i]=new PlantingBatch {Fruit=FruitId.Strawberry,Quantity=i+1,PlantedUtcDay=100+i,LastGrowthUtcDay=110,Stage=GardenGrowthStage.Fruit};
        string purchaseId=Id();old.Transactions["seed:"+purchaseId]=new ProfileTransactionRecord {Kind="seed_purchase",Request=JsonConvert.SerializeObject(new {fruit=FruitId.Apple,quantity=3L}),AppliedAtUtc=DateTime.UtcNow,
            Result=JsonConvert.SerializeObject(new SeedPurchaseResult {Fruit=FruitId.Apple,Quantity=3,UnitCoinPrice=1,TotalCoinPrice=3,CoinsAfter=137,BatchId="already-collected"})};
        var storage=new MemoryPlayerProfileStorage();storage.Save(old);
        var migrated=new PlayerProfileRepository(storage,()=>throw new Exception("Must load old profile"));garden=new GardenService(migrated,settings,clock,new AchievementRules(settings));var data=migrated.Snapshot();
        Require(data.Version==5 && data.Wallet.Coins==137 && data.Garden.Count==7 && data.Garden.Values.Count(batch=>batch.IsLegacyOverflow)==3,"Version 4 keeps balances and all excess batches in a migration queue");
        var replay=garden.PurchaseSeeds(purchaseId,FruitId.Apple,3);
        Require(replay.Status==ProfileCommitResult.AlreadyApplied && replay.UnitCoinPrice==1 && replay.TotalCoinPrice==3 && migrated.Snapshot().Wallet.Coins==137,"Old one-seed-price receipts still replay despite new pack rules");
        garden.Harvest("old:0");data=migrated.Snapshot();
        Require(data.Garden["old:4"].CellIndex==0 && !data.Garden["old:4"].IsLegacyOverflow && data.Garden["old:1"].CellIndex==1 && data.Collection[FruitId.Strawberry].Current==1,"Harvest promotes the oldest waiting legacy batch without moving other cells");
        var restart=new PlayerProfileRepository(storage,()=>throw new Exception("No reset"));Require(restart.Snapshot().Garden["old:4"].CellIndex==0,"Migrated slots survive a fresh repository");
        checks.Add("Four-cell capacity, same-day merge when full, no-spend rejection, stable reuse, version-4 migration, legacy overflow promotion and old receipt compatibility");
    }

    private static void SharedProfileBindings(List<string> checks)
    {
        var profiles=Profiles(20);
        using(PlayerProfileRuntime.UseForTests(profiles))
        {
            var root=new GameObject("GardenServiceBindings");root.SetActive(false);
            try
            {
                var container=new DiContainer();var installer=root.AddComponent<GlobalMonoInstaller>();
                typeof(MonoInstallerBase).GetProperty("Container",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(installer,container,null);installer.InstallBindings();
                Require(container.Resolve<IGardenService>()==container.Resolve<IGardenService>()
                    && ReferenceEquals(container.Resolve<IPlayerProfileRepository>(),profiles),"Garden is a singleton using the common profile");
                Require(container.Resolve<IGardenClock>().UtcNow.Kind==DateTimeKind.Utc,"Production clock is UTC");
                var tracker=root.AddComponent<LevelTracker>();
                typeof(LevelTracker).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(tracker,null);
                var garden=new GardenService(profiles,new EconomySettings(),new Clock(),new AchievementRules(new EconomySettings()));garden.PurchaseSeeds(Id(),FruitId.Blueberry,20);
                Require(tracker.GetCoinCount()==16,"Purchase refreshes the existing LevelTracker coin cache");
                tracker.SavePlayerPrefs();
                Require(profiles.Snapshot().Wallet.Coins==16 && profiles.Snapshot().Garden.Count==1
                    && profiles.Snapshot().Progress.LevelId==17 && profiles.Snapshot().Wallet.CrystalProgress==5
                    && profiles.Snapshot().Wallet.PowerUps.Values.All(n=>n==2),"Existing saves preserve garden, progress, crystal and powerup balances");
            }
            finally {Object.DestroyImmediate(root);}
        }
        checks.Add("Production DI resolves the shared profile/settings/clock; seed purchase refreshes coin HUD data without overwriting old progression or stocks");
    }

    private static PlayerProfileData Initial(int coins)
    {
        var profile=new PlayerProfileData();profile.Wallet.Coins=coins;profile.Progress.LevelId=17;profile.Wallet.CrystalProgress=5;
        foreach(RewardType power in Enum.GetValues(typeof(RewardType)))profile.Wallet.PowerUps[power]=2;
        return profile;
    }
    private static PlayerProfileRepository Profiles(int coins)=>new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>Initial(coins));
    private static PlayerProfileRepository Reopen(string path)=>new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("Existing garden profile must load"));
    private static string Id()=>Guid.NewGuid().ToString("N");
    private sealed class Clock : IGardenClock
    {
        public DateTime UtcNow {get;set;}=new DateTime(2026,11,1,12,0,0,DateTimeKind.Utc);
        public void Day(int day) {UtcNow=new DateTime(2026,11,1,12,0,0,DateTimeKind.Utc).AddDays(day);}
    }
    private static string PlayerFingerprint()
    {
        var keys=new[]{"level_id","star_count","gift_star_count","reward_type"}.Concat(TutorialProgress.Keys)
            .Concat(Enum.GetValues(typeof(RewardType)).Cast<RewardType>().Select(LegacyProfileMigration.PowerUpKey))
            .Concat(Enum.GetValues(typeof(RewardType)).Cast<RewardType>().Select(LegacyProfileMigration.IntroductionKey));
        string path=Path.Combine(Application.persistentDataPath,"player-profile.json");
        return JsonConvert.SerializeObject(new {prefs=keys.ToDictionary(k=>k,k=>new {exists=PlayerPrefs.HasKey(k),value=PlayerPrefs.GetInt(k)}),
            checkpoint=PlayerPrefs.GetString("saved_game_key"),files=new[]{path,path+".backup"}.Select(p=>File.Exists(p)?Convert.ToBase64String(File.ReadAllBytes(p)):null)});
    }
    private static void Expect<T>(Action action) where T:Exception
    {try {action();}catch(T){return;}throw new Exception("Expected "+typeof(T).Name);}
    private static void Require(bool condition,string message)
    {if(!condition)throw new Exception("Garden review: "+message);}
}

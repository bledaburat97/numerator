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
        var garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));garden.PurchaseSeeds(Id(),FruitId.Apple,2);clock.Day(1);
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
        garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));garden.PurchaseSeeds(Id(),FruitId.Kiwi,3);clock.Day(1);
        var coordinator=new GardenVisitCoordinator(garden);int failures=0;coordinator.Failed+=_=>failures++;
        fail=true;coordinator.Initialize();
        Require(failures==1 && coordinator.TakeNotice()==null && garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Seed,"A startup write error is nonfatal and publishes no false progress");
        fail=false;coordinator.SetPaused(true);coordinator.SetPaused(false);
        Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Sprout && coordinator.TakeNotice()!=null,"The next foreground retries the uncommitted startup visit");
        coordinator.SetFocused(false);coordinator.SetPaused(true);coordinator.SetFocused(true);coordinator.SetPaused(false);
        Require(coordinator.TakeNotice()==null,"Paired lifecycle events and same-day returns never repeat the notice");
        clock=new Clock();var backgroundProfiles=Profiles(10);var backgroundGarden=new GardenService(backgroundProfiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        backgroundGarden.PurchaseSeeds(Id(),FruitId.Banana,1);clock.Day(1);
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
        var clock=new Clock();var settings=new EconomySettings();
        var profiles=Profiles(100);var garden=new GardenService(profiles,settings,clock,new AchievementRules(settings));
        var quote=garden.QuoteSeeds(FruitId.Strawberry,10);
        Require(quote.TotalCoinPrice==10 && quote.AvailableCoins==100 && quote.CanAfford,"Central one-coin seed quote");
        string firstId=Id();var first=garden.PurchaseSeeds(firstId,FruitId.Strawberry,10);
        Require(first.Status==ProfileCommitResult.Applied && first.CoinsAfter==90 && garden.GetBatches().Count==1,"Coin debit and one quantity batch");
        Require(garden.PurchaseSeeds(firstId,FruitId.Strawberry,10).Status==ProfileCommitResult.AlreadyApplied
            && profiles.Snapshot().Wallet.Coins==90 && garden.GetBatches()[first.BatchId].Quantity==10,"Immediate duplicate tap cannot buy the same request twice");
        Require(garden.PurchaseSeeds(firstId.ToUpperInvariant(),FruitId.Strawberry,10).Status==ProfileCommitResult.AlreadyApplied,
            "Purchase identity is unchanged by hexadecimal letter case");
        var second=garden.PurchaseSeeds(Id(),FruitId.Strawberry,7);
        Require(first.BatchId==second.BatchId && garden.GetBatches()[first.BatchId].Quantity==17,"Same fruit and purchase day merge");
        garden.PurchaseSeeds(Id(),FruitId.Apple,3);
        Require(garden.GetBatches().Count==2,"Different fruit has a separate batch");
        long revision=profiles.Snapshot().Revision;
        Require(!garden.ProcessVisit().HasProgress && profiles.Snapshot().Revision==revision,"Purchase day does not grow or save a no-op");
        settings.SeedUnitCoinPrice=2;clock.Day(1);
        var retry=garden.PurchaseSeeds(firstId,FruitId.Strawberry,10);
        Require(retry.Status==ProfileCommitResult.AlreadyApplied && retry.UnitCoinPrice==1 && retry.PlantedUtcDay==first.PlantedUtcDay
            && profiles.Snapshot().Revision==revision && garden.GetBatches()[first.BatchId].Quantity==17,"Retry across day/price changes returns the original outcome without charging");
        Expect<InvalidOperationException>(()=>garden.PurchaseSeeds(firstId,FruitId.Apple,10));
        Expect<InvalidOperationException>(()=>garden.PurchaseSeeds(firstId,FruitId.Strawberry,11));
        var advance=garden.ProcessVisit();
        Require(advance.HasProgress && advance.Changes.Count==2 && garden.GetBatches()[first.BatchId].Stage==GardenGrowthStage.Sprout,"Next UTC day advances both batches once");
        var fresh=garden.PurchaseSeeds(Id(),FruitId.Strawberry,5);
        Require(fresh.BatchId!=first.BatchId && garden.GetBatches().Count==3 && garden.GetBatches()[fresh.BatchId].Stage==GardenGrowthStage.Seed,"Different-day seeds remain independent");
        revision=profiles.Snapshot().Revision;
        var duplicate=garden.ProcessVisit();
        Require(duplicate.IsReplay && !duplicate.HasProgress && profiles.Snapshot().Revision==revision
            && garden.GetBatches()[fresh.BatchId].Stage==GardenGrowthStage.Seed,"Same-day reopen does not grow new seeds or repeat welcome effects");
        clock.Day(2);garden.ProcessVisit();
        Require(garden.GetBatches()[first.BatchId].Stage==GardenGrowthStage.Leaves
            && garden.GetBatches()[fresh.BatchId].Stage==GardenGrowthStage.Sprout,"Older and newer batches advance independently");
        var external=garden.GetBatches();external[first.BatchId].Quantity=999;
        Require(garden.GetBatches()[first.BatchId].Quantity==17,"Garden readers cannot mutate durable state");
        checks.Add("Quote, atomic debit, same-day merging, separate fruits/dates, retry after midnight/price changes, conflict rejection and independent batch ages");
    }

    private static void VisitsAndHarvest(List<string> checks)
    {
        var clock=new Clock();var profiles=Profiles(100);var garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        new FruitInventoryService(profiles,new AchievementRules(new EconomySettings())).Grant("preexisting-game-fruit",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,2}});
        var purchase=garden.PurchaseSeeds(Id(),FruitId.Strawberry,10);
        foreach(int day in new[]{1,40,41})
        {
            clock.Day(day);var visit=garden.ProcessVisit();
            Require(visit.HasProgress && visit.Changes.Count==1 && visit.Harvested.Count==0,"A real entry day advances one stage");
            long revision=profiles.Snapshot().Revision;
            Require(!garden.ProcessVisit().HasProgress && profiles.Snapshot().Revision==revision,"Repeated foreground visits on the same day do nothing");
        }
        var batch=garden.GetBatches()[purchase.BatchId];
        Require(batch.Stage==GardenGrowthStage.Flower && batch.LastGrowthUtcDay==purchase.PlantedUtcDay+41,"A long absence counts as one entry, not forty days");
        clock.Day(42);var harvest=garden.ProcessVisit();
        var balance=profiles.Snapshot().Collection[FruitId.Strawberry];
        Require(harvest.HasProgress && harvest.Harvested[FruitId.Strawberry]==10 && harvest.Changes[0].CurrentStage==GardenGrowthStage.Fruit
            && garden.GetBatches().Count==0,"Fourth distinct entry harvests automatically and removes the finished pending batch");
        Require(balance.Current==12 && balance.LifetimeEarned==12 && balance.EarnedBySource[FruitGainSource.Garden]==10
            && balance.EarnedBySource[FruitGainSource.SinglePlayer]==2,"Game and garden fruit share inventory with distinct source totals");
        Require(!garden.ProcessVisit().HasProgress,"Harvest receipt replay has no new notification");
        clock.Day(43);Require(!garden.ProcessVisit().HasProgress && profiles.Snapshot().Collection[FruitId.Strawberry].Current==12,"Harvest cannot happen twice on a later day");

        // Separate planting dates can reach maturity together when neither batch
        // was visited in between. The common inventory receives their total once.
        clock=new Clock();profiles=Profiles(100);garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        garden.PurchaseSeeds(Id(),FruitId.Kiwi,10);clock.Day(1);garden.PurchaseSeeds(Id(),FruitId.Kiwi,7);
        foreach(int day in new[]{2,3,4,5}){clock.Day(day);harvest=garden.ProcessVisit();}
        Require(harvest.Harvested[FruitId.Kiwi]==17 && harvest.Changes.Count==2 && garden.GetBatches().Count==0
            && profiles.Snapshot().Collection[FruitId.Kiwi].LifetimeEarned==17,"Two batches harvest into one fruit total without duplication");
        checks.Add("Four visited UTC days, skipped-day pause, no catch-up, same-day no-op, automatic single harvest, grouped harvest and shared current/lifetime/source accounting");
    }

    private static void LimitsAndClock(List<string> checks)
    {
        var clock=new Clock();var settings=new EconomySettings();var profiles=Profiles(1);
        var garden=new GardenService(profiles,settings,clock,new AchievementRules(settings));string id=Id();
        Require(!garden.QuoteSeeds(FruitId.Banana,2).CanAfford && garden.PurchaseSeeds(id,FruitId.Banana,2).Status==ProfileCommitResult.Rejected
            && profiles.Snapshot().Garden.Count==0 && profiles.Snapshot().Transactions.Count==0,"Insufficient coins create neither batch nor success receipt");
        profiles.Update(p=>p.Wallet.Coins=2);
        Require(garden.PurchaseSeeds(id,FruitId.Banana,2).Status==ProfileCommitResult.Applied,"Rejected purchase may be retried after funds arrive");
        Expect<ArgumentOutOfRangeException>(()=>garden.PurchaseSeeds(Id(),FruitId.Apple,0));
        Expect<ArgumentOutOfRangeException>(()=>garden.PurchaseSeeds(Id(),FruitId.Apple,-1));
        Expect<ArgumentOutOfRangeException>(()=>garden.PurchaseSeeds(Id(),(FruitId)999,1));
        Expect<ArgumentException>(()=>garden.PurchaseSeeds("",FruitId.Apple,1));
        settings.SeedUnitCoinPrice=int.MaxValue;
        Expect<ArgumentOutOfRangeException>(()=>garden.QuoteSeeds(FruitId.Apple,long.MaxValue));
        settings.SeedUnitCoinPrice=0;Expect<InvalidOperationException>(()=>garden.QuoteSeeds(FruitId.Apple,1));

        profiles=Profiles(int.MaxValue);garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        var bulk=garden.PurchaseSeeds(Id(),FruitId.Watermelon,int.MaxValue);
        Require(bulk.CoinsAfter==0 && garden.GetBatches().Count==1 && garden.GetBatches()[bulk.BatchId].Quantity==int.MaxValue,"Very large seed quantities remain a single record");
        for(int day=1;day<=4;day++){clock.Day(day);garden.ProcessVisit();}
        Require(profiles.Snapshot().Collection[FruitId.Watermelon].Current==int.MaxValue,"Bulk harvest preserves the full quantity");

        clock=new Clock();profiles=Profiles(10);garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        var original=garden.PurchaseSeeds(Id(),FruitId.Apple,2);clock.Day(1);garden.ProcessVisit();
        clock.Day(0);Require(!garden.ProcessVisit().HasProgress,"Clock rollback cannot advance a batch");
        var rolledBackPurchase=garden.PurchaseSeeds(Id(),FruitId.Apple,2);
        Require(rolledBackPurchase.BatchId!=original.BatchId,"Rollback purchase cannot merge into a sprout and inherit its age");
        clock.Day(1);Require(!garden.ProcessVisit().HasProgress,"Previously processed day cannot become a second growth day after rollback");
        clock.Day(2);garden.ProcessVisit();
        Require(garden.GetBatches()[rolledBackPurchase.BatchId].Stage==GardenGrowthStage.Sprout,"Rollback seeds eventually grow on a new actual processing day");

        clock=new Clock {UtcNow=new DateTime(2026,11,5,23,59,59,DateTimeKind.Utc)};
        garden=new GardenService(Profiles(10),new EconomySettings(),clock,new AchievementRules(new EconomySettings()));
        garden.PurchaseSeeds(Id(),FruitId.Grape,1);
        Require(!garden.ProcessVisit().HasProgress,"Just before UTC midnight is still the planting day");
        clock.UtcNow=clock.UtcNow.AddSeconds(1);Require(garden.ProcessVisit().HasProgress,"UTC midnight opens the next entry day");
        clock.UtcNow=DateTime.SpecifyKind(clock.UtcNow,DateTimeKind.Unspecified);Expect<ArgumentException>(()=>garden.ProcessVisit());
        clock.UtcNow=new DateTime(1969,12,31,0,0,0,DateTimeKind.Utc);Expect<ArgumentException>(()=>garden.ProcessVisit());
        var concurrentProfiles=Profiles(1000);
        var concurrentGarden=new GardenService(concurrentProfiles,new EconomySettings(),new Clock(),new AchievementRules(new EconomySettings()));
        string concurrentId=Id();var statuses=new ProfileCommitResult[8];
        System.Threading.Tasks.Parallel.For(0,8,index=>statuses[index]=concurrentGarden.PurchaseSeeds(concurrentId,FruitId.Apple,100).Status);
        Require(statuses.Count(status=>status==ProfileCommitResult.Applied)==1
            && statuses.Count(status=>status==ProfileCommitResult.AlreadyApplied)==7
            && concurrentProfiles.Snapshot().Wallet.Coins==900 && concurrentGarden.GetBatches().Single().Value.Quantity==100,"Concurrent retries of one purchase commit exactly once");
        checks.Add("Insufficient funds, invalid input, checked price arithmetic, two-billion-seed single batch, clock rollback, UTC midnight and explicit UTC contract and eight concurrent duplicate purchase attempts");
    }

    private static void DurableFailureAndRetry(string folder,List<string> checks)
    {
        string path=Path.Combine(folder,"garden.json");bool fail=false;
        var clock=new Clock();var settings=new EconomySettings();
        var profiles=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("simulated interrupted publish");}),()=>Initial(100));
        profiles.Snapshot();var garden=new GardenService(profiles,settings,clock,new AchievementRules(settings));string id=Id();
        fail=true;Expect<IOException>(()=>garden.PurchaseSeeds(id,FruitId.Orange,10));
        Require(profiles.Snapshot().Wallet.Coins==100 && profiles.Snapshot().Garden.Count==0 && profiles.Snapshot().Transactions.Count==0,"Failed publish rolls back coins, seeds and receipt in memory");
        var reopened=Reopen(path);Require(reopened.Snapshot().Wallet.Coins==100 && reopened.Snapshot().Garden.Count==0,"Interrupted purchase has no partial durable state");
        fail=false;var purchase=garden.PurchaseSeeds(id,FruitId.Orange,10);
        reopened=Reopen(path);var reopenedGarden=new GardenService(reopened,settings,clock,new AchievementRules(settings));
        Require(reopenedGarden.PurchaseSeeds(id,FruitId.Orange,10).Status==ProfileCommitResult.AlreadyApplied
            && reopened.Snapshot().Wallet.Coins==90 && reopenedGarden.GetBatches()[purchase.BatchId].Quantity==10,"Successful purchase/retry survives process restart");
        for(int day=1;day<=3;day++){clock.Day(day);garden.ProcessVisit();}
        clock.Day(4);fail=true;Expect<IOException>(()=>garden.ProcessVisit());
        Require(garden.GetBatches()[purchase.BatchId].Stage==GardenGrowthStage.Flower
            && profiles.Snapshot().Collection[FruitId.Orange].Current==0,"Failed harvest keeps the pending flowers and grants no inventory");
        reopened=Reopen(path);Require(reopened.Snapshot().Garden[purchase.BatchId].Stage==GardenGrowthStage.Flower
            && reopened.Snapshot().Collection[FruitId.Orange].Current==0,"Failed harvest has no partial state after restart");
        fail=false;var harvest=garden.ProcessVisit();Require(harvest.Harvested[FruitId.Orange]==10,"Failed harvest can be retried");
        reopened=Reopen(path);reopenedGarden=new GardenService(reopened,settings,clock,new AchievementRules(settings));
        var replay=reopenedGarden.ProcessVisit();
        Require(replay.IsReplay && !replay.HasProgress && replay.Harvested[FruitId.Orange]==10 && reopened.Snapshot().Garden.Count==0
            && reopened.Snapshot().Collection[FruitId.Orange].EarnedBySource[FruitGainSource.Garden]==10,"Harvest result, source and once-only protection survive restart");
        Require(!Directory.GetFiles(folder,"*.pending").Any(),"Failed writes clean their unpublished temporary files");
        checks.Add("Real file storage: interrupted purchase/harvest, atomic rollback, durable retry, restart, JSON result replay and no stranded temporary files");
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
                var garden=new GardenService(profiles,new EconomySettings(),new Clock(),new AchievementRules(new EconomySettings()));garden.PurchaseSeeds(Id(),FruitId.Blueberry,4);
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

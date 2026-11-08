using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Game;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Scripts.Economy;

// Domain checks shared by the Unity task runner and standalone compilation.
public static class FruitLabEconomyReleaseChecks
{
    public static IReadOnlyList<EconomyTelemetryEvent> Review(string folder,EconomySettings settings)
    {
        Directory.CreateDirectory(folder);
        var clock=new Clock();bool allowed=false;
        var context=new EconomyTelemetryContext {ConsentScope=Guid.NewGuid().ToString("N"),UtcDay=clock.Day,BuildChannel="beta",Version="1",Build="17"};
        Func<EconomyTelemetryContext> capture=()=>allowed?context:null;
        var journal=new ProfileTelemetryJournal(capture);
        var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>Initial(100),commitPolicy:journal);
        var rules=new AchievementRules(settings);var garden=new GardenService(profiles,settings,clock,rules);
        string beforeConsent=Id();garden.PurchaseSeeds(beforeConsent,FruitId.Apple,1);
        Require(profiles.Snapshot().TelemetryOutbox.Count==0,"No events are collected before consent");
        allowed=true;garden.PurchaseSeeds(beforeConsent,FruitId.Apple,1);
        Require(profiles.Snapshot().TelemetryOutbox.Count==0,"Opt-in never replays old purchases");
        string purchase=Id();garden.PurchaseSeeds(purchase,FruitId.Strawberry,10);garden.PurchaseSeeds(purchase,FruitId.Strawberry,10);
        var data=profiles.Snapshot();var seed=data.TelemetryOutbox.Single().Value;
        Require(seed.Action=="seed_purchased" && seed.Fruit==FruitId.Strawberry && seed.Amount==10 && seed.CoinCost==10 && seed.Balance==89 && !seed.First,"A committed purchase yields one correctly priced event");
        long revision=data.Revision;
        Require(garden.PurchaseSeeds(Id(),FruitId.Kiwi,1000).Status==ProfileCommitResult.Rejected && profiles.Snapshot().Revision==revision,"Insufficient funds creates neither a success event nor a receipt");
        garden.ProcessVisit();Require(profiles.Snapshot().TelemetryOutbox.Count==1,"Planting-day visits do not emit growth");
        foreach(int day in new[]{20001,20025,20026,20027})
        {
            clock.Day=day;context.UtcDay=day;garden.ProcessVisit();revision=profiles.Snapshot().Revision;garden.ProcessVisit();
            Require(profiles.Snapshot().Revision==revision,"Same-day returns never re-emit growth or harvest");
        }
        data=profiles.Snapshot();
        Require(data.Garden.Count==0 && data.Collection[FruitId.Strawberry].Current==10 && data.Wallet.PowerUps[RewardType.Life]==1 && data.Wallet.PowerUps[RewardType.Revealing]==1,"Fourth visited day harvests once and pays the two earned badges");
        var events=data.TelemetryOutbox.Values.ToList();
        Require(events.Count(ev=>ev.Action=="garden_grown")==4 && events.Where(ev=>ev.Action=="garden_grown").All(ev=>ev.Batches==2),"A long absence advances one step and emits one growth observation per visit");
        Require(events.Count(ev=>ev.Action=="garden_harvested")==2 && events.Where(ev=>ev.Action=="garden_harvested").All(ev=>ev.First),"A grouped first harvest emits quantities for both fruit types");
        Require(events.Count(ev=>ev.Action=="achievement_unlocked")==2 && events.Where(ev=>ev.Action=="achievement_unlocked").All(ev=>ev.Source=="garden"),"Achievement events come from actual newly committed reward receipts");
        revision=data.Revision;new AchievementService(profiles,rules).Reconcile();
        Require(profiles.Snapshot().Revision==revision,"Reopening badge evaluation cannot duplicate events or rewards");

        var engagement=new PlayerEngagementService(profiles,capture);int receiptCount=profiles.Snapshot().Transactions.Count;
        for(int i=0;i<5;i++){engagement.Record(PlayerActivity.ActiveDay);engagement.Record(PlayerActivity.SinglePlayed);}
        new PlayerEngagementService(profiles,capture).Record(PlayerActivity.ActiveDay);
        Require(profiles.Snapshot().TelemetryOutbox.Values.Count(ev=>ev.Action=="active_day")==1 && profiles.Snapshot().TelemetryOutbox.Values.Count(ev=>ev.Action=="single_played_day")==1,"Repeated focus callbacks and a fresh service still count one active/played day");
        context.UtcDay++;engagement.Record(PlayerActivity.ActiveDay);
        Require(profiles.Snapshot().TelemetryOutbox.Values.Count(ev=>ev.Action=="active_day")==2 && profiles.Snapshot().Transactions.Count==receiptCount,"Daily checkpoints stay bounded rather than adding daily transaction receipts");
        context.UtcDay--;engagement.Record(PlayerActivity.ActiveDay);
        Require(profiles.Snapshot().TelemetryOutbox.Values.Count(ev=>ev.Action=="active_day")==2,"Clock rollback does not create another activity day");
        events=profiles.Snapshot().TelemetryOutbox.Values.ToList();context.Version="2";context.Build="18";
        Require(events.All(ev=>ev.Version=="1" && ev.Build=="17"),"Queued records retain the originating build and UTC day");
        var delivery=new ProfileTelemetryDelivery(profiles);int count=profiles.Snapshot().TelemetryOutbox.Count;
        Require(delivery.Drain(context.ConsentScope,_=>false)==0 && profiles.Snapshot().TelemetryOutbox.Count==count,"Offline rejection leaves the durable queue intact");
        var sent=new List<string>();delivery.Drain(context.ConsentScope,ev=>{sent.Add(ev.EventId);return true;},256);
        Require(sent.Count==count && sent.Distinct().Count()==count && profiles.Snapshot().TelemetryOutbox.Count==0,"SDK acceptance removes exactly the accepted observations");
        garden.PurchaseSeeds(purchase,FruitId.Strawberry,10);Require(profiles.Snapshot().TelemetryOutbox.Count==0,"Acknowledged transaction replay does not enqueue another purchase");
        engagement.Record(PlayerActivity.DuelPlayed);
        string priorScope=context.ConsentScope;context.ConsentScope=Id();
        sent.Clear();delivery.Drain(context.ConsentScope,ev=>{sent.Add(ev.EventId);return true;});
        Require(sent.Count==0 && profiles.Snapshot().TelemetryOutbox.Count==0,"A new consent scope discards older queued data without sending it");
        allowed=false;garden.PurchaseSeeds(Id(),FruitId.Banana,1);engagement.Record(PlayerActivity.ActiveDay);
        Require(profiles.Snapshot().TelemetryOutbox.Count==0,"Revoked consent collects neither transactions nor activity");
        allowed=true;engagement.Record(PlayerActivity.ActiveDay);
        Require(profiles.Snapshot().TelemetryOutbox.Values.Single().ConsentScope==context.ConsentScope,"Re-enabling analytics starts a fresh consent scope");

        ConcurrentPurchase(settings,folder,events);
        AtomicDelivery(settings,folder,events);
        Migration(folder);
        // Typed SDK encoding must also handle long values beyond Int32.
        var large=events.First(ev=>ev.Action=="seed_purchased");
        large=JsonConvert.DeserializeObject<EconomyTelemetryEvent>(JsonConvert.SerializeObject(large));large.Amount=(long)int.MaxValue+1;events.Add(large);
        return events;
    }
    private static void ConcurrentPurchase(EconomySettings settings,string folder,List<EconomyTelemetryEvent> examples)
    {
        var context=Context();var rules=new AchievementRules(settings);
        var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>Initial(int.MaxValue),commitPolicy:new ProfileTelemetryJournal(()=>context));
        var garden=new GardenService(profiles,settings,new Clock(),rules);string identity=Id();
        var tasks=Enumerable.Range(0,8).Select(_=>Task.Run(()=>garden.PurchaseSeeds(identity,FruitId.Kiwi,1000000))).ToArray();Task.WaitAll(tasks);
        var data=profiles.Snapshot();Require(data.Garden.Count==1 && data.Garden.Single().Value.Quantity==1000000 && data.Wallet.Coins==int.MaxValue-1000000 && data.TelemetryOutbox.Count==1,"Concurrent duplicate and large purchases debit, plant and enqueue once");
        examples.Add(data.TelemetryOutbox.Single().Value);
        for(int day=20000;day<20300;day++){context.UtcDay=day;new PlayerEngagementService(profiles,()=>context).Record(PlayerActivity.ActiveDay);}
        Require(profiles.Snapshot().TelemetryOutbox.Count==ProfileTelemetryJournal.Capacity,"Long offline histories have a bounded telemetry queue");
    }
    private static void AtomicDelivery(EconomySettings settings,string folder,List<EconomyTelemetryEvent> examples)
    {
        var context=Context();bool fail=false;string path=Path.Combine(folder,Id()+".json");
        var storage=new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("Interrupted save");});
        var journal=new ProfileTelemetryJournal(()=>context);
        var profiles=new PlayerProfileRepository(storage,()=>Initial(100),commitPolicy:journal);var rules=new AchievementRules(settings);
        var garden=new GardenService(profiles,settings,new Clock(),rules);profiles.Snapshot();byte[] before=File.ReadAllBytes(path);string identity=Id();fail=true;
        ExpectWriteFailure(()=>garden.PurchaseSeeds(identity,FruitId.Grape,10));
        Require(before.SequenceEqual(File.ReadAllBytes(path)) && profiles.Snapshot().TelemetryOutbox.Count==0 && profiles.Snapshot().Wallet.Coins==100,"Interrupted transaction leaves no success telemetry or partial wallet change");
        fail=false;garden.PurchaseSeeds(identity,FruitId.Grape,10);var queued=profiles.Snapshot().TelemetryOutbox.Single().Value;examples.Add(queued);
        var reopened=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("Do not reset"),commitPolicy:journal);
        Require(reopened.Snapshot().TelemetryOutbox.Single().Value.EventId==queued.EventId,"Offline queue and event identity survive a full disk reload");
        fail=true;var seen=new List<string>();
        ExpectWriteFailure(()=>new ProfileTelemetryDelivery(profiles).Drain(context.ConsentScope,ev=>{seen.Add(ev.EventId);return true;}));
        Require(profiles.Snapshot().TelemetryOutbox.Count==1,"Interrupted delivery acknowledgement retains the event for retry");
        fail=false;new ProfileTelemetryDelivery(reopened).Drain(context.ConsentScope,ev=>{seen.Add(ev.EventId);return true;});
        Require(seen.Count==2 && seen[0]==seen[1] && reopened.Snapshot().TelemetryOutbox.Count==0,"The same stable event id allows analytics deduplication after an interrupted acknowledgement");
        Require(reopened.Snapshot().Wallet.Coins==90 && reopened.Snapshot().Garden.Single().Value.Quantity==10,"Delivery and retries never alter the economy outcome");
    }
    private static void Migration(string folder)
    {
        string path=Path.Combine(folder,Id()+".json");var old=Initial(77);old.Progress.LevelId=19;old.Wallet.PowerUps[RewardType.Ultra]=3;
        var json=JObject.FromObject(old);json["Version"]=3;json.Remove("TelemetryOutbox");json.Remove("TelemetryEngagement");string payload=json.ToString(Formatting.None);
        using(var sha=SHA256.Create())File.WriteAllText(path,JsonConvert.SerializeObject(new {Payload=payload,Checksum=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)))}));
        var profiles=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("No remigration"));var data=profiles.Snapshot();
        Require(data.Version==4 && data.Wallet.Coins==77 && data.Progress.LevelId==19 && data.Wallet.PowerUps[RewardType.Ultra]==3 && data.TelemetryOutbox.Count==0,"Version 3 upgrades without losing progression, inventory or replay protection");
        profiles.Update(profile=>profile.Wallet.Coins++);Require(new AtomicPlayerProfileStorage(path).Load().Version==4,"The next complete save persists version 4");
    }
    private static PlayerProfileData Initial(int coins){var profile=new PlayerProfileData();profile.Wallet.Coins=coins;return profile;}
    private static EconomyTelemetryContext Context()=>new EconomyTelemetryContext {ConsentScope=Id(),UtcDay=20000,BuildChannel="beta",Version="1",Build="17"};
    private static string Id()=>Guid.NewGuid().ToString("N");
    private sealed class Clock:IGardenClock {public int Day=20000;public DateTime UtcNow=>new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddDays(Day);}
    private static void ExpectWriteFailure(Action action){try{action();}catch(IOException){return;}throw new Exception("Expected interrupted write");}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Release check: "+message);}
}

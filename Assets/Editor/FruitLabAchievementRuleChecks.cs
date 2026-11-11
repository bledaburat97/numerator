using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using Newtonsoft.Json;
using Scripts.Economy;

// Also executable outside Unity against the compiled game domain.
public static class FruitLabAchievementRuleChecks
{
    public static void Review(string folder,EconomySettings settings,WalletRewardRules walletRules)
    {
        ReviewCollectionBadges(settings);
        settings=new EconomySettings {Achievements=new[] {
            new AchievementDefinition {Id="first-harvest",Title="İlk Hasat",Description="İlk hasat",Target=1,Source=FruitGainSource.Garden,Reward=RewardType.Life},
            new AchievementDefinition {Id="strawberry-10",Title="Çilek Aşığı",Description="10 çilek",Target=10,Fruit=FruitId.Strawberry,Reward=RewardType.Revealing},
            new AchievementDefinition {Id="collector-50",Title="Meyve Koleksiyoncusu",Description="50 meyve",Target=50,Reward=RewardType.Helper}
        }};
        var rules=new AchievementRules(settings);
        var storage=new MemoryPlayerProfileStorage();var profiles=new PlayerProfileRepository(storage,()=>new PlayerProfileData());
        var inventory=new FruitInventoryService(profiles,rules);var service=new AchievementService(profiles,rules);
        service.Reconcile();long revision=profiles.Snapshot().Revision;service.Reconcile();
        Require(profiles.Snapshot().Revision==revision,"Repeated reconciliation does not rewrite the profile");
        inventory.Grant("eight",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,8}});
        inventory.Grant("nine",FruitGainSource.Duel,new Dictionary<FruitId,long>{{FruitId.Strawberry,1}});
        Require(service.GetAchievements().Single(item=>item.Id=="strawberry-10").Progress==9,"Single and duel share fruit progress");
        var clock=new AchievementClock();profiles.Update(profile=>profile.Garden.Add("ready",new PlantingBatch {CellIndex=0,Fruit=FruitId.Strawberry,Quantity=1,PlantedUtcDay=1,LastGrowthUtcDay=3,Stage=GardenGrowthStage.Flower}));
        var garden=new GardenService(profiles,settings,clock,rules);garden.ProcessVisit();revision=profiles.Snapshot().Revision;garden.Harvest("ready");
        var data=profiles.Snapshot();
        Require(data.Revision==revision+1 && data.Garden.Count==0 && data.Achievements["first-harvest"].RewardGranted
            && data.Achievements["strawberry-10"].RewardGranted && data.Wallet.PowerUps[RewardType.Life]==1 && data.Wallet.PowerUps[RewardType.Revealing]==1,
            "Harvest, fruit, two badge rewards and receipts use one commit");
        revision=data.Revision;garden.ProcessVisit();service.Reconcile();
        Require(profiles.Snapshot().Revision==revision,"Same visit and reconciliation do not award twice");
        var rewards=new GameWinRewardService(profiles,walletRules,rules);string id=Guid.NewGuid().ToString("N");
        rewards.SinglePlayer(id,0,new[]{1,3},0,0);rewards.SinglePlayer(id,0,new[]{1,3},0,0);
        rewards.Duel(Guid.NewGuid().ToString("N"),1,new[]{2,3},RewardType.Bomb);
        inventory.Grant("total50",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Apple,36}});
        data=profiles.Snapshot();Require(data.Achievements["collector-50"].RewardGranted && data.Wallet.PowerUps[RewardType.Helper]==1,"Fifty total game and garden fruits unlock the collection badge");
        inventory.Spend("spend-strawberries",new Dictionary<FruitId,long>{{FruitId.Strawberry,10}});service.Reconcile();
        Require(service.GetAchievements().Single(item=>item.Id=="strawberry-10").Unlocked,"Spending never removes lifetime badges");
        var restarted=new PlayerProfileRepository(storage,()=>throw new Exception("No remigration"));
        var restartedService=new AchievementService(restarted,rules);restartedService.Reconcile();
        Require(JsonConvert.SerializeObject(restarted.Snapshot())==JsonConvert.SerializeObject(profiles.Snapshot()),"Rewards and exact receipts survive restart");
        settings.Achievements[1].Title="Yeni isim";settings.Achievements[1].Target=25;settings.Achievements[1].RewardCount=9;
        var edited=new AchievementService(restarted,new AchievementRules(settings));edited.Reconcile();
        Require(restarted.Snapshot().Wallet.PowerUps[RewardType.Revealing]==1,"Config name/target/reward edits never pay an existing badge again");
        Require(edited.GetAchievements().Single(goal=>goal.Id=="strawberry-10").RewardCount==1,"Earned badge displays the actual paid reward after settings change");

        var adjustments=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>new PlayerProfileData());
        new FruitInventoryService(adjustments,rules).Grant("admin",FruitGainSource.Adjustment,new Dictionary<FruitId,long>{{FruitId.Strawberry,100}});
        Require(new AchievementService(adjustments,rules).GetAchievements().All(goal=>goal.Progress==0 && !goal.Unlocked),"Adjustment grants do not count as gameplay or harvest");
        var old=new PlayerProfileData();old.Collection[FruitId.Strawberry]=new FruitBalance {Current=10,LifetimeEarned=10,EarnedBySource=new Dictionary<FruitGainSource,long>{{FruitGainSource.SinglePlayer,10}}};
        var oldProfile=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>old);new AchievementService(oldProfile,rules).Reconcile();
        Require(oldProfile.Snapshot().Wallet.PowerUps[RewardType.Revealing]==1,"Existing collections receive eligible badges once");
        var invalid=new EconomySettings();invalid.Achievements[1].Id=invalid.Achievements[0].Id;ExpectAchievementFailure(()=>invalid.Validate());

        string path=folder+"/achievement-atomic-"+Guid.NewGuid().ToString("N")+".json";bool fail=false;
        var disk=new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("simulated failure");});
        var durable=new PlayerProfileRepository(disk,()=>old.Copy());durable.Snapshot();byte[] bytes=File.ReadAllBytes(path);fail=true;
        try {new AchievementService(durable,rules).Reconcile();throw new Exception("Expected failed save");}catch(IOException){}
        Require(bytes.SequenceEqual(File.ReadAllBytes(path)) && durable.Snapshot().Wallet.PowerUps[RewardType.Revealing]==0,"Failed reconciliation leaves both disk and memory unchanged");
        fail=false;new AchievementService(durable,rules).Reconcile();
        var reopened=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("Missing profile"));
        new AchievementService(reopened,rules).Reconcile();Require(reopened.Snapshot().Wallet.PowerUps[RewardType.Revealing]==1,"Disk retry and reload pay once");
        // The fruit and badge must also roll back together, not just backfill.
        var fresh=new PlayerProfileRepository(disk,()=>new PlayerProfileData());var grant=new FruitInventoryService(fresh,rules);fresh.Snapshot();fail=true;bytes=File.ReadAllBytes(path);
        try {grant.Grant("failed-total",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Apple,50}});throw new Exception("Expected failed grant");}catch(IOException){}
        Require(bytes.SequenceEqual(File.ReadAllBytes(path)) && !fresh.Snapshot().Achievements["collector-50"].RewardGranted,"Failed fruit plus badge commit rolls everything back");
        fail=false;grant.Grant("failed-total",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Apple,50}});
        grant.Grant("failed-total",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Apple,50}});
        Require(fresh.Snapshot().Wallet.PowerUps[RewardType.Helper]==1,"Retry of shared grant pays exactly once");
    }

    public static void ReviewCollectionBadges(EconomySettings settings)
    {
        var priorities=new[]{
            new AchievementStatus {Id="less",Progress=1,Target=10},
            new AchievementStatus {Id="longer",Progress=10,Target=20},
            new AchievementStatus {Id="b",Progress=5,Target=10},
            new AchievementStatus {Id="a",Progress=5,Target=10},
            new AchievementStatus {Id="closest",Progress=9,Target=10},
            new AchievementStatus {Id="earned",Progress=10,Target=10,Unlocked=true}
        };
        Require(AchievementPriority.Pending(priorities).Select(goal=>goal.Id).SequenceEqual(new[]{"closest","a","b","longer","less"}),
            "Badge priority shares ratio, remaining amount and ordinal ID tie breakers, excluding earned badges");
        Require(settings.Achievements.Length==5 && settings.Achievements.All(item=>item.RewardKind==AchievementRewardKind.Badge),"Exactly five badge-only goals");
        var rules=new AchievementRules(settings);
        var storage=new MemoryPlayerProfileStorage();var profiles=new PlayerProfileRepository(storage,()=>new PlayerProfileData());
        var inventory=new FruitInventoryService(profiles,rules);var service=new AchievementService(profiles,rules);
        service.Reconcile();Require(service.Closest().Id=="garden-owner-10","The first home goal is the reachable ten-fruit badge");
        inventory.Grant("admin",FruitGainSource.Adjustment,new Dictionary<FruitId,long>{{FruitId.Strawberry,2000}});
        Require(service.GetAchievements().All(item=>item.Progress==0) && FruitCollectionTotals.Earned(profiles.Snapshot().Collection)==0,"Administrative grants are excluded from totals and goals");
        inventory.Grant("single",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,60}});
        inventory.Grant("duel",FruitGainSource.Duel,new Dictionary<FruitId,long>{{FruitId.Strawberry,39}});
        Require(service.Closest().Id==service.GetAchievements().First(goal=>!goal.Unlocked).Id
            && service.ClosestFor(new[]{(int)FruitId.Strawberry}).Id==service.Closest().Id,"Home, relevant round target and collection consume the same prepared pending order");
        Require(service.GetAchievements().Single(item=>item.Id=="strawberry-grower-100").Progress==99,"Duels count toward fruit goals");
        inventory.Grant("harvest",FruitGainSource.Garden,new Dictionary<FruitId,long>{{FruitId.Strawberry,1},{FruitId.Watermelon,100},{FruitId.Apple,1300}});
        inventory.Grant("harvest",FruitGainSource.Garden,new Dictionary<FruitId,long>{{FruitId.Strawberry,1},{FruitId.Watermelon,100},{FruitId.Apple,1300}});
        Require(service.GetAchievements().All(item=>item.Unlocked && item.RewardGranted) && service.Closest()==null,"All five thresholds unlock once; no finished home goal remains");
        Require(profiles.Snapshot().Wallet.PowerUps.Values.All(value=>value==0),"Badge-only goals never pay a phantom power-up");
        inventory.Spend("spend",new Dictionary<FruitId,long>{{FruitId.Apple,1300}});
        Require(FruitCollectionTotals.Earned(profiles.Snapshot().Collection)==1500,"Collection display uses lifetime eligible earnings after spending");
        var reopened=new PlayerProfileRepository(storage,()=>throw new Exception("No migration"));new AchievementService(reopened,rules).Reconcile();
        Require(JsonConvert.SerializeObject(reopened.Snapshot())==JsonConvert.SerializeObject(profiles.Snapshot()),"Badge receipts survive restart without a second award");
        var legacy=new AchievementRules(new EconomySettings {Achievements=new[]{new AchievementDefinition {Id="old-badge",Title="Old",Description="Old",Target=1,Reward=RewardType.Life}}});
        new AchievementService(profiles,legacy).Reconcile();var oldReceipt=profiles.Snapshot().Transactions["achievement:old-badge"].Request;
        service.Reconcile();Require(profiles.Snapshot().Wallet.PowerUps[RewardType.Life]==1 && profiles.Snapshot().Transactions["achievement:old-badge"].Request==oldReceipt,"Archived rewards and receipts are preserved with the new catalog");
    }

    private static void ExpectAchievementFailure(Action action)
    {try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected invalid achievement config");}
    private sealed class AchievementClock:IGardenClock {public DateTime UtcNow=>new DateTime(1970,1,5,0,0,0,DateTimeKind.Utc);}

    private static void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
}

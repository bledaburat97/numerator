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
        var rules=new AchievementRules(settings);
        var storage=new MemoryPlayerProfileStorage();var profiles=new PlayerProfileRepository(storage,()=>new PlayerProfileData());
        var inventory=new FruitInventoryService(profiles,rules);var service=new AchievementService(profiles,rules);
        service.Reconcile();long revision=profiles.Snapshot().Revision;service.Reconcile();
        Require(profiles.Snapshot().Revision==revision,"Repeated reconciliation does not rewrite the profile");
        inventory.Grant("eight",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,8}});
        inventory.Grant("nine",FruitGainSource.Duel,new Dictionary<FruitId,long>{{FruitId.Strawberry,1}});
        Require(service.GetAchievements().Single(item=>item.Id=="strawberry-10").Progress==9,"Single and duel share fruit progress");
        var clock=new AchievementClock();profiles.Update(profile=>profile.Garden.Add("ready",new PlantingBatch {Fruit=FruitId.Strawberry,Quantity=1,PlantedUtcDay=1,LastGrowthUtcDay=3,Stage=GardenGrowthStage.Flower}));
        var garden=new GardenService(profiles,settings,clock,rules);revision=profiles.Snapshot().Revision;garden.ProcessVisit();
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

    private static void ExpectAchievementFailure(Action action)
    {try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected invalid achievement config");}
    private sealed class AchievementClock:IGardenClock {public DateTime UtcNow=>new DateTime(1970,1,5,0,0,0,DateTimeKind.Utc);}

    private static void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
}

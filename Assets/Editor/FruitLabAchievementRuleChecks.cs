using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using Scripts;
using Newtonsoft.Json;
using Scripts.Economy;

public static class FruitLabAchievementRuleChecks
{
    public static void Review(string folder,EconomySettings settings,WalletRewardRules walletRules)
    {
        Directory.CreateDirectory(folder);ReviewCollectionBadges(settings);
        ReviewFruitTargets(settings);ReviewHarvest(settings);ReviewWins(settings,walletRules);ReviewLegacy(settings);ReviewAtomic(folder,settings);
    }
    public static void ReviewCollectionBadges(EconomySettings settings)
    {
        settings.Validate();Require(settings.SeedsPerPack==2 && settings.SeedPackCoinPrice==1,"One coin buys two seeds");
        var profiles=Profiles();var rules=new AchievementRules(settings,_=>0);var service=new AchievementService(profiles,rules);service.Reconcile();
        var data=profiles.Snapshot();var goals=service.GetAchievements();
        Require(data.Missions.Active.Count==5 && goals.Count==5 && goals.Count(goal=>goal.Visible)==4,"Five independent channels, with duel hidden before unlock");
        Require(Goal(data,MissionKind.FruitA).Fruit==FruitId.Strawberry && Goal(data,MissionKind.FruitB).Fruit==FruitId.Orange
            && Goal(data,MissionKind.FruitA).Target==10 && Goal(data,MissionKind.Level).Target==10 && Goal(data,MissionKind.Harvest).Target==5 && Goal(data,MissionKind.Duel).Target==1,"Exact starting goals");
        long revision=data.Revision;service.Reconcile();Require(profiles.Snapshot().Revision==revision,"Repeated reconciliation is read-only when no goals complete");
        var expected=new[]{new long[]{10,20,50,100,150,200,250,300},new long[]{5,15,30,50,100,150,200},new long[]{1,5,10,20,30,40,50,60}};
        var tiers=new[]{new MissionThresholds(settings.Missions.FruitTargets,settings.Missions.FruitStep),new MissionThresholds(settings.Missions.HarvestTargets,settings.Missions.HarvestStep),new MissionThresholds(settings.Missions.DuelTargets,settings.Missions.DuelStep)};
        for(int index=0;index<tiers.Length;index++)for(int step=0;step<expected[index].Length-1;step++)Require(tiers[index].TryNext(expected[index][step],out long next) && next==expected[index][step+1],"Configured threshold progression");
        var inventory=new FruitInventoryService(profiles,rules);inventory.Grant("adjustment",FruitGainSource.Adjustment,new Dictionary<FruitId,long>{{FruitId.Strawberry,1000}});
        Require(service.GetAchievements().All(goal=>goal.Progress==0) && service.BadgeCount==0,"Administrative fruit does not count as collection or missions");
        profiles.Update(profile=>profile.Progress.LevelId=MenuProgression.MultiplayerUnlockLevel-1);service.Reconcile();
        Require(service.GetAchievements().Single(goal=>goal.Kind==MissionKind.Duel).Visible,"Duel becomes visible at the existing multiplayer unlock boundary");
        var invalid=new EconomySettings();invalid.Missions.FruitTargets=new long[]{10,5};
        try{invalid.Validate();throw new Exception("Expected invalid thresholds");}catch(InvalidOperationException){}
    }
    private static void ReviewFruitTargets(EconomySettings settings)
    {
        var profiles=Profiles();var rules=new AchievementRules(settings,_=>1);var service=new AchievementService(profiles,rules);var inventory=new FruitInventoryService(profiles,rules);
        service.Reconcile();inventory.Grant("banana18",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Banana,18}});
        inventory.Grant("straw9",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,9}});
        inventory.Grant("straw1",FruitGainSource.Duel,new Dictionary<FruitId,long>{{FruitId.Strawberry,1}});
        var data=profiles.Snapshot();var goal=Goal(data,MissionKind.FruitA);
        Require(goal.Target==20 && goal.Fruit==FruitId.Banana && service.GetAchievements().Single(item=>item.Kind==MissionKind.FruitA).Progress==18 && service.BadgeCount==1,"New random banana goal starts at cumulative 18/20");
        string saved=JsonConvert.SerializeObject(data);for(int repeat=0;repeat<5;repeat++){service.GetAchievements();service.Reconcile();}
        Require(saved==JsonConvert.SerializeObject(profiles.Snapshot()),"Opening and reconciling never rerolls the selected fruit");
        inventory.Spend("spend",new Dictionary<FruitId,long>{{FruitId.Banana,10}});
        Require(service.GetAchievements().Single(item=>item.Kind==MissionKind.FruitA).Progress==18,"Spending does not reduce lifetime progress");
        inventory.Grant("banana2",FruitGainSource.Garden,new Dictionary<FruitId,long>{{FruitId.Banana,2}});
        Require(Goal(profiles.Snapshot(),MissionKind.FruitA).Target==50 && Goal(profiles.Snapshot(),MissionKind.FruitB).Target==10,"Only the completed fruit channel advances");
        var skip=Profiles();var skipRules=new AchievementRules(settings,_=>0);var skipService=new AchievementService(skip,skipRules);skipService.Reconcile();
        new FruitInventoryService(skip,skipRules).Grant("all50",FruitGainSource.SinglePlayer,Enum.GetValues(typeof(FruitId)).Cast<FruitId>().ToDictionary(fruit=>fruit,fruit=>50L));
        Require(skipService.BadgeCount==2 && Goal(skip.Snapshot(),MissionKind.FruitA).Target==100 && Goal(skip.Snapshot(),MissionKind.FruitB).Target==100,"All fruits at fifty skip twenty and fifty without paying those skipped tiers");
        var reopened=new PlayerProfileRepository(new MemorySnapshot(skip.Snapshot()),()=>throw new Exception("No reset"));new AchievementService(reopened,new AchievementRules(settings,_=>8)).Reconcile();
        Require(JsonConvert.SerializeObject(reopened.Snapshot().Missions)==JsonConvert.SerializeObject(skip.Snapshot().Missions),"Restart with a different random selector retains selected goals");
    }
    private static void ReviewHarvest(EconomySettings settings)
    {
        var profiles=Profiles();var rules=new AchievementRules(settings,_=>0);var service=new AchievementService(profiles,rules);service.Reconcile();
        profiles.Update(profile=>profile.Garden.Add("ready",new PlantingBatch{Fruit=FruitId.Apple,Quantity=30,CellIndex=0,PlantedUtcDay=1,LastGrowthUtcDay=4,Stage=GardenGrowthStage.Fruit}));
        var garden=new GardenService(profiles,settings,new AchievementClock(),rules);var harvest=garden.Harvest("ready");
        Require(harvest.BadgesEarned==3 && service.BadgeCount==3 && Goal(profiles.Snapshot(),MissionKind.Harvest).Target==50 && profiles.Snapshot().Garden.Count==0,"One harvest of thirty pays five/fifteen/thirty in one atomic result");
        var replay=garden.Harvest("ready");Require(replay.Status==ProfileCommitResult.AlreadyApplied && service.BadgeCount==3,"Repeated harvest never repeats medals");
        var old=Profiles();var oldRules=new AchievementRules(settings,_=>0);new FruitInventoryService(old,oldRules).Grant("old-harvest",FruitGainSource.Garden,new Dictionary<FruitId,long>{{FruitId.Apple,30}});
        new AchievementService(old,oldRules).Reconcile();Require(old.Snapshot().Missions.Badges==3 && Goal(old.Snapshot(),MissionKind.Harvest).Target==50,"Previously earned harvest totals count too");
        var bulk=new MissionThresholds(settings.Missions.HarvestTargets,settings.Missions.HarvestStep);
        Require(bulk.Reached(5,1000000000)==20000003,"Large harvests advance arithmetically without looping per badge");
        var purchase=Profiles();purchase.Update(profile=>profile.Wallet.Coins=2);var store=new GardenService(purchase,settings,new AchievementClock(),rules);
        var quote=store.QuoteSeeds(FruitId.Apple,4);Require(quote.TotalCoinPrice==2 && quote.CanPurchase,"Central two-seed pack pricing");
        string identity=Guid.NewGuid().ToString("N");store.PurchaseSeeds(identity,FruitId.Apple,4);store.PurchaseSeeds(identity,FruitId.Apple,4);
        Require(purchase.Snapshot().Wallet.Coins==0 && purchase.Snapshot().Garden.Single().Value.Quantity==4,"Seed debit and repeat protection preserve the new exchange rate");
    }
    private static void ReviewWins(EconomySettings settings,WalletRewardRules wallet)
    {
        var profiles=Profiles();var rules=new AchievementRules(settings,_=>0);var service=new AchievementService(profiles,rules);service.Reconcile();profiles.Update(profile=>profile.Progress.LevelId=9);
        var rewards=new GameWinRewardService(profiles,wallet,rules);string round=Guid.NewGuid().ToString("N");var win=rewards.SinglePlayer(round,9,new[]{1,2},0,0);rewards.SinglePlayer(round,9,new[]{1,2},0,0);
        Require(win.BadgesEarned==1 && profiles.Snapshot().Progress.LevelId==10 && Goal(profiles.Snapshot(),MissionKind.Level).Target==20,"Completing level ten grants the level medal once");
        string match=Guid.NewGuid().ToString("N");rewards.Duel(match,1,new[]{1,2},RewardType.Bomb);rewards.Duel(match,1,new[]{1,2},RewardType.Bomb);
        Require(profiles.Snapshot().Progress.DuelWins==1 && Goal(profiles.Snapshot(),MissionKind.Duel).Target==5 && service.BadgeCount==2,"Duel wins, medals and match receipts share one commit boundary");
        profiles.Update(profile=>{profile.Progress.LevelId=50;profile.Progress.DuelWins=20;rules.Apply(profile);});
        Require(Goal(profiles.Snapshot(),MissionKind.Level).Target==100 && Goal(profiles.Snapshot(),MissionKind.Duel).Target==30,"Level and duel channels independently award all crossed tiers");
        Require(profiles.Snapshot().Wallet.PowerUps[RewardType.Bomb]==1 && profiles.Snapshot().Wallet.PowerUps.Where(item=>item.Key!=RewardType.Bomb).All(item=>item.Value==0),"Missions award medals and no phantom powerups");
    }
    private static void ReviewLegacy(EconomySettings settings)
    {
        var data=new PlayerProfileData{Version=5,Missions=null};data.Wallet.Coins=77;data.Wallet.PowerUps[RewardType.Life]=2;
        data.Transactions["old-badge"]=new ProfileTransactionRecord{Kind="achievement_reward",Request="{}",AppliedAtUtc=DateTime.UtcNow};
        data.Achievements["old-name"]=new AchievementProgress{Unlocked=true,RewardGranted=true,Progress=1,UnlockedAtUtc=DateTime.UtcNow,RewardTransactionId="old-badge"};
        for(int turn=0;turn<2;turn++)data.Transactions["old-duel:"+turn]=new ProfileTransactionRecord{Kind="duel_win",Request="{}",AppliedAtUtc=DateTime.UtcNow};
        var profiles=new PlayerProfileRepository(new MemorySnapshot(data),()=>throw new Exception("No legacy reset"));var service=new AchievementService(profiles,new AchievementRules(settings,_=>0));service.Reconcile();
        var migrated=profiles.Snapshot();Require(migrated.Version==6 && migrated.Missions.Badges==2 && migrated.Progress.DuelWins==2 && migrated.Wallet.Coins==77 && migrated.Wallet.PowerUps[RewardType.Life]==2,"Migration keeps the old badge, reconstructs duel wins, then grants the retrospective duel tier once");
        Require(service.GetAchievements().Count==5 && service.GetAchievements().All(goal=>goal.Id!="old-name"),"Legacy completed task history is not exposed in the active list");
        long revision=migrated.Revision;service.Reconcile();Require(profiles.Snapshot().Revision==revision,"Migration and retrospective awards never repeat");
    }
    private static void ReviewAtomic(string folder,EconomySettings settings)
    {
        bool fail=false;string path=Path.Combine(folder,"mission-"+Guid.NewGuid().ToString("N")+".json");
        var disk=new AtomicPlayerProfileStorage(path,beforePublish:()=>{if(fail)throw new IOException("Injected mission write failure");});
        var profiles=new PlayerProfileRepository(disk,()=>new PlayerProfileData());var rules=new AchievementRules(settings,_=>0);var service=new AchievementService(profiles,rules);service.Reconcile();
        var inventory=new FruitInventoryService(profiles,rules);inventory.Grant("nine",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,9}});
        byte[] bytes=File.ReadAllBytes(path);string snapshot=JsonConvert.SerializeObject(profiles.Snapshot());fail=true;
        try{inventory.Grant("last",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,1}});throw new Exception("Expected failed write");}catch(IOException){}
        Require(bytes.SequenceEqual(File.ReadAllBytes(path)) && snapshot==JsonConvert.SerializeObject(profiles.Snapshot()),"Fruit, medal and new random goal all roll back with a failed file write");
        fail=false;inventory.Grant("last",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,1}});inventory.Grant("last",FruitGainSource.SinglePlayer,new Dictionary<FruitId,long>{{FruitId.Strawberry,1}});
        var reopened=new PlayerProfileRepository(new AtomicPlayerProfileStorage(path),()=>throw new Exception("No reset"));new AchievementService(reopened,new AchievementRules(settings,_=>8)).Reconcile();
        Require(reopened.Snapshot().Missions.Badges==1 && JsonConvert.SerializeObject(reopened.Snapshot())==JsonConvert.SerializeObject(profiles.Snapshot()),"Retry and disk reload grant once and retain the committed selected fruit");
    }
    private static PlayerProfileRepository Profiles()=>new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>new PlayerProfileData());
    private static MissionProgress Goal(PlayerProfileData profile,MissionKind kind)=>profile.Missions.Active.Single(goal=>goal.Kind==kind);
    private sealed class MemorySnapshot:IPlayerProfileStorage
    {private PlayerProfileData _data;public MemorySnapshot(PlayerProfileData data){_data=data.Copy();}public PlayerProfileData Load()=>_data.Copy();public void Save(PlayerProfileData data)=>_data=data.Copy();}
    private sealed class AchievementClock:IGardenClock {public DateTime UtcNow=>new DateTime(1970,1,5,0,0,0,DateTimeKind.Utc);}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Mission check: "+message);}
}

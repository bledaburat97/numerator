using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Game;
using Newtonsoft.Json;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeLevelIdentity()
    {
        const string path="Assets/Scenes/Game.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var canvas=Find<Canvas>(scene);Find<FruitLabGameLayout>(scene).ApplyLayout();
            var environment=LevelEnvironmentView.Ensure(canvas);environment.ResetPresentation();
            PowerUpRewardView.Ensure(canvas).Hide();
            EditorUtility.SetDirty(environment);EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Save level identity component");
            return "Saved level identity component; content cycle is unchanged";
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    public static void ReviewLevelIdentity(Canvas canvas,Camera camera)
    {
        string before=Profile(),cycle=File.ReadAllText("Assets/Resources/LevelPlaytestConfig.json");
        var old=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        var errors=new List<string>();Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert || message.Contains("DOTWEEN ►"))errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            var definitions=new[]{
                new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=10},
                new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=10},
                new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=10,Mechanic=LevelMechanic.Helper},
                new LevelData {NumOfBoardHolders=4,NumOfCards=8,MaxNumOfTries=10,Mechanic=LevelMechanic.Pair},
                new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=10,Mechanic=LevelMechanic.Split},
                new LevelData {NumOfBoardHolders=2,NumOfCards=9,MaxNumOfTries=8,Mechanic=LevelMechanic.Growing},
                new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=8,Mechanic=LevelMechanic.Dynamite},
                new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9,BonusReward=RewardType.Ultra},
                new LevelData {NumOfBoardHolders=4,NumOfCards=8,MaxNumOfTries=9,Mechanic=LevelMechanic.Ice},
                new LevelData {NumOfBoardHolders=4,NumOfCards=9,MaxNumOfTries=10,DisablePowerUps=true}};
            foreach(var size in new[]{new Vector2(320,568),new Vector2(375,812)})
            foreach(var definition in definitions)
            {
                ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));
                using(var game=new Fixture(canvas,camera,definition))
                {
                    game.BuildActors();game.Gate.Clear();var id=LevelIdentity.For(definition);
                    new LevelMechanicsPresenter(game.View,game.Board).ShowDynamite(definition.Mechanic,null);
                    new LevelMechanicsPresenter(game.View,game.Board).ShowStatus(definition.Mechanic,definition,definition.NumOfBoardHolders,0,definition.Mechanic==LevelMechanic.Split);
                    var environment=LevelEnvironmentView.Ensure(canvas);
                    Require(canvas.GetComponent<Image>().color==id.Background,"First-frame background matches actual content");
                    var intro=environment.PlayIntro();
                    var panel=canvas.transform.Find("LevelIntro");
                    if(id.Group==LevelGroup.Normal)
                    {
                        Require(intro==null && (panel==null || !panel.gameObject.activeSelf),"Normal levels have no opening banner or banner delay, including five-slot puzzles");
                    }
                    else
                    {
                        string expected=definition.BonusReward.HasValue?"Bonus":definition.DisablePowerUps?"Güçlendirici Yasağı":definition.Mechanic==LevelMechanic.Helper?"Üçte Bir İpucu":definition.Mechanic==LevelMechanic.Pair?"Bağlı Ardışık Kartlar":definition.Mechanic==LevelMechanic.Split?"Bölünmüş Tahta":definition.Mechanic==LevelMechanic.Growing?"Büyüyen Tahta":definition.Mechanic==LevelMechanic.Dynamite?"Dinamit":"Dondurulmuş";
                        Require(intro!=null && panel.Find("Title").GetComponent<TMP_Text>().text==expected,"Special level opening uses the requested name");
                        Tick(.20f);Require(panel.gameObject.activeSelf && !panel.GetComponent<CanvasGroup>().blocksRaycasts,"Intro never requires a tap or blocks input");
                        var introRect=(RectTransform)panel;var introGroup=panel.GetComponent<CanvasGroup>();
                        Tick(.85f);Require(panel.gameObject.activeSelf && Mathf.Abs(introRect.anchoredPosition.x)<.1f && introGroup.alpha>.99f,"The title remains fully readable for one second");
                        Tick(.2f);Require(panel.gameObject.activeSelf && introRect.anchoredPosition.x>0 && introGroup.alpha>.99f,"The introduction exits toward the right without fading out");
                        Tick(.4f);Require(!panel.gameObject.activeSelf,"Introduction dismisses after moving offscreen");
                    }
                    var rear=canvas.transform.Find("SafeArea/MiddleArea/GameBackground").GetComponent<Image>();
                    var front=canvas.transform.Find("SafeArea/MiddleArea/GameForeground").GetComponent<Image>();
                    Require(rear.sprite!=null && rear.sprite.name=="first" && rear.color==id.Background,"Supplied first image is the type-tinted rear layer");
                    Require(!front.gameObject.activeSelf,"Gameplay foreground is retired without removing its source asset");
                    Require(canvas.transform.Find("SafeArea/GameHeaderBand").GetComponent<Image>().color==id.Background,"Header and upper unsafe area use the selected gameplay color");
                    if(definition.Mechanic==LevelMechanic.Growing)
                        Require(canvas.GetComponentsInChildren<Image>().Count(image=>image.name.StartsWith("FutureSlot"))==2,"Two future-slot silhouettes are visible beside the initial growing board");
                    if(definition.Mechanic==LevelMechanic.Dynamite)
                    {
                        var danger=new DynamiteRoundState {BoardIndex=1};game.Board.ShowDynamite(danger);Tick(.25f);
                        Require(game.Board.GetRectTransformOfGarden(1).GetComponentInParent<BoardHolderView>().GetComponentsInChildren<FruitLabGlyph>().Any(g=>g.Type==FruitLabGlyph.Symbol.Dynamite),"Dynamite uses the new compact icon");
                    }
                    var popup=canvas.GetComponentInChildren<LevelFinishPopupView>(true);var controller=new LevelEndPopupController(popup);
                    popup.SetOutcome(false);controller.InitButton(LevelFinishButtonType.Game,"Tekrar oyna",()=>{});controller.SetContinuationIdentity(definition);
                    var action=((Component)popup.GetButton(LevelFinishButtonType.Game)).GetComponent<BaseButtonView>();
                    Require(action.GetComponentsInChildren<FruitLabGlyph>().Any(g=>g.name=="LevelKind" && g.Type==id.Symbol),"Retry carries the current mechanic icon");
                    controller.SetContinuationIdentity(LevelDataCreator.DefinitionFor(37));
                    Require(action.GetComponentsInChildren<FruitLabGlyph>().Any(g=>g.name=="LevelKind" && g.Type==LevelIdentity.For(LevelDataCreator.DefinitionFor(37)).Symbol),"Continue can carry the actual target definition independent of the current group");
                    popup.SetStatus(false);
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-level-"+(definition.BonusReward.HasValue?"bonus":definition.Mechanic==LevelMechanic.Normal?id.Group.ToString().ToLowerInvariant():definition.Mechanic.ToString().ToLowerInvariant())+"-"+(int)size.y+".png");
                }
            }
            ReviewBonusAward(canvas,camera);ReviewBonusResume(canvas,camera);
            for(int level=0;level<100;level++)
            {
                var actual=LevelDataCreator.DefinitionFor(level);var preview=LevelDefinitionPreview.Resolve(level);
                Require(JsonConvert.SerializeObject(actual)==JsonConvert.SerializeObject(preview),"Menu and gameplay agree at "+(level+1));
            }
            Require(File.ReadAllText("Assets/Resources/LevelPlaytestConfig.json")==cycle,"The production test cycle is unchanged");
            Require(before==Profile() && errors.Count==0,"Player preferences and tween checks: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/level-identity-review.json",JsonConvert.SerializeObject(new{passed=true,layouts=definitions.Length*2,previewedLevels=100,
                checks=new[]{"content-based groups","no number-ending distribution","first-frame themes","normal levels have no intro or intro delay","requested special-level names","one-second title followed by rightward exit","tinted rear background with hidden foreground","game-colored header and top unsafe area","future growing slots","retry and target buttons","bonus wallet preserved","idempotent bonus and fruit award","bonus win and acknowledgement","unchanged content cycle","unchanged real preferences"}},Formatting.Indented));
        }
        finally{Application.logMessageReceived-=collect;DOTween.defaultUpdateType=old;}
    }
    private static void ReviewBonusResume(Canvas canvas,Camera camera)
    {
        var template=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9,BonusReward=RewardType.Revealing};
        using(var schedule=LevelPlaytestSchedule.UseForTests(new LevelPlaytestSettings {Enabled=true,StartLevel=5,Revision=3,Cycle=new List<LevelData>{template}}))
        {
            LevelPlaytestSchedule.TryCreate(17,out var definition);LevelSaveData snapshot=null;
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",17);
                using(var session=CreateContractSession(game,definition))
                {
                    session.Begin(LevelEntryMode.New);Tick(.6f);game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{3,1,5});
                    SubmitSplit(game,new[]{1,2,4});
                    Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out snapshot),"A partially played bonus can be checkpointed");
                    snapshot=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(snapshot));
                    Require(snapshot.Definition.BonusReward==RewardType.Revealing && snapshot.ActiveRewardTokens.Count==0,"Checkpoint preserves direct reward identity without normal reward tokens");
                }
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",17);((MemorySave)Get(game,"_memory")).Save(snapshot);
                using(var session=CreateContractSession(game,definition))
                {
                    session.Begin(LevelEntryMode.Resume);game.Gate.Clear();
                    Require(session.State==LevelFlowState.Playing && game.Round.GetTriedCardsList().Count==1 && game.Round.GetRemainingGuessCount()==8,
                        "Resume restores bonus history and lives without replaying the intro");
                    Require(game.Target.GetTargetCardsList().SequenceEqual(snapshot.TargetCards) && game._saves.GetLevelSaveData().RoundId==snapshot.RoundId,"Resume retains the exact answer and durable round identity");
                    int stock=game.Tracker.GetPowerUpCount(RewardType.Revealing),coins=game.Tracker.GetCoinCount(),crystals=game.Tracker.GetCrystalProgressCount();
                    game._guess.FailRound();for(int i=0;i<25;i++)Tick(.1f);
                    Require(game.Tracker.GetPowerUpCount(RewardType.Revealing)==stock && game.Tracker.GetCoinCount()==coins && game.Tracker.GetCrystalProgressCount()==crystals,
                        "A lost bonus gives no direct award or currencies");
                }
            }
        }
    }
    private static void ReviewBonusAward(Canvas canvas,Camera camera)
    {
        var settings=new EconomySettings();var rules=new AchievementRules(settings);
        var storage=new MemoryPlayerProfileStorage();
        var profiles=new PlayerProfileRepository(storage,()=>{var data=new PlayerProfileData();data.Progress.LevelId=40;data.Wallet.Coins=73;data.Wallet.CrystalProgress=5;data.Wallet.NextReward=RewardType.Life;return data;});
        var rewards=new GameWinRewardService(profiles,new WalletRewardRules(6,(r,l)=>true,(r,l)=>RewardType.Bomb),rules);
        string id=Guid.NewGuid().ToString("N");var answer=new[]{1,2,3,4};var first=rewards.Bonus(id,40,answer,RewardType.Split);long revision=profiles.Snapshot().Revision;
        var replay=rewards.Bonus(id,40,answer,RewardType.Split);var saved=profiles.Snapshot();
        Require(replay.IsReplay && profiles.Snapshot().Revision==revision && first.PowerUps.SequenceEqual(new[]{RewardType.Split}),"A repeated bonus result awards only once");
        Require(saved.Wallet.Coins==73 && saved.Wallet.CrystalProgress==5 && saved.Wallet.NextReward==RewardType.Life && saved.Wallet.PowerUps[RewardType.Split]==1 && saved.Progress.LevelId==41,"Bonus preserves coin/crystal/next-reward state while advancing progression");
        Require(answer.All(fruit=>saved.Collection[(FruitId)fruit].Current==1),"Bonus winning answer enters the common fruit collection");
        var reopened=new PlayerProfileRepository(storage,()=>new PlayerProfileData());Require(reopened.Snapshot().Wallet.CrystalProgress==5 && reopened.Snapshot().Collection[FruitId.Strawberry].Current==1,"Bonus persists across a profile reload");
        ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
        var definition=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9,BonusReward=RewardType.Split};
        using(var game=new Fixture(canvas,camera,definition))
        using(var presentation=new PowerUpRewardPresenter(PowerUpRewardView.Ensure(canvas),game.Tracker,new HapticController()))
        using(var session=CreateContractSession(game,definition))
        {
            var popup=(LevelEndPopupController)Get(Get(session,"_endPresenter"),"_popup");Field(popup,"_rewardPresentation",presentation);
            session.Begin(LevelEntryMode.New);Tick(.6f);game.Gate.Clear();
            Require(game.Round.GetLifeBarRewardInfoList().Count==0,"Bonus has no coin/crystal life-bar tokens");
            int coins=game.Tracker.GetCoinCount(),crystals=game.Tracker.GetCrystalProgressCount(),stock=game.Tracker.GetPowerUpCount(RewardType.Split);
            game.Target.RevealMultiplayerAnswer(new[]{3,1,5});for(int i=0;i<3;i++)Require(game.Placement.TryPlaceCardOnBoard(new[]{3,1,5}[i]-1,i),"Bonus placement");Tick(.3f);game.Check();
            Require(game.Tracker.GetPowerUpCount(RewardType.Split)==stock+1 && game.Tracker.GetCoinCount()==coins && game.Tracker.GetCrystalProgressCount()==crystals,"The real bonus session awards its direct powerup without currency");
            for(int i=0;i<40;i++)Tick(.1f);Require(presentation.IsPresenting,"Bonus uses the common claim presentation");
            PowerUpRewardView.Ensure(canvas).ClaimButton.onClick.Invoke();for(int i=0;i<12;i++)Tick(.1f);
            Require(!presentation.IsPresenting && game.Tracker.GetPowerUpCount(RewardType.Split)==stock+1,"Bonus acknowledgement never repeats the award");
        }
    }
}

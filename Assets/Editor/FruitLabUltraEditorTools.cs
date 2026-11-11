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
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeUltraPresentation()
    {
        foreach(string name in new[]{"Helper","Ultra","Pair","Split","Information","Eraser","Confirm","Swap"})
        {
            string asset="Assets/Resources/Art/UI/"+name+".png";
            if(!File.Exists(asset))continue;
            AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(asset);
            if(importer.textureType!=TextureImporterType.Sprite || importer.maxTextureSize!=512)
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                var textureSettings=new TextureImporterSettings();importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(textureSettings);
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
        FruitLabStyle.ResetCachedAssets();
        var settings=PrefabUtility.LoadPrefabContents("Assets/Prefabs/SettingsPopup.prefab");
        try {settings.GetComponent<SettingsPopupView>().ApplyLabStyle();PrefabUtility.SaveAsPrefabAsset(settings,"Assets/Prefabs/SettingsPopup.prefab");}
        finally {Object.DestroyImmediate(settings);}
        var holder=PrefabUtility.LoadPrefabContents("Assets/Prefabs/BoardHolder.prefab");
        try {holder.GetComponent<BoardHolderView>().ApplyLabStyle();PrefabUtility.SaveAsPrefabAsset(holder,"Assets/Prefabs/BoardHolder.prefab");}
        finally {Object.DestroyImmediate(holder);}
        foreach(string sceneName in new[]{"Game","Menu","Lobby","WaitingScene"})
        {
            string path="Assets/Scenes/"+sceneName+".unity";
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool loaded=scene.IsValid() && scene.isLoaded;
            if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                foreach(var root in scene.GetRootGameObjects())
                {
                    foreach(var layout in root.GetComponentsInChildren<FruitLabGameLayout>(true))layout.ApplyLayout();
                    var gameView=root.GetComponentInChildren<GameUIView>(true);
                    if(gameView!=null)
                    {
                        var information=gameView.GetInformationButton();
                        foreach(var duplicate in root.GetComponentsInChildren<Transform>(true).Where(item=>item.name=="InformationButton" && item!=information.transform).ToArray())
                            Object.DestroyImmediate(duplicate.gameObject);
                    }
                    foreach(var layout in root.GetComponentsInChildren<FruitLabMenuLayout>(true))layout.ApplyLayout();
                    foreach(var layout in root.GetComponentsInChildren<FruitLabOnlineLayout>(true))layout.ApplyLayout();
                    foreach(var bar in root.GetComponentsInChildren<VerticalCrystalProgressView>(true))bar.ApplyLabStyle();
                    foreach(var popup in root.GetComponentsInChildren<LevelFinishPopupView>(true))popup.ApplyLabLayout();
                    foreach(var component in root.GetComponentsInChildren<Component>(true))
                        if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
                }
                EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Save styled "+sceneName);
            }
            finally {if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        }
        return "Original casual presentation restored in the real scenes; Ultra, information and stable note layout retained.";
    }

    public static void ReviewUltraNotes(Canvas canvas,Camera camera)
    {
        var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        var errors=new List<string>();Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            ValidateUltraEconomy();
            Require(canvas.GetComponentsInChildren<Transform>(true).Count(item=>item.name=="InformationButton")==1,"The serialized game scene contains one information control");
            Require(PowerUpTypeUtility.OrderedRewardTypes.Last()==RewardType.Ultra,"Ultra appends to the existing reward rotation without reordering powers");
            var definition=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9};
            LevelSaveData saved=null;
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",7);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{2,3,1});
                using(var notes=AssistanceFor(game,definition,out var view,false))
                using(var editor=new CardAnnotationController(game.Initial,game.Placement,game.UI,canvas.GetComponent<FruitLabGameLayout>(),game.Gate,null,()=>true,notes,game.Initial,game.Board))
                {
                    notes.Refresh();Require(!notes.UltraActive && !notes.IndicatorsVisible,"New games start without automatic help or indicator strips");
                    var board=game.Board.GetRectTransformOfGarden(0).position;
                    var card=game.Initial.GetRectTransformOfCardItem(0).position;
                    Require(editor.TrySelect(0),"Manual editing is optional and available");
                    editor.GetButton(0).onClick.Invoke();Require(editor.IsSelecting && !notes.IndicatorsVisible,"A red presence note keeps editing open without revealing position strips");
                    editor.GetButton(1).onClick.Invoke();Require(!notes.IndicatorsVisible,"Neutral presence notes do not reveal strips");
                    editor.GetButton(2).onClick.Invoke();
                    var manualPresence=game.Initial.GetRectTransformOfCardItem(0).GetComponent<NormalCardItemView>();
                    Require(manualPresence.HasPresenceBadge && !CardKnowledgeFrame(manualPresence) && !notes.IndicatorsVisible,
                        "The actual manual green button shows only a presence badge without opening position strips");
                    editor.GetButton(1).onClick.Invoke();Require(!manualPresence.HasPresenceBadge,"The actual neutral button clears the presence badge");
                    ClickPositionIndicator(game,editor.GetPositionButton(0));
                    Require(editor.IsSelecting && notes.IndicatorsVisible && !notes.State.IsPossible(1,0),"A direct positional X opens all strips and keeps selection");
                    for(int i=0;i<7;i++)Require(game.Initial.GetHolderView(i).IndicatorStrip.gameObject.activeSelf,"Every fruit strip becomes visible");
                    ClickPositionIndicator(game,editor.GetPositionButton(0));Require(notes.IndicatorsVisible,"Removing the last X keeps strips visible");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-manual-notes-restored.png");
                    editor.Cancel();
                    Require((board-game.Board.GetRectTransformOfGarden(0).position).sqrMagnitude<.01f && (card-game.Initial.GetRectTransformOfCardItem(0).position).sqrMagnitude<.01f,"Editing and indicator visibility cannot move board or cards");
                    notes.ResetNotes();SubmitAssistedGuess(game,new[]{1,2,3},notes);
                    Require(Enumerable.Range(1,7).All(fruit=>Enumerable.Range(0,3).All(slot=>notes.State.IsPossible(fruit,slot))),"Ordinary feedback never creates automatic notes");
                    notes.SetPresence(1,FruitPresence.Absent);
                    var profile=game.PowerProfiles;
                    Field(game.Tracker,"_ultraPowerUpCount",1);
                    var container=new Zenject.DiContainer();container.Bind<PositionAssistanceController>().FromInstance(notes);
                    container.Bind<ILevelSaveDataManager>().FromInstance(game._saves);
                    container.Bind<UltraActivationService>().FromInstance(new UltraActivationService(profile));
                    container.Bind<PowerUpUseService>().FromInstance(game.Uses);
                    var power=container.Instantiate<UltraPowerUpController>();Field(game._powers,"_ultra",power);
                    Require(power.TryApply() && power.TryApply() && profile.Snapshot().Wallet.PowerUps[RewardType.Ultra]==0,
                        "The actual controller uses the common atomic journal and repeat taps cost nothing");
                    Require(notes.UltraActive && !notes.CanResetNotes && !notes.CanEditNotes && !notes.State.HasManualChanges,"Ultra discards manual notes and disables editing/reset");
                    Require(notes.State.IsPossible(1,1) && !notes.State.IsPossible(1,0) && notes.State.Presence(1)==FruitPresence.Present,"Ultra rebuilds public history and ignores an incorrect manual exclusion");
                    var marked=(NormalCardItemView)game.Initial.GetRectTransformOfCardItem(0).GetComponent<NormalCardItemView>();
                    Require(marked.HasPresenceBadge && !CardKnowledgeFrame(marked),"Actual Ultra shows a presence badge without a card frame or real lock");
                    var before=notes.State.Capture(1,true);notes.TogglePosition(1,1);notes.SetPresence(1,FruitPresence.Absent);notes.ResetNotes();
                    Require(notes.State.Capture(1,true).Possible.SequenceEqual(before.Possible) && !editor.TrySelect(0),"Editing commands cannot alter active Ultra");
                    var provider=new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
                    Field(provider,"_saveData",game._saves);Field(provider,"_levels",new TestLevel(definition));Field(provider,"_tracker",game.Tracker);
                    Require(provider.TryCreateSnapshot(out saved) && saved.UltraActive,"Ultra activation is included in checkpoints");
                    saved=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(saved));
                    var popup=Object.Instantiate(AssetDatabase.LoadAssetAtPath<SettingsPopupView>("Assets/Prefabs/SettingsPopup.prefab"),canvas.transform,false);
                    try
                    {
                        popup.Init();popup.ConfigureNotes(notes.CanResetNotes,notes.ResetNotes);
                        Require(popup.GetComponentsInChildren<TMP_Text>(false).All(text=>text.text!="Normal yardım" && text.text!="Notları Sıfırla"),"Ultra settings omit automatic toggle and reset");
                        popup.ConfigureNotes(true,()=>{});
                        Require(popup.GetComponentsInChildren<TMP_Text>(false).Any(text=>text.text=="Notları Sıfırla"),"Normal settings retain reset only");
                    }
                    finally{Object.DestroyImmediate(popup.gameObject);}
                    foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
                    {
                        camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));
                        ((RectTransform)canvas.transform).sizeDelta=size;Field(canvas.GetComponent<FruitLabGameLayout>(),"_lastSize",Vector2.zero);
                        canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();game.Board.RefreshLayout();game.Initial.RefreshLayout();
                        var info=game.View.GetInformationButton();Require(((RectTransform)info.transform).rect.width>=32,"Information touch target");
                        var information=GameInformationPopup.Open(canvas.transform,LevelMechanic.Split,()=>{});
                        try
                        {
                            Require(information.GetComponentsInChildren<TMP_Text>(true).Any(label=>label.text==LevelInformation.Description(LevelMechanic.Split)),"Special level description is present");
                            foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)Require(information.GetComponentsInChildren<TMP_Text>(true).Any(label=>label.text==PowerUpTypeUtility.Description(type)),"Every power has a short reference description");
                            var scroll=information.GetComponentInChildren<ScrollRect>();Require(scroll.content.rect.height>scroll.viewport.rect.height,"Long information remains scrollable on short phones");
                            Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-information-"+size.x+"-"+size.y+".png");
                        }
                        finally{Object.DestroyImmediate(information.gameObject);}
                    }
                    camera.orthographicSize=406;
                    ((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-ultra-active.png");
                    var powers=canvas.GetComponentInChildren<PowerUpListHolderView>(true);powers.SetProgression(7);
                    foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)
                    {
                        var powerButton=((BaseButtonView)powers.GetPowerUpButton(type)).GetButtonRectTransform();
                        Require(powerButton.gameObject.activeInHierarchy && powerButton.parent==powers.transform,"All seven powers stay in the original visible strip");
                    }
                    Require(canvas.GetComponentsInChildren<Transform>(true).All(item=>item.name!="MorePowers" && item.name!="MorePowerOverlay"),"No overflow menu remains");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-restored-game.png");
                }
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",7);((MemorySave)Get(game,"_memory")).Save(saved);game._saves.SetLevelSaveDataAsSaved(saved);
                game.Round.Initialize();game.BuildActors();game.Results.TryAddTriedCards();
                using(var notes=AssistanceFor(game,definition,out _,false))
                {
                    notes.Refresh();Require(notes.UltraActive && notes.IndicatorsVisible && !notes.CanEditNotes,"Resume preserves Ultra and recomputes its public evidence");
                    game._saves.CreateDefaultLevelSaveData();game.Round.Initialize();game._area.RemoveResultBlocks();game.Initial.ClearInitialCardHolders();game.BuildActors();notes.Tick();
                    Require(!notes.UltraActive && !notes.IndicatorsVisible,"Retry/new round restores the initial normal state");
                    game.Target.RevealMultiplayerAnswer(new[]{2,3,1});game.Gate.Clear();
                    SubmitAssistedGuess(game,new[]{4,5,6},notes);
                    var helper=game.Helper;helper.Applied+=(sender,guess)=>game._area.RevealGuess(guess);
                    Require(helper.TryApply(),"A real detailed hint is available");notes.Tick();
                    Require(!notes.IndicatorsVisible && !notes.CanEditFruit(4),"Verified absent detail cannot enable strips or be overwritten manually");
                    notes.SetPresence(4,FruitPresence.Present);Require(notes.State.Presence(4)==FruitPresence.Unknown,"A manual green command cannot contradict a verified absent fruit");
                    notes.SetPresence(7,FruitPresence.Absent);
                    var verified=canvas.GetComponentsInChildren<FruitLabGlyph>(true).First(glyph=>glyph.name=="HelperAbsent");
                    var manual=game.Initial.GetRectTransformOfCardItem(6).GetComponentsInChildren<FruitLabGlyph>(true).First(glyph=>glyph.Type==FruitLabGlyph.Symbol.Cross);
                    Require(verified.Type==manual.Type && verified.color==manual.color && verified.rectTransform.anchorMin==manual.rectTransform.anchorMin && verified.rectTransform.anchorMax==manual.rectTransform.anchorMax,"Manual and verified absence share the same mark styling without sharing their editable state");
                }
            }
            Require(errors.Count==0,"No Unity errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/ultra-review.json",JsonConvert.SerializeObject(new {passed=true,manual_opt_in=true,stable_layout=true,public_evidence_only=true,atomic_activation=true,save_resume=true,information_layouts=4}));
        }
        finally {Application.logMessageReceived-=collect;DOTween.defaultUpdateType=previous;}
    }

    public static void ReviewIconPolish(Canvas canvas, Camera camera)
    {
        var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        var errors=new List<string>();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=9}))
            {
                Field(game.Tracker,"_levelId",7);game.BuildActors();game.Gate.Clear();
                var powers=canvas.GetComponentInChildren<PowerUpListHolderView>(true);powers.SetProgression(7);
                foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)
                {
                    var button=((Component)powers.GetPowerUpButton(type));
                    Require(button.GetComponentsInChildren<TMP_Text>(false).Length==1,"Only the power count remains visible: "+type);
                    Require(button.GetComponentsInChildren<FruitLabGlyph>(false).All(g=>g.Type!=FruitLabGlyph.Symbol.Reset),"No inherited reset arrows on powers: "+type);
                    Require(button.GetComponentsInChildren<TMP_Text>(false)[0].fontSize>=16,"Readable power count: "+type);
                }
                foreach(var button in new[]{game.View.GetCheckButton(),game.View.GetResetButton()})
                    Require(((Component)button).GetComponentsInChildren<TMP_Text>(false).Length==0,"Game actions are icon-only");
                Require(((Component)game.View.GetResetButton()).GetComponentsInChildren<FruitLabGlyph>(false).Any(g=>g.Type==FruitLabGlyph.Symbol.Eraser),"Clear uses the dedicated eraser icon");
                Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-icon-polish-game.png");
                var life=(LifeBarController)Get(game._guess,"_lifeBarController");
                var crystalView=canvas.GetComponentInChildren<VerticalCrystalProgressView>(true);
                var crystal=new VerticalCrystalProgressController(crystalView,game.Tracker,null);
                var popupView=canvas.GetComponentInChildren<LevelFinishPopupView>(true);
                var bindings=new Zenject.DiContainer();bindings.Bind<LevelFinishPopupView>().FromInstance(popupView);
                var popup=bindings.Instantiate<LevelEndPopupController>();
                var collection=new LevelSuccessRewardCollectionAnimationController(life,game.UI,crystal,popup);
                var success=new LevelSuccessAnimationManager(popup,new FadePanelController(canvas.GetComponentInChildren<FadePanelView>(true)),
                    new LevelSuccessBoardAnimationController(game.Board,game.Placement),
                    new LevelSuccessCleanupAnimationController(game.Initial,game.UI,life,game._area,game.Placement),collection);
                var previewTokens=canvas.GetComponentsInChildren<RewardTokenView>(false);
                foreach(int earnedCrystals in new[]{0,1})
                foreach(float interruptAt in new[]{.01f,1.45f,1.75f})
                {
                    life.SetLifeBar(9,new[]{new LifeBarRewardInfo(1,LifeBarRewardType.Coin,true),new LifeBarRewardInfo(3,LifeBarRewardType.Coin,true),new LifeBarRewardInfo(5,earnedCrystals>0?LifeBarRewardType.Crystal:LifeBarRewardType.Coin,true)},9);
                    var tokens=life.GetActiveRewardTokens(LifeBarRewardType.Coin).Concat(life.GetActiveRewardTokens(LifeBarRewardType.Crystal)).ToArray();
                    popup.SetOutcome(true,earnedCrystals);
                    var parent=success.SuccessLevelAnimation(earnedCrystals>0?2:3,earnedCrystals,0,0,RewardType.Bomb);
                    Tick(interruptAt);
                    var presenter=new SinglePlayerEndPresenter(null,game.UI,null,null,null,crystal,popup,success,null);
                    Field(presenter,"_ending",parent);presenter.PrepareRound();
                    Require(tokens.All(t=>t.GetRectTransform()==null),"Continuing the round immediately clears reparented tokens at "+interruptAt);
                    life.ClearBoundaries();Tick(.05f);
                    Require(tokens.All(t=>t.GetRectTransform()==null),"Round transition removes every in-flight token, with or without crystals, at "+interruptAt);
                    Require(canvas.GetComponentsInChildren<RewardTokenView>(false).All(t=>previewTokens.Contains(t)),"No orphan reward image remains in the HUD");
                }
                game.UI.RefreshCoinCounter();
            }
            Require(errors.Count==0,"Reward interruption produces no Unity errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/icon-polish-review.json",JsonConvert.SerializeObject(new {passed=true,icon_only_powers=true,icon_only_actions=true,interrupted_collections=6,orphan_tokens=0}));
        }
        finally {Application.logMessageReceived-=collect;DOTween.defaultUpdateType=previous;}
    }

    private static void ValidateUltraEconomy()
    {
        var old=new PlayerProfileData {Version=2};old.Wallet.PowerUps.Remove(RewardType.Ultra);old.Wallet.Coins=123;old.Progress.LevelId=17;
        PlayerProfileValidation.Upgrade(old);PlayerProfileValidation.Validate(old);
        Require(old.Version==PlayerProfileData.CurrentVersion && old.Wallet.PowerUps[RewardType.Ultra]==0 && old.Wallet.Coins==123 && old.Progress.LevelId==17,"Adding Ultra preserves existing balances/progression");
        var storage=new MemoryPlayerProfileStorage();
        var repository=new PlayerProfileRepository(storage,()=>{var state=new PlayerProfileData();state.Wallet.PowerUps[RewardType.Ultra]=1;return state;});
        string id=Guid.NewGuid().ToString("N");
        var before=new LevelSaveData {RoundId=id};
        var after=new LevelSaveData {RoundId=id,PowerUpUseRevision=1,UltraActive=true};
        var journal=new PowerUpUseJournal(repository);var request=new PowerUpUseRequest(RewardType.Ultra);
        Require(journal.Commit(before,after,request)==ProfileCommitResult.Applied && journal.Commit(before,after,request)==ProfileCommitResult.AlreadyApplied,"Activation is repeat-safe");
        Require(repository.Snapshot().Wallet.PowerUps[RewardType.Ultra]==0 && repository.Snapshot().Transactions.Count==1,"Only one Ultra is consumed");
        var resumed=new PowerUpUseJournal(new PlayerProfileRepository(storage,()=>throw new Exception("Must load")));
        Require(resumed.Recover(before).UltraActive,"Activation checkpoint survives reload");

    }
}

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewPowerUpUses(Canvas canvas,Camera camera)
    {
        var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        string prefs=Profile();var errors=new List<string>();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            ValidateMechanicBindings();ReviewPowerUpJournal();
            var definition=new LevelData {NumOfBoardHolders=4,NumOfCards=8,MaxNumOfTries=9};
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{4,1,2,3});
                void Wrong(){for(int card=0;card<4;card++)game.Place(card);game.Check();Tick(.5f);}
                Require(!game.Uses.CanUse(RewardType.Life),"Life unavailable before spending attempts");
                Wrong();Require(!game.Uses.CanUse(RewardType.Life),"One spent attempt is insufficient");
                Wrong();Require(game.Uses.CanUse(RewardType.Life),"Exactly two spent attempts unlock Life");
                int stock=game.Tracker.GetPowerUpCount(RewardType.Life);
                Require(game.Uses.TryApply(RewardType.Life)==PowerUpUseResult.Applied && game.Round.GetRemainingGuessCount()==9
                    && game.Tracker.GetPowerUpCount(RewardType.Life)==stock-1,"One common use grants exactly two lives and spends one item");
                Require(game.Uses.TryApply(RewardType.Life)==PowerUpUseResult.Unavailable && game.Tracker.GetPowerUpCount(RewardType.Life)==stock-1,"A duplicate click cannot overfill or spend again");
                Require(((MemorySave)Get(game,"_memory")).Data.RemainingGuessCount==9,"The granted lives are checkpointed before animation completion");
                Tick(.13f);var gain=canvas.GetComponentInChildren<LifeBarView>(true).transform.Find("LifeGain").GetComponent<TMP_Text>();
                Require(gain.text=="+2","The existing life animation displays +2");
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3,4});
                int stock=game.Tracker.GetPowerUpCount(RewardType.Bomb);
                Require(game.Uses.TryApply(RewardType.Bomb)==PowerUpUseResult.Applied && game.Target.GetTargetCardsList().SequenceEqual(new[]{1,2,3})
                    && game._counts.GetRemovedBoardHolderCount()==1 && game.Tracker.GetPowerUpCount(RewardType.Bomb)==stock-1,"Bomb commits the shortened answer and changes the live board");
                Require(game.Uses.TryApply(RewardType.Bomb)==PowerUpUseResult.Unavailable,"Bomb is single-use");
                Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetRevealingPowerUpButton());
                int revealStock=game.Tracker.GetPowerUpCount(RewardType.Revealing);
                game.PowerMessages.CloseActivePowerUp();Require(!game.Reveal.IsActive && game.Tracker.GetPowerUpCount(RewardType.Revealing)==revealStock,"Cancelling target selection consumes nothing");
                Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetRevealingPowerUpButton());
                Require(game.Reveal.TryRequestRevealCard(0) && game._info.GetCardItemInfoList()[0].isLocked
                    && game.Tracker.GetPowerUpCount(RewardType.Revealing)==revealStock-1,"Reveal returns a real lock result and debits only the selected target");
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20);Field(game.Tracker,"_pairPowerUpCount",1);Field(game.Tracker,"_splitPowerUpCount",1);
                game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{4,1,2,3});
                Require(game.Uses.TryApply(RewardType.Pair)==PowerUpUseResult.Applied && game.Pair.Current!=null,"Pair uses its prepared durable clue");
                Require(game.Uses.TryApply(RewardType.Pair)==PowerUpUseResult.Unavailable,"A provided pair cannot be charged twice");
                Require(game.Uses.TryApply(RewardType.Split)==PowerUpUseResult.Applied,"Split applies before the first guess");
                Require(!game.Uses.CanUse(RewardType.Ultra),"Split and Ultra are mutually exclusive");
                Field(game.Tracker,"_helperPowerUpCount",1);game.Helper.Applied+=(sender,guess)=>game._area.RevealGuess(guess);
                for(int card=0;card<4;card++)game.Place(card);game.Check();Tick(.5f);
                Require(game.Uses.TryApply(RewardType.Helper)==PowerUpUseResult.Applied && game._saves.GetLevelSaveData().RevealedGuessNumbers.SequenceEqual(new[]{1}),"Helper uses the existing shared detailed effect");
                Require(game.Uses.TryApply(RewardType.Helper)==PowerUpUseResult.Unavailable,"An already inspected row is not charged again");
                Field(game.Tracker,"_gameOption",GameOption.MultiPlayer);
                foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)Require(!game.Uses.CanUse(type) && game.Uses.TryApply(type)==PowerUpUseResult.Unavailable,"No multiplayer use: "+type);
            }
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20);Field(game.Tracker,"_ultraPowerUpCount",1);Field(game.Tracker,"_splitPowerUpCount",1);
                game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{4,1,2,3});
                using(var notes=AssistanceFor(game,definition,out _,false))
                {
                    var di=new Zenject.DiContainer();di.Bind<PositionAssistanceController>().FromInstance(notes);
                    di.Bind<ILevelSaveDataManager>().FromInstance(game._saves);di.Bind<PowerUpUseService>().FromInstance(game.Uses);
                    di.Bind<UltraActivationService>().FromInstance(new UltraActivationService(game.PowerProfiles));
                    var ultra=di.Instantiate<UltraPowerUpController>();Field(game._powers,"_ultra",ultra);
                    Require(ultra.TryApply() && ultra.TryApply() && notes.UltraActive && game.Tracker.GetPowerUpCount(RewardType.Ultra)==0,"Ultra shares the journal, survives zero stock and repeat activation costs nothing");
                    Require(!game.Uses.CanUse(RewardType.Split) && !((BoardSplitService)Get(game.Results,"_split")).CanApply,"Both UI and effect reject Split after Ultra");
                }
            }
            ReviewPowerUpPreparation(canvas,camera,definition);
            ReviewPowerUpInterruption(canvas,camera,definition);
            Require(prefs==Profile(),"Powerup use review preserves real player preferences");
            Require(errors.Count==0,"No powerup runtime errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/powerup-use-review.json",JsonConvert.SerializeObject(new {passed=true,life_grant=2,
                checks=new[]{"injected strategy catalog and custom effect","prepared Helper Split Bomb results reused without recalculation","seven shared CanUse/TryApply effects","central definitions","same-write stock and checkpoint","interruption recovery","duplicate receipts","later checkpoints preserved","other rounds isolated","storage failure leaves stock unchanged","actual Bomb and Reveal","cancel without charge","actual Helper Pair Split Ultra","Life +2 animation","mutual exclusion","multiplayer blocked","scene DI","real preferences preserved"}},Formatting.Indented));
        }
        finally{Application.logMessageReceived-=collect;DOTween.defaultUpdateType=previous;}
    }

    private static void ReviewPowerUpPreparation(Canvas canvas,Camera camera,LevelData definition)
    {
        ClearPreviewActors(canvas);
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3,4});
            Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out var before,true),"Prepare a real resumable round");
            var context=new PowerUpContext {State=before,Answer=before.TargetCards,History=before.TriedCardsList,Cards=before.CardItemInfoList,
                Remaining=9,Capacity=9,Slots=4,CanShrink=true};
            var split=(BoardSplitService)Get(game.Results,"_split");
            var splitAfter=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(before));
            Require(game.PowerContainer.Resolve<IPowerUpCatalog>().Get(RewardType.Split).TryPrepare(context,splitAfter,-1,out var preparedSplit),"Prepare Split once before any guess");
            before.TriedCardsList.Add(new List<int>{8,2,1,4});game.Round.AddTriedCards(new List<int>{8,2,1,4});
            var inspected=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(before));GuessEvaluation published=null;
            game.Helper.Applied+=(sender,guess)=>published=guess;
            Require(game.PowerContainer.Resolve<IPowerUpCatalog>().Get(RewardType.Helper).TryPrepare(context,inspected,-1,out var preparedHelper),"Prepare the first detailed inspection");
            before.TriedCardsList.Add(new List<int>{4,3,2,1});game.Round.AddTriedCards(new List<int>{4,3,2,1});
            Require(preparedHelper.ApplyCommitted() && published?.Number==1 && published.Cards.SequenceEqual(new[]{8,2,1,4})
                && game._saves.GetLevelSaveData().RevealedGuessNumbers.SequenceEqual(inspected.RevealedGuessNumbers),"Helper publishes the approved row and marker rather than recomputing the latest row");
            Require(!split.CanApply && preparedSplit.ApplyCommitted() && split.IsActive==splitAfter.BoardSplitApplied,"Split publishes the approved flag without a second eligibility decision");
        }
        ClearPreviewActors(canvas);
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{1,2,3,4});
            Require(GrowingSnapshot(game,definition).TryCreateSnapshot(out var before,true),"Snapshot for prepared Bomb");
            var context=new PowerUpContext {State=before,Answer=before.TargetCards,History=before.TriedCardsList,Cards=before.CardItemInfoList,
                Remaining=9,Capacity=9,Slots=4,CanShrink=true};
            var after=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(before));
            Require(game.PowerContainer.Resolve<IPowerUpCatalog>().Get(RewardType.Bomb).TryPrepare(context,after,-1,out var prepared),"Prepare Bomb's shortened answer");
            game.Target.RevealMultiplayerAnswer(new[]{8,7,6,5});
            Require(prepared.ApplyCommitted() && game.Target.GetTargetCardsList().SequenceEqual(after.TargetCards)
                && after.TargetCards.SequenceEqual(new[]{1,2,3}) && game._counts.GetBoardHolderCount()==3,"Bomb projects the saved answer rather than shortening a second live answer");
        }
        ClearPreviewActors(canvas);
        using(var game=new Fixture(canvas,camera,definition))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{4,1,2,3});Field(game.Round,"_remainingGuessCount",8);
            var effect=new ReviewLifeEffect(game._guess);var catalog=new PowerUpCatalog(new[]{effect});
            var profile=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{var p=new PlayerProfileData();p.Wallet.PowerUps[RewardType.Life]=1;return p;});
            var save=new PowerUseControlledSave();var snapshots=GrowingSnapshot(game,definition);
            var service=new PowerUpUseService(game.Tracker,new TestLevel(definition),game._saves,game.Round,game._info,game._counts,game.Target,
                new PowerUpUseJournal(profile),save,()=>snapshots,catalog,()=>null);
            Require(!game.Uses.CanUse(RewardType.Life) && service.CanUse(RewardType.Life),"Availability uses the injected strategy rather than a static catalog");
            Require(service.TryApply(RewardType.Life)==PowerUpUseResult.Applied && effect.Prepared==1 && effect.Published==1
                && game.Round.GetRemainingGuessCount()==9 && save.Data.RemainingGuessCount==9 && profile.Snapshot().Wallet.PowerUps[RewardType.Life]==0,
                "The coordinator commits and projects a custom strategy without a runtime type switch");
            bool rejected=false;try{new PowerUpCatalog(new[]{effect,effect});}catch(ArgumentException){rejected=true;}
            Require(rejected && catalog.Get(RewardType.Bomb)==null,"Duplicate registrations fail and missing effects stay unavailable");
        }
    }
    private sealed class ReviewLifeEffect : PowerUpEffect
    {
        private readonly IGuessManager _live;public int Prepared,Published;
        public ReviewLifeEffect(IGuessManager live){_live=live;}
        public override RewardType Type=>RewardType.Life;
        public override bool CanUse(PowerUpContext c,int slot=-1)=>c.Remaining>0 && c.Capacity-c.Remaining>=1;
        protected override PreparedPowerUp Prepare(PowerUpContext c,LevelSaveData after,int slot)
        {Prepared++;int before=c.Remaining;after.RemainingGuessCount=before+1;return new PreparedPowerUp(()=>{Published++;return _live.ApplyCommittedLives(before,before+1);});}
    }
    private sealed class PowerUseFailingStorage : IPlayerProfileStorage
    {
        private readonly MemoryPlayerProfileStorage _inner=new MemoryPlayerProfileStorage();
        public bool Fail;
        public PlayerProfileData Load()=>_inner.Load();
        public void Save(PlayerProfileData profile){if(Fail)throw new IOException("Simulated interrupted atomic write");_inner.Save(profile);}
    }
    private static void ReviewPowerUpJournal()
    {
        foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)
        {
            var definition=PowerUpDefinition.Get(type);
            Require(definition.Type==type && definition.TutorialKey==PowerUpTypeUtility.TutorialKey(type) && !string.IsNullOrEmpty(definition.IconName),"Central power definition: "+type);
            var before=new LevelSaveData {RoundId=Guid.NewGuid().ToString("N"),Definition=new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=9},
                TargetCards=new List<int>{1,2,3,4,5},TriedCardsList=new List<List<int>>(),RemainingGuessCount=type==RewardType.Life?7:9,
                CardItemInfoList=Enumerable.Range(1,9).Select(_=>new CardItemInfo {isExisted=true,possibleCardHolderIndicatorIndexes=Enumerable.Range(0,5).ToList()}).ToList()};
            if(type==RewardType.Helper)before.TriedCardsList.Add(new List<int>{6,2,5,4,1});
            var context=new PowerUpContext {State=before,Answer=before.TargetCards,History=before.TriedCardsList,Cards=before.CardItemInfoList,
                Remaining=before.RemainingGuessCount,Capacity=9,Slots=5,CanShrink=true};
            var after=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(before));
            var bindings=new Zenject.DiContainer();GameMonoInstaller.BindPowerUpEffects(bindings);
            var effect=bindings.Resolve<IPowerUpCatalog>().Get(type);int slot=type==RewardType.Revealing?2:-1;
            Require(effect.CanUse(context,slot) && effect.TryPrepare(context,after,slot,out _),"Effect can prepare without spending or changing live state: "+type);
            after.PowerUpUseRevision=1;
            var storage=new PowerUseFailingStorage();
            var repository=new PlayerProfileRepository(storage,()=>{var profile=new PlayerProfileData();profile.Wallet.PowerUps[type]=2;return profile;});
            repository.Snapshot();var journal=new PowerUpUseJournal(repository);var request=new PowerUpUseRequest(type,slot);
            storage.Fail=true;
            try{journal.Commit(before,after,request);throw new Exception("Storage should reject");}catch(IOException){}
            Require(repository.Snapshot().Wallet.PowerUps[type]==2 && journal.Recover(before)==before,"Interrupted write cannot debit stock: "+type);
            storage.Fail=false;
            Require(journal.Commit(before,after,request)==ProfileCommitResult.Applied && journal.Commit(before,after,request)==ProfileCommitResult.AlreadyApplied,"Receipt retry is idempotent: "+type);
            var reopened=new PlayerProfileRepository(storage,()=>throw new Exception("Must load existing profile"));
            var recovered=new PowerUpUseJournal(reopened).Recover(before);
            Require(reopened.Snapshot().Wallet.PowerUps[type]==1 && JsonConvert.SerializeObject(recovered)==JsonConvert.SerializeObject(after),"Closing before live effect/checkpoint restores the exact committed effect: "+type);
            var later=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(after));later.RemainingGuessCount--;
            Require(journal.Recover(later)==later,"A later saved guess is not rolled back: "+type);
            var other=new LevelSaveData {RoundId=Guid.NewGuid().ToString("N")};Require(journal.Recover(other)==other,"Old effects cannot leak to retry/new round: "+type);
        }
    }
}

public static partial class FruitLabTutorialEditorTools
{
    private sealed class PowerUseControlledSave : IGameSaveService
    {
        public LevelSaveData Data;public int Writes,FailOn;
        public LevelSaveData GetSavedLevel()=>Data;
        public bool HasSavedGame()=>Data!=null;
        public void DeleteSave()=>Data=null;
        public void Save(LevelSaveData state)
        {
            if(++Writes==FailOn)throw new IOException("Simulated checkpoint interruption");
            Data=JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(state));
        }
    }
    private static void ReviewPowerUpInterruption(Canvas canvas,Camera camera,LevelData definition)
    {
        foreach(int failure in new[]{0,1,2})
        {
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();
                game.Target.RevealMultiplayerAnswer(new[]{4,1,2,3});Field(game.Round,"_remainingGuessCount",7);
                var profile=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{var p=new PlayerProfileData();p.Wallet.PowerUps[RewardType.Life]=1;p.Wallet.PowerUps[RewardType.Revealing]=1;return p;});
                var journal=new PowerUpUseJournal(profile);var saves=new PowerUseControlledSave {FailOn=failure};
                var snapshots=GrowingSnapshot(game,definition);var split=(BoardSplitService)Get(game.Results,"_split");
                var service=new PowerUpUseService(game.Tracker,new TestLevel(definition),game._saves,game.Round,game._info,game._counts,game.Target,journal,saves,
                    ()=>snapshots,new PowerUpCatalog(new IPowerUpEffect[]{new LifePowerUpEffect(()=>failure==0?throw new IOException("Interrupted before live effect"):game._guess),new RevealPowerUpEffect(()=>game.Placement)}),()=>null);
                int recovery=0;service.RecoveryRequired+=()=>recovery++;
                var outcome=service.TryApply(RewardType.Life);
                if(failure==1)
                {
                    Require(outcome==PowerUpUseResult.StorageFailed && recovery==0 && profile.Snapshot().Wallet.PowerUps[RewardType.Life]==1
                        && game.Round.GetRemainingGuessCount()==7,"Failure before the base checkpoint spends nothing and changes no live state");
                }
                else
                {
                    Require(outcome==PowerUpUseResult.ResumeRequired && recovery==1 && profile.Snapshot().Wallet.PowerUps[RewardType.Life]==0,
                        "An interrupted committed effect requests recovery and debits exactly once");
                    Require(journal.Recover(saves.Data).RemainingGuessCount==9,"Recovery includes both lives even before live application or final checkpoint");
                    Require(service.TryApply(RewardType.Life)==PowerUpUseResult.Unavailable && profile.Snapshot().Transactions.Count==1,"An interrupted pending operation cannot be charged again");
                    var restored=journal.Recover(saves.Data);
                    game._saves.SetLevelSaveDataAsSaved(restored);game.Round.Initialize();game._info.Initialize();
                    Require(service.CanUse(RewardType.Revealing,0),"Reloading the same round clears the failed live-instance guard and restores other powers");
                }
            }
        }
        ClearPreviewActors(canvas);
        var dangerDefinition=definition.Copy();dangerDefinition.Mechanic=LevelMechanic.Dynamite;
        using(var game=new Fixture(canvas,camera,dangerDefinition))
        {
            Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(new[]{4,1,2,3});
            using(var danger=new DynamiteService(new TestLevel(dangerDefinition),game.Tracker,game._saves,game.Target,game._info,game.Placement,game._guess))
            {
                danger.Prepare();danger.Current.BoardIndex=0;game.Results.GuessResolved+=(sender,guess)=>danger.Resolve(guess);
                for(int turn=0;turn<2;turn++){for(int card=0;card<4;card++)game.Place(card);game.Check();Tick(.5f);}
                Require(danger.Current.GuessesUsed==2 && game.Uses.TryApply(RewardType.Life)==PowerUpUseResult.Applied
                    && game.Round.GetRemainingGuessCount()==9 && danger.Current.GuessesUsed==2 && !danger.Current.Disarmed,
                    "Paid Life grants two global attempts without extending the four-move dynamite deadline");
            }
        }
    }
}

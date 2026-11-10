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
                    var profile=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{var p=new PlayerProfileData();p.Wallet.PowerUps[RewardType.Ultra]=1;return p;});
                    var service=new UltraActivationService(profile);
                    var container=new Zenject.DiContainer();container.Bind<PositionAssistanceController>().FromInstance(notes);
                    container.Bind<ILevelSaveDataManager>().FromInstance(game._saves);container.Bind<UltraActivationService>().FromInstance(service);
                    var power=container.Instantiate<UltraPowerUpController>();
                    var activationSnapshot=new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
                    Field(activationSnapshot,"_saveData",game._saves);Field(activationSnapshot,"_levels",new TestLevel(definition));Field(activationSnapshot,"_tracker",game.Tracker);
                    int saves=0;
                    power.Configure(()=>{saves++;return activationSnapshot.TryCreateSnapshot(out _,true);});
                    Require(power.TryApply() && power.TryApply() && saves==2 && profile.Snapshot().Wallet.PowerUps[RewardType.Ultra]==0,"The actual power controller checkpoints before/after activation and repeat taps cost nothing");
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
                    var helper=new HelperEffect(game.Round,game.Target,game._saves);helper.Applied+=(sender,guess)=>game._area.RevealGuess(guess);
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
        var activation=new UltraActivationService(repository);string id=Guid.NewGuid().ToString("N");
        Require(!activation.IsActive(id) && activation.TryActivate(id) && activation.TryActivate(id),"Activation is repeat-safe");
        Require(repository.Snapshot().Wallet.PowerUps[RewardType.Ultra]==0 && repository.Snapshot().Transactions.Count==1,"Only one Ultra is consumed");
        var resumed=new UltraActivationService(new PlayerProfileRepository(storage,()=>throw new Exception("Must load")));
        Require(resumed.IsActive(id) && !resumed.TryActivate(Guid.NewGuid().ToString("N")),"Receipt survives reload and empty stock cannot activate another round");
    }
}

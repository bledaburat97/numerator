using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using Game;
using Newtonsoft.Json;
using Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewPopupArtwork(Canvas canvas,Camera camera)
    {
        var original=((RectTransform)canvas.transform).sizeDelta;float ortho=camera.orthographicSize;
        try
        {
            foreach(var size in new[]{new Vector2(320,568),new Vector2(375,812)})
            {
                ((RectTransform)canvas.transform).sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));
                var information=GameInformationPopup.Open(canvas.transform,LevelMechanic.Growing,()=>{});
                try
                {
                    Require(information.transform.Find("Panel").GetComponent<UnityEngine.UI.Image>().sprite.name=="popup","Information uses the supplied popup artwork");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-popup-information-"+(int)size.y+".png");
                }
                finally{Object.DestroyImmediate(information.gameObject);}
                var prefab=AssetDatabase.LoadAssetAtPath<Menu.FruitCollectionView>("Assets/Prefabs/FruitCollection.prefab");
                var collection=Object.Instantiate(prefab,canvas.transform,false);
                try
                {
                    FruitLabStyle.Stretch((RectTransform)collection.transform);collection.SetEmbedded(true);collection.SetVisible(true);
                    collection.CollectionPopup.SetVisible(true);
                    var panel=collection.CollectionPopup.transform.Find("Panel");
                    Require(panel.GetComponent<UnityEngine.UI.Image>().sprite.name=="popup","Collection uses the supplied popup artwork");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-popup-collection-"+(int)size.y+".png");
                }
                finally{Object.DestroyImmediate(collection.gameObject);}
            }
        }
        finally{((RectTransform)canvas.transform).sizeDelta=original;camera.orthographicSize=ortho;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();}
    }

    public static string BakePowerUpRewardPresentation()
    {
        var path="Assets/Scenes/Game.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            Find<FruitLabGameLayout>(scene).ApplyLayout();
            var view=PowerUpRewardView.Ensure(Find<Canvas>(scene));view.Hide();
            foreach(var component in Find<Canvas>(scene).GetComponentsInChildren<Component>(true))
            {EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Save reward presentation");
            return "Saved central powerup reward overlay in Game";
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    public static void ReviewPowerUpRewardPresentation(Canvas canvas,Camera camera)
    {
        var oldUpdate=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        string before=Profile();var errors=new List<string>();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert || message.Contains("DOTWEEN ►"))errors.Add(message);};
        Application.logMessageReceived+=collect;
        int presentations=0;
        try
        {
            foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
            {
                ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));
                using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=10}))
                {
                    game.BuildActors();game.Gate.Clear();var view=PowerUpRewardView.Ensure(canvas);
                    using(var presenter=new PowerUpRewardPresenter(view,game.Tracker,new HapticController()))
                    foreach(RewardType reward in Enum.GetValues(typeof(RewardType)))
                    {
                        int stock=game.Tracker.GetPowerUpCount(reward),closed=0;
                        presenter.Present(reward,()=>closed++);view.ClaimButton.onClick.Invoke();
                        Require(presenter.IsPresenting && closed==0,"Intro cannot be skipped by an early claim");
                        Tick(.45f);
                        Require(view.GetComponentInChildren<RewardBurstGraphic>().GetComponent<CanvasRenderer>()!=null,"The halo and star mesh has a canvas renderer");
                        var center=((RectTransform)canvas.transform).TransformPoint(((RectTransform)canvas.transform).rect.center);
                        Require(Vector3.Distance(center,view.Hero.position)<.05f,"Reward uses the real screen center");
                        Require(view.Hero.rect.width>=size.x*.30f && view.Hero.rect.width<=size.x*.36f,"Reward is roughly one third of the screen width");
                        Require(((RectTransform)view.ClaimButton.transform).rect.height>=48,"Claim has a comfortable touch target");
                        Require(view.GetComponentsInChildren<TMP_Text>().Any(label=>label.text=="Güçlendirici kazandın!"),"Short readable reward title");
                        Require(!view.GetComponentsInChildren<TMP_Text>().Any(label=>label.text=="×1" || label.text=="x1"),"Reward has no redundant quantity label");
                        var small=(RectTransform)view.transform.Find("InventoryReward");var amount=(RectTransform)view.transform.Find("InventoryCount");
                        Require(Mathf.Abs(small.anchoredPosition.x-small.rect.width*.5f+amount.anchoredPosition.x+amount.rect.width*.5f)<.1f,"Inventory icon and amount are centered together");
                        Tick(2);Require(presenter.IsPresenting && view.IsVisible && closed==0,"Reward waits for player acknowledgement");
                        if(reward==RewardType.Ultra){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-powerup-earned-"+(int)size.y+".png");}
                        TapGarden(canvas,camera,EventSystem.current,view.ClaimButton);view.ClaimButton.onClick.Invoke();
                        string beforeClaim=Mathf.Max(0,stock-1).ToString();
                        Tick(.2f);Require(amount.GetComponent<TMP_Text>().text==beforeClaim,"Inventory amount stays at the previous count during flight");
                        Tick(.15f);Require(amount.GetComponent<TMP_Text>().text==Mathf.Max(1,stock).ToString(),"Inventory amount advances when the reward arrives");
                        Require(Vector2.Distance(view.Hero.anchoredPosition,small.anchoredPosition)<.1f,"Reward stays at its destination after the count updates");
                        Tick(.5f);
                        Require(!presenter.IsPresenting && !view.IsVisible && closed==1,"Double claim finishes exactly once and removes its blocker");
                        Require(game.Tracker.GetPowerUpCount(reward)==stock,"Presentation never writes inventory");presentations++;
                    }
                }
            }
            var receipt=PowerUpRewardView.Ensure(canvas);
            foreach(int finalCount in new[]{8,10})
            {
                receipt.Bind(RewardType.Ultra,finalCount,()=>{});var arrival=receipt.PlayIntro();Tick(.5f);
                var label=receipt.transform.Find("InventoryCount").GetComponent<TMP_Text>();
                Require(label.text==(finalCount-1).ToString(),"Saved stock is presented as its pre-award count");
                var flight=receipt.PlayClaim();Tick(.2f);Require(label.text==(finalCount-1).ToString(),"7-to-8 and 9-to-10 keep the old count until arrival");
                Tick(.15f);Require(label.text==finalCount.ToString(),"The counter changes at the arrival milestone");
                Require(Vector2.Distance(receipt.Hero.anchoredPosition,((RectTransform)receipt.transform.Find("InventoryReward")).anchoredPosition)<.1f,"Digit growth keeps the arriving reward on the small icon");
                Tick(.5f);receipt.Hide();
            }
            ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();camera.orthographicSize=Mathf.Max(406,375/(2*camera.aspect));
            var definition=new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=10};
            using(var game=new Fixture(canvas,camera,definition))
            {
                Field(game.Tracker,"_levelId",17);Field(game.Tracker,"_giftStarCount",5);Field(game.Tracker,"_currentRewardType",RewardType.Bomb);
                using(var rewards=new PowerUpRewardPresenter(PowerUpRewardView.Ensure(canvas),game.Tracker,new HapticController()))
                using(var session=CreateContractSession(game,definition))
                {
                    var end=Get(session,"_endPresenter");var popup=(LevelEndPopupController)Get(end,"_popup");Field(popup,"_rewardPresentation",rewards);
                    session.Begin(LevelEntryMode.New);Tick(.6f);game.Gate.Clear();
                    var answer=new[]{3,1,5,7,9};game.Target.RevealMultiplayerAnswer(answer);
                    for(int i=0;i<answer.Length;i++)Require(game.Placement.TryPlaceCardOnBoard(answer[i]-1,i),"Winning placement");Tick(.3f);
                    int stock=game.Tracker.GetPowerUpCount(RewardType.Bomb);game.Check();
                    Require(game.Tracker.GetPowerUpCount(RewardType.Bomb)==stock+1,"Real win commits its powerup before presentation");
                    Tick(.9f);var finish=canvas.GetComponentInChildren<LevelFinishPopupView>(true);
                    Require(finish.GetText().text=="Çözdün!","Real win uses the requested success title");
                    Require(!finish.transform.Find("RewardStage/Outcome").gameObject.activeSelf,"The extra title check stays hidden after outcome and layout refreshes");
                    Require(!finish.GetComponentInChildren<FruitCollectionAwardView>(true).transform.Find("Collection").gameObject.activeSelf,"Fruit award has no leading collection icon");
                    Require(!finish.GetButton(LevelFinishButtonType.Game).GetCanvasGroup().blocksRaycasts,"An earned powerup blocks early continuation");
                    for(int i=0;i<40;i++)Tick(.1f);
                    Require(rewards.IsPresenting,"Real crystal collection opens the acknowledgement overlay");
                    PowerUpRewardView.Ensure(canvas).ClaimButton.onClick.Invoke();for(int i=0;i<12;i++)Tick(.1f);
                    Require(!rewards.IsPresenting && finish.GetButton(LevelFinishButtonType.Game).GetCanvasGroup().blocksRaycasts,"Claim enables continuation");
                    Require(game.Tracker.GetPowerUpCount(RewardType.Bomb)==stock+1,"Claim never gives a second saved award");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-game-win-refreshed.png");
                    ClickContractGameButton(canvas);for(int i=0;i<20;i++)Tick(.1f);
                    Require(session.State==LevelFlowState.Playing,"Next round is playable after claim without stale blockers");
                    int closed=0;rewards.Present(RewardType.Bomb,()=>closed++);Tick(.12f);rewards.Cancel();Tick(1);
                    Require(!rewards.IsPresenting && closed==0 && !PowerUpRewardView.Ensure(canvas).IsVisible,"Cancelled intro releases the UI without firing completion");
                    rewards.Present(RewardType.Bomb,()=>closed++);Tick(.5f);PowerUpRewardView.Ensure(canvas).ClaimButton.onClick.Invoke();Tick(.1f);rewards.Cancel();Tick(1);
                    Require(closed==0 && !rewards.IsPresenting,"Cancelled collection flight has no stale callbacks");
                }
            }
            for(int previous=0;previous<6;previous++)for(int earned=0;earned<=3;earned++)
                Require(PowerUpRewardPresenter.EarnsReward(previous,earned)==(previous+earned>=6),"Reward threshold agrees with committed wallet rules");
            Require(before==Profile() && errors.Count==0,"No preference mutations or Unity/tween errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/powerup-reward-review.json",JsonConvert.SerializeObject(new{passed=true,presentations,
                checks=new[]{"7 rewards x 4 phone layouts","true screen center","wait for claim","actual claim raycast","double tap","no inventory writes","previous count until arrival","7 to 8 and 9 to 10 receipt","real singleplayer win and crystal flow","continue blocked until claim","next round input","cancel during intro and flight","real PlayerPrefs unchanged"}},Formatting.Indented));
        }
        finally{Application.logMessageReceived-=collect;DOTween.defaultUpdateType=oldUpdate;}
    }
}

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

public static partial class FruitLabTutorialEditorTools
{
    public static string BakePowerUpRewardPresentation()
    {
        var path="Assets/Scenes/Game.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var view=PowerUpRewardView.Ensure(Find<Canvas>(scene));view.Hide();
            foreach(var component in view.GetComponentsInChildren<Component>(true))EditorUtility.SetDirty(component);
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
                        Tick(2);Require(presenter.IsPresenting && view.IsVisible && closed==0,"Reward waits for player acknowledgement");
                        if(reward==RewardType.Ultra){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-powerup-earned-"+(int)size.y+".png");}
                        TapGarden(canvas,camera,EventSystem.current,view.ClaimButton);view.ClaimButton.onClick.Invoke();
                        Tick(.8f);
                        Require(!presenter.IsPresenting && !view.IsVisible && closed==1,"Double claim finishes exactly once and removes its blocker");
                        Require(game.Tracker.GetPowerUpCount(reward)==stock,"Presentation never writes inventory");presentations++;
                    }
                }
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
                    Require(!finish.GetButton(LevelFinishButtonType.Game).GetCanvasGroup().blocksRaycasts,"An earned powerup blocks early continuation");
                    for(int i=0;i<40;i++)Tick(.1f);
                    Require(rewards.IsPresenting,"Real crystal collection opens the acknowledgement overlay");
                    PowerUpRewardView.Ensure(canvas).ClaimButton.onClick.Invoke();for(int i=0;i<12;i++)Tick(.1f);
                    Require(!rewards.IsPresenting && finish.GetButton(LevelFinishButtonType.Game).GetCanvasGroup().blocksRaycasts,"Claim enables continuation");
                    Require(game.Tracker.GetPowerUpCount(RewardType.Bomb)==stock+1,"Claim never gives a second saved award");
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
                checks=new[]{"7 rewards x 4 phone layouts","true screen center","wait for claim","actual claim raycast","double tap","no inventory writes","real singleplayer win and crystal flow","continue blocked until claim","next round input","cancel during intro and flight","real PlayerPrefs unchanged"}},Formatting.Indented));
        }
        finally{Application.logMessageReceived-=collect;DOTween.defaultUpdateType=oldUpdate;}
    }
}

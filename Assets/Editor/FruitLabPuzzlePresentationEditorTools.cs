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

public static partial class FruitLabTutorialEditorTools
{
    public static string BakePuzzlePresentation()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        try
        {
            var popup=Find<LevelFinishPopupView>(scene);FruitLabStyle.Font=popup.GetText().font;
            popup.SetOutcome(true);popup.SetText("Çözdün!");popup.ApplyLabLayout();popup.SetStatus(false);
            var strip=popup.GetComponentInChildren<FruitCollectionAwardView>(true);
            Layer(popup.transform);
            Require(strip!=null && strip.GetComponentsInChildren<Image>(true).Length==5,"Collection award icons are authored in Game scene");
            foreach(var component in popup.GetComponentsInChildren<Component>(true))
            {
                EditorUtility.SetDirty(component);
                if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Game scene presentation saves");
            return "Saved collection summary and reward group in Game scene";
        }
        finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }

    public static void ReviewPuzzlePresentation(Canvas canvas,Camera camera)
    {
        string before=Profile();var errors=new List<string>();var oldUpdate=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert || (type==LogType.Warning && message.Contains("DOTWEEN") && message.Contains("error")))errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            ReviewPuzzleFeedback(canvas,camera);ReviewPuzzleLayouts(canvas,camera);ReviewPuzzleWinFlow(canvas,camera);
            var source=new[]{3,1};var result=new SinglePlayerRoundResult(true,1,0,0,0,0,RewardType.Bomb,2,earnedFruits:source);source[0]=9;
            Require(result.EarnedFruits.SequenceEqual(new[]{3,1}),"Fruit presentation owns a snapshot of the actual awarded answer");
            Require(new SinglePlayerRoundResult(false,1,0,0,0,0,RewardType.Bomb,2,earnedFruits:source).EarnedFruits.Count==0,"A loss never presents a collection gain");
            Require(before==Profile(),"Presentation checks preserve real PlayerPrefs");
            Require(errors.Count==0,"No Unity errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/puzzle-presentation-review.json",JsonConvert.SerializeObject(new {passed=true,
                checks=new[]{"single-player solved title","answer before reward details","continue during reward animation","token cancellation","actual fruit order and quantity","four phone sizes","board and history remain visible","goal/crystal/collection do not overlap","new result only","no resume or scroll replay","pooled tween reset","split feedback","multiplayer and failure preserved","real player prefs unchanged"}},Formatting.Indented));
        }
        finally {Application.logMessageReceived-=collect;DOTween.defaultUpdateType=oldUpdate;}
    }
    private static void ReviewPuzzleFeedback(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
        using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=10}))
        {
            game.BuildActors();var area=canvas.GetComponentInChildren<ResultAreaView>(true);
            Func<ResultBlockModel> row=()=>new ResultBlockModel {finalNumbers=new List<int>{3,1,5,7,9},correctPosCount=0,wrongPosCount=2};
            game._area.AddResultBlock(row(),false);FlushHistory(area);Require(!area.GetVisibleRow(0).IsFeedbackAnimating,"Saved history does not animate");
            game._area.AddResultBlock(row());FlushHistory(area);Tick(.10f);
            var latest=area.GetVisibleRow(1);var latestCount=(TMP_Text)Get(latest,"_correctCount");
            Require(latest.IsFeedbackAnimating && latestCount.rectTransform.localScale.x>1,"New feedback numbers get a short emphasis, including zero");
            Require(!area.GetVisibleRow(0).IsFeedbackAnimating && latest.GetRectTransform().localScale==Vector3.one,"Previous rows and row geometry remain still");
            Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-puzzle-new-feedback.png");
            Tick(.3f);Require(!latest.IsFeedbackAnimating && latestCount.rectTransform.localScale==Vector3.one && latestCount.color==FruitLabStyle.Text,"Feedback returns to its normal style without looping");
            var split=row();split.groups=new[]{new GuessGroupFeedback(0,2,0,1),new GuessGroupFeedback(2,3,1,1)};
            game._area.AddResultBlock(split);FlushHistory(area);Tick(.08f);
            Require(area.GetVisibleRow(2).IsSplit && area.GetVisibleRow(2).IsFeedbackAnimating,"Split boards emphasize both local result groups");
            for(int i=0;i<35;i++)game._area.AddResultBlock(row(),false);FlushHistory(area);
            int last=area.HistoryCount-1;area.PlayFeedbackForRow(last);FlushHistory(area);Tick(.08f);
            var scroll=area.GetComponentInParent<ScrollRect>();scroll.verticalNormalizedPosition=1;scroll.onValueChanged.Invoke(Vector2.up);
            Require(area.AllocatedRowCount<18,"Feedback keeps the bounded history pool");
            scroll.verticalNormalizedPosition=0;scroll.onValueChanged.Invoke(Vector2.zero);FlushHistory(area);
            Require(!area.GetVisibleRow(last).IsFeedbackAnimating,"Recycling and returning to the last result do not replay the emphasis");
        }
    }
    private static void FlushHistory(ResultAreaView area)=>typeof(ResultAreaView).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(area,null);

    private static void ReviewPuzzleLayouts(Canvas canvas,Camera camera)
    {
        foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
        foreach(int slots in new[]{2,5})foreach(int crystals in new[]{0,2})foreach(bool goal in new[]{false,true})
        {
            ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
            using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=slots,NumOfCards=9,MaxNumOfTries=9}))
            {
                game.BuildActors();var popup=canvas.GetComponentInChildren<LevelFinishPopupView>(true);
                var fruits=new[]{3,1,5,7,9}.Take(slots).ToArray();popup.SetOutcome(true,crystals);
                popup.SetAchievementTarget(goal?"Meyve Koleksiyoncusu: 48/50":null);popup.PrepareRewardDetails(fruits);
                popup.SetText("Çözdün!");popup.GetText().rectTransform.localScale=Vector3.one;popup.SetStatus(true);
                var controller=new LevelEndPopupController(popup);controller.InitButton(LevelFinishButtonType.Game,"Bölüm 2",()=>{});controller.InitButton(LevelFinishButtonType.Menu,"Menü",()=>{});
                popup.RevealRewardDetails(.18f).Complete(true);controller.ChangeFadeButtons(0,1).Complete(true);Canvas.ForceUpdateCanvases();
                var panel=(RectTransform)popup.transform;var board=(RectTransform)canvas.GetComponentInChildren<BoardAreaView>(true).transform;
                Require(BoundsIn(board,canvas.transform).yMin>=BoundsIn(panel,canvas.transform).yMax+4,"Popup remains below the answer board: "+size);
                var strip=popup.GetComponentInChildren<FruitCollectionAwardView>(true);
                Require(strip.Count==slots && strip.GetComponentsInChildren<Image>().Length==slots,"Summary shows exactly the awarded fruit count");
                var icons=strip.GetComponentsInChildren<Image>();for(int i=0;i<icons.Length;i++)Require(icons[i].sprite==FruitLabStyle.Icon(((FruitId)fruits[i]).ToString()),"Summary preserves the winning board's order");
                Require(((RectTransform)((Component)popup.GetButton(LevelFinishButtonType.Game)).transform).rect.height>=44,"The success continuation target is at least 44 units");
                var subtitle=popup.GetSubtitle();var bar=(RectTransform)((Component)popup.GetVerticalCrystalProgressView()).transform;
                Rect awardBounds=BoundsIn((RectTransform)strip.transform,panel),barBounds=BoundsIn(bar,panel);
                if(goal)Require(awardBounds.yMin>=BoundsIn(subtitle.rectTransform,panel).yMax-.1f,"Fruit and goal rows do not overlap");
                if(crystals>0)Require((goal?BoundsIn(subtitle.rectTransform,panel).yMin:awardBounds.yMin)>=barBounds.yMax-.1f,"Crystal bar stays below the collection and goal");
                if(slots==5 && goal && crystals==2)
                {Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-puzzle-solved-"+(int)size.y+".png");}
                popup.SetRewardPresentation();Require(popup.GetText().text=="Çözdün!","Powerup presentation retains the solved title");
                popup.SetMultiplayerResult(true,false,RewardType.Bomb);popup.SetText("Kazandın!");
                Require(!strip.gameObject.activeSelf && popup.GetText().text=="Kazandın!","Duel result keeps its own reward presentation");
                popup.SetOutcome(false);Require(!strip.gameObject.activeSelf,"Failure does not show earned fruits");popup.SetStatus(false);
            }
        }
    }
    private static Rect BoundsIn(RectTransform rect,Transform parent)
    {var corners=new Vector3[4];rect.GetWorldCorners(corners);var min=parent.InverseTransformPoint(corners[0]);var max=parent.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(min.x,min.y,max.x,max.y);}

    private static void ReviewPuzzleWinFlow(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=new Vector2(375,812);canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
        var level=new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=9};
        using(var game=new Fixture(canvas,camera,level))
        {
            Field(game.Tracker,"_levelId",17);
            using(var session=CreateContractSession(game,level))
            {
                session.Begin(LevelEntryMode.New);Tick(.6f);game.Gate.Clear();var answer=new[]{3,1,5,7,9};game.Target.RevealMultiplayerAnswer(answer);
                for(int i=0;i<answer.Length;i++)Require(game.Placement.TryPlaceCardOnBoard(answer[i]-1,i),"Actual winning card placement");Tick(.3f);
                var collection=Get(Get(Get(session,"_endPresenter"),"_success"),"_rewardCollectionAnimationController");
                var life=(ILifeBarController)Get(collection,"_lifeBarController");
                var tokens=life.GetActiveRewardTokens(LifeBarRewardType.Coin).Concat(life.GetActiveRewardTokens(LifeBarRewardType.Crystal)).ToArray();game.Check();
                var popup=canvas.GetComponentInChildren<LevelFinishPopupView>(true);var details=(CanvasGroup)Get(popup,"rewardDetailsGroup");
                Require(session.IsGameOver && popup.GetText().text=="Çözdün!" && details.alpha==0,"The outcome commits and the solved title precedes reward details");
                Tick(.59f);Require(details.alpha==0,"The board's left-to-right reveal gets the first attention");
                Tick(.37f);Require(details.alpha>.99f,"Collection details follow the solved answer");
                Require(popup.GetComponentInChildren<FruitCollectionAwardView>(true).Count==5,"The real win uses the committed answer's fruits");
                Require(popup.GetSubtitle().text.Contains("1/10"),"The goal line reflects the same committed fruit award");
                Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-puzzle-solved-game.png");
                int next=game.Tracker.GetLevelId();ClickContractGameButton(canvas);
                Require(session.State==LevelFlowState.Transitioning,"Continue is clickable before reward flights finish");
                for(int i=0;i<25;i++)Tick(.1f);game.Gate.Clear();
                Require(session.State==LevelFlowState.Playing && game.Tracker.GetLevelId()==next,"Early continuation creates the next playable level without advancing twice");
                Require(tokens.All(token=>token.GetRectTransform()==null),"Early continuation cleans up old life-bar rewards");
                game._guess.FailRound();for(int i=0;i<25;i++)Tick(.1f);
                Require(popup.GetText().text=="Tekrar dene" && !popup.GetComponentInChildren<FruitCollectionAwardView>(true).gameObject.activeSelf,"A later loss clears the previous collection summary");
            }
        }
    }
}

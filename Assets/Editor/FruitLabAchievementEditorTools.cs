using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using Menu;
using Newtonsoft.Json;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object=UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeAchievements()
    {
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/Menu.unity");bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity",OpenSceneMode.Additive);
        try
        {
            var menu=Find<MenuUIView>(scene);var parent=menu.GetCollectionView().transform.parent;
            FruitLabStyle.Font=menu.GetComponentInChildren<TMP_Text>(true).font;
            var previous=menu.GetAchievementsView();if(previous!=null)Object.DestroyImmediate(previous.gameObject);
            var root=new GameObject("Achievements",typeof(RectTransform),typeof(AchievementsView));
            var view=root.GetComponent<AchievementsView>();root.transform.SetParent(parent,false);
            FruitLabStyle.Stretch((RectTransform)root.transform);
            var rules=new AchievementRules(PlayerProfileRuntime.LoadSettings());view.Render(rules.Read(new PlayerProfileData()));view.ApplyLayout();Layer(root.transform);
            view.SetVisible(false);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Achievements.prefab");
            Object.DestroyImmediate(root);
            root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Achievements.prefab"),parent);
            view=root.GetComponent<AchievementsView>();FruitLabStyle.Stretch((RectTransform)root.transform);
            var entry=menu.GetAchievementsButton() as BaseButtonView ?? Button(menu.transform,"AchievementsButton",false);
            entry.SetText("Başarılar");Set(menu,"achievementsButton",entry);Set(menu,"achievementsView",view);
            Find<FruitLabMenuLayout>(scene).ApplyLayout();
            foreach(var component in scene.GetRootGameObjects().SelectMany(item=>item.GetComponentsInChildren<Component>(true)))
                if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
            EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Achievements menu saves");
            return "Saved editable Achievements.prefab and menu button";
        }
        finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }

    public static void ReviewAchievements(Canvas canvas,Camera camera)
    {
        string before=Profile();var errors=new List<string>();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            FruitLabAchievementRuleChecks.Review(Folder,PlayerProfileRuntime.LoadSettings(),PlayerProfileRuntime.CreateRewardRules());ReviewCollectionHubUi(canvas,camera);ReviewAchievementTarget(canvas,camera);
            Require(before==Profile(),"Achievement review preserves real PlayerPrefs");
            Require(errors.Count==0,"No Unity errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/achievements-review.json",JsonConvert.SerializeObject(new {passed=true,checks=new[]{
                "shared single/duel/garden grants", "source and lifetime targets", "multiple unlocks in one commit", "durable reward receipts", "restart and replay", "failed write rollback and retry", "backfill and config edits", "adjustments excluded", "four mobile UI sizes", "real menu/close raycasts", "live badge-only progress", "level-end goal layout", "real prefs unchanged"}},Formatting.Indented));
        }
        finally {Application.logMessageReceived-=collect;}
    }

    private static void ReviewAchievementTarget(Canvas canvas,Camera camera)
    {
        foreach(var size in new[]{new Vector2(320,568),new Vector2(375,812)})
        foreach(int crystals in new[]{0,2})
        {
            ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=size;canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
            using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=4,NumOfCards=9,MaxNumOfTries=8}))
            {
                game.BuildActors();var view=canvas.GetComponentInChildren<LevelFinishPopupView>(true);
                view.SetOutcome(true,crystals);view.PrepareRewardDetails(new[]{1,2,3,4});view.SetText("Kazandın!");view.SetStatus(true);view.GetText().rectTransform.localScale=Vector3.one;
                view.GetButton(LevelFinishButtonType.Game).SetText("Bölüm 2");view.GetButton(LevelFinishButtonType.Menu).SetText("Menü");
                Canvas.ForceUpdateCanvases();var goal=view.GetSubtitle();
                Require(!goal.gameObject.activeSelf,"The level-end popup does not show the closest achievement target");
                if(crystals>0)
                {
                    var bar=(RectTransform)((Component)view.GetVerticalCrystalProgressView()).transform;
                    Require(!bar.Find("CrystalTitle").gameObject.activeSelf && !bar.Find("CrystalCount").gameObject.activeSelf,"Level-end progress is icon-only");
                }
                Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-achievement-goal-"+(int)size.y+"-"+crystals+".png");view.SetStatus(false);
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static string BakeMobilePolish()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        string result = BakePairViews();
        var popup = PrefabUtility.LoadPrefabContents(MessagePath);
        try
        {
            var view = popup.GetComponent<TutorialMessagePopupView>();
            var data = new SerializedObject(view);
            var secondary = data.FindProperty("secondaryButton").objectReferenceValue as BaseButtonView;
            if (secondary != null) Object.DestroyImmediate(secondary.gameObject);
            data.Update(); data.FindProperty("secondaryButton").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo();
            view.Init(); view.Show("Kristal İpuçları", "Kazanamadığın kristaller oyun içinde sana yardım eder.", true);
            PrefabUtility.SaveAsPrefabAsset(popup,MessagePath);
        }
        finally { PrefabUtility.UnloadPrefabContents(popup); }
        BakePowerUpBarLabels();
        return result + "; portrait locked; one-button tutorial and shared powerup bar labels saved in prefab, Game and Menu";
    }

    private static void BakePowerUpBarLabels()
    {
        const string path = "Assets/Prefabs/UI/CrystalProgressBar.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var subtitle = root.transform.Find("NextReward");
            if (subtitle != null) Object.DestroyImmediate(subtitle.gameObject);
            root.GetComponent<VerticalCrystalProgressView>().ApplyLabStyle();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (string scenePath in new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/Menu.unity" })
        {
            var scene = SceneManager.GetSceneByPath(scenePath); bool close = !scene.isLoaded;
            if (close) scene = EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                foreach (var view in scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<VerticalCrystalProgressView>(true)))
                {
                    view.ApplyLabStyle();
                    foreach (var component in view.GetComponentsInChildren<Component>(true))
                    {
                        EditorUtility.SetDirty(component);
                        if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (close) EditorSceneManager.CloseScene(scene,true); }
        }
    }

    private static string BakePairRopeViews()
    {
        var root = new GameObject("PairRope",typeof(RectTransform),typeof(CanvasGroup),typeof(PairClueView));
        try
        {
            var canvasGroup = root.GetComponent<CanvasGroup>(); canvasGroup.interactable = true; canvasGroup.blocksRaycasts = true;
            var child = new GameObject("Cord",typeof(RectTransform),typeof(CanvasRenderer),typeof(PairRopeGraphic));
            child.transform.SetParent(root.transform,false); FruitLabStyle.Stretch((RectTransform)child.transform);
            child.GetComponent<PairRopeGraphic>().raycastTarget = true;
            Set(root.GetComponent<PairClueView>(),"rope",child.GetComponent<PairRopeGraphic>());
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,PairPath);
        }
        finally { Object.DestroyImmediate(root); }
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Game.unity"); bool close = !scene.isLoaded;
        if (close) scene = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity",OpenSceneMode.Additive);
        try
        {
            var game = scene.GetRootGameObjects().SelectMany(item=>item.GetComponentsInChildren<GameUIView>(true)).Single();
            var canvas = game.GetComponentInParent<Canvas>();
            var previous = game.GetPairView();
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var popups = canvas.GetComponentInChildren<GamePopupCreator>(true)?.transform;
            var parent = popups != null ? popups.parent : canvas.transform;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PairPath),parent);
            instance.name = "PairRope"; FruitLabStyle.Stretch((RectTransform)instance.transform);
            foreach (Transform item in instance.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = game.GetUserText().gameObject.layer;
            if (popups != null) instance.transform.SetSiblingIndex(popups.GetSiblingIndex());
            Set(game,"pairClue",instance.GetComponent<PairClueView>());
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (close) EditorSceneManager.CloseScene(scene,true); }
        return "Saved a live-card rope prefab and Game canvas binding; removed the duplicate-card header panel";
    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using DG.Tweening;
using Game;
using Menu;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FruitLabRewardEditorTools
{
    private const string PrefabPath = "Assets/Prefabs/UI/CrystalProgressBar.prefab";
    private const string Folder = "Library/FruitLabEditor";

    public static string BakeAndValidate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before saving reward UI.");
        Directory.CreateDirectory(Folder);
        Scene game = SceneManager.GetSceneByPath("Assets/Scenes/Game.unity");
        bool closeGame = !game.isLoaded;
        if (closeGame) game = EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Additive);
        try
        {
            File.Copy("Assets/Scenes/Game.unity", Folder + "/Game.before-rewards.unity", true);
            var popup = Find<LevelFinishPopupView>(game);
            FruitLabStyle.Font = Find<GameUIView>(game).GetUserText().font;
            Canvas.ForceUpdateCanvases();
            popup.ApplyLabLayout();
            var view = (VerticalCrystalProgressView)popup.GetVerticalCrystalProgressView();
            var fill = (SlicedFilledImage)new SerializedObject(view).FindProperty("progressImage").objectReferenceValue;
            Require(fill.transform.GetSiblingIndex() > view.transform.Find("CrystalTrack").GetSiblingIndex(),
                "Pink crystal fill must render above the recessed track");
            view.ApplyLabStyle();
            if (!File.Exists(PrefabPath))
            {
                Directory.CreateDirectory("Assets/Prefabs/UI");
                PrefabUtility.SaveAsPrefabAssetAndConnect(view.gameObject, PrefabPath, InteractionMode.AutomatedAction);
            }
            Require(AssetDatabase.LoadAssetAtPath<VerticalCrystalProgressView>(PrefabPath) != null, "Shared crystal prefab must exist");
            Save(game, popup.gameObject);
        }
        finally { if (closeGame) EditorSceneManager.CloseScene(game, true); }

        Scene menu = SceneManager.GetSceneByPath("Assets/Scenes/Menu.unity");
        bool closeMenu = !menu.isLoaded;
        if (closeMenu) menu = EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity", OpenSceneMode.Additive);
        try
        {
            File.Copy("Assets/Scenes/Menu.unity", Folder + "/Menu.before-rewards.unity", true);
            var canvas = Find<MenuUIView>(menu).GetComponentInParent<Canvas>();
            Transform safe = canvas.transform.Find("SafeArea");
            RectTransform middle = (RectTransform)safe.Find("MiddleArea");
            Transform legacy = middle.Find("CircleProgressBar");
            if (legacy != null) legacy.gameObject.SetActive(false);
            var crystals = FindOrNull<VerticalCrystalProgressView>(menu);
            if (crystals == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), middle);
                crystals = instance.GetComponent<VerticalCrystalProgressView>();
            }
            var powers = Find<PowerUpListHolderView>(menu);
            foreach (PowerUpButtonView button in powers.GetComponentsInChildren<PowerUpButtonView>(true))
                if (!Enum.GetValues(typeof(RewardType)).Cast<RewardType>().Any(type => ReferenceEquals(powers.GetPowerUpButton(type), button)))
                    button.gameObject.SetActive(false);
            var layout = canvas.GetComponent<FruitLabMenuLayout>() ?? canvas.gameObject.AddComponent<FruitLabMenuLayout>();
            Set(layout, "safeArea", safe); Set(layout, "middleArea", middle);
            Set(layout, "header", Find<MenuHeaderView>(menu)); Set(layout, "menu", Find<MenuUIView>(menu));
            Set(layout, "powers", powers); Set(layout, "crystals", crystals);
            Set(Find<MenuSceneMonoInstaller>(menu), "crystalProgressView", crystals);
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(375f, 667f); scaler.matchWidthOrHeight = 0f;
            scaler.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases(); layout.ApplyLayout();
            crystals.SetStatus(true); crystals.SetAlpha(1f);
            Save(menu, canvas.gameObject);
        }
        finally { if (closeMenu) EditorSceneManager.CloseScene(menu, true); }
        ValidateRewards();
        return "Saved Game.unity, Menu.unity and shared CrystalProgressBar.prefab; reward UI checks passed";
    }

    private static void ValidateRewards()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        GameObject clone = null;
        GameObject trackerObject = null;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        try
        {
            var source = Find<LevelFinishPopupView>(scene);
            clone = Object.Instantiate(source.gameObject, source.transform.parent, false);
            var popup = clone.GetComponent<LevelFinishPopupView>();
            popup.ApplyLabLayout();
            var view = (VerticalCrystalProgressView)popup.GetVerticalCrystalProgressView();
            foreach (string field in new[] { "revealingPowerUpButton", "lifePowerUpButton", "bombPowerUpButton" })
            {
                var preview = (PowerUpButtonView)new SerializedObject(view).FindProperty(field).objectReferenceValue;
                Require(preview != null && preview.transform.IsChildOf(view.transform), "Powerup preview must be a local scene instance: " + field);
            }
            trackerObject = new GameObject("RewardPreviewTracker");
            trackerObject.SetActive(false);
            var tracker = trackerObject.AddComponent<LevelTracker>();
            foreach (int previous in new[] { 0, 4, 5 })
            foreach (int earned in new[] { 1, 2, 3 })
            foreach (RewardType type in Enum.GetValues(typeof(RewardType)))
            {
                Field(tracker, "_giftStarCount", previous); Field(tracker, "_currentRewardType", type);
                var controller = new VerticalCrystalProgressController(view, tracker, null);
                controller.Initialize(); controller.PrepareForCollection(previous, type);
                controller.AddCrystals(null, earned).Complete(true);
                int total = previous + earned;
                int capacity = ConstantValues.NUM_OF_STARS_FOR_WILD;
                Require(view.transform.Find("CrystalCount").GetComponent<TMPro.TMP_Text>().text == (total % capacity) + " / " + capacity,
                    "Animated crystal count must carry remaining crystals after overflow");
                RewardType next = (RewardType)(((int)type + total / capacity) % 3);
                var preview = view.GetRewardPreviewRectTransform(next);
                Require(preview != null && preview.gameObject.activeSelf,
                    "Upcoming reward follows the existing rotation");
            }
            view.SetStatus(true);
            view.ShowRewardPreview(RewardType.Life);
            var original = view.GetRewardPreviewRectTransform(RewardType.Revealing);
            Transform parent = original.parent;
            Vector2 size = original.sizeDelta;
            view.PresentRewardPreviewAtCenter(RewardType.Revealing, .42f).Complete(true);
            Require(view.GetRewardPreviewRectTransform(RewardType.Life).gameObject.activeSelf,
                "Presenting the completed reward must keep the upcoming reward visible in the bar");
            view.HidePresentedRewardPreview(.24f).Complete(true);
            Require(original.parent == parent && (original.sizeDelta - size).sqrMagnitude < .01f, "Claim presentation must restore the preview hierarchy and size");
            Require(!original.gameObject.activeSelf, "Completed reward returns to its original hidden state");
            foreach (float height in new[] { 580f, 667f, 812f })
            {
                var canvas = popup.GetComponentInParent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(375f, height);
                popup.ApplyLabLayout();
                Require(((RectTransform)popup.transform).rect.width > 250f, "Popup remains readable on phones");
            }
            Require(errors.Count == 0, "Unity console errors during reward preview: " + string.Join("; ", errors));
        }
        finally
        {
            Application.logMessageReceived -= collect;
            if (clone != null) Object.DestroyImmediate(clone);
            if (trackerObject != null) Object.DestroyImmediate(trackerObject);
            EditorSceneManager.ClosePreviewScene(scene);
        }
        Debug.Log("FRUIT_LAB_REWARDS_PASS: 27 collection/overflow/rotation cases; preview restoration and shared prefab references valid.");
    }

    public static string RenderMenu()
    {
        Scene source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RenderTexture target = null;
        try
        {
            GameObject clone = Object.Instantiate(Find<FruitLabMenuLayout>(source).gameObject);
            SceneManager.MoveGameObjectToScene(clone, scene);
            var canvas = clone.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)canvas.transform;
            rect.localScale = Vector3.one; rect.position = Vector3.zero; rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = new Vector2(375f, 812f);
            clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();
            clone.GetComponentInChildren<MenuHeaderView>(true).Init(240);
            var menu = clone.GetComponentInChildren<MenuUIView>(true);
            menu.GetSinglePlayerButton().SetText("Bölüm 13");
            menu.GetMultiplayerButton().SetText("Multiplayer");
            var crystals = clone.GetComponentInChildren<VerticalCrystalProgressView>(true);
            crystals.SetStatus(true); crystals.SetAlpha(1f);
            crystals.InitProgress(4f / 6f); crystals.ShowRewardPreview(RewardType.Revealing);
            var powers = clone.GetComponentInChildren<PowerUpListHolderView>(true);
            powers.SetPowerUpCount(RewardType.Bomb, 2); powers.SetPowerUpCount(RewardType.Revealing, 1); powers.SetPowerUpCount(RewardType.Life, 3);
            var cameraObject = new GameObject("MenuCapture", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            camera.orthographic = true; camera.orthographicSize = 406f; camera.aspect = 750f / 1624f;
            camera.transform.position = new Vector3(0f, 0f, -1000f);
            camera.nearClipPlane = 1f; camera.farClipPlane = 2000f;
            camera.cullingMask = 1 << 30; camera.backgroundColor = FruitLabStyle.Background;
            camera.clearFlags = CameraClearFlags.SolidColor; canvas.worldCamera = camera;
            foreach (Transform child in clone.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            target = new RenderTexture(750, 1624, 24, RenderTextureFormat.ARGB32); target.Create();
            camera.targetTexture = target; Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(750, 1624, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 750, 1624), 0, 0); image.Apply(); RenderTexture.active = previous;
            string output = Folder + "/preview-menu-crystals.png";
            File.WriteAllBytes(output, image.EncodeToPNG()); Object.DestroyImmediate(image);
            return output;
        }
        finally
        {
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            EditorSceneManager.CloseScene(scene, true); EditorSceneManager.ClosePreviewScene(source);
        }
    }

    private static T Find<T>(Scene scene) where T : Component => FindOrNull<T>(scene) ?? throw new InvalidOperationException("Missing " + typeof(T).Name);
    private static T FindOrNull<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static void Set(Object target, string name, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Save(Scene scene, GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "Scene must save");
    }
    private static void Field(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Reward UI: " + message); }
}

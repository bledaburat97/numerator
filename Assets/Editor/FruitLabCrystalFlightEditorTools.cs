using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using Game;
using Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FruitLabCrystalFlightEditorTools
{
    private const string PrefabPath = "Assets/Prefabs/Crystal.prefab";
    private const string Folder = "Library/FruitLabEditor";

    public static void PreparePrefab()
    {
        string backup = Folder + "/PrefabBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        File.Copy(PrefabPath, backup + "/Crystal.prefab");
        File.Copy(PrefabPath + ".meta", backup + "/Crystal.prefab.meta");
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var token = root.GetComponent<CrystalView>();
            token.ApplyLabStyle();
            Transform trailObject = root.transform.Find("FlightTrail");
            if (trailObject == null)
            {
                trailObject = new GameObject("FlightTrail", typeof(RectTransform), typeof(CanvasRenderer), typeof(CrystalFlightTrail)).transform;
                trailObject.SetParent(root.transform, false);
            }
            trailObject.SetAsFirstSibling();
            var trail = trailObject.GetComponent<CrystalFlightTrail>();
            FruitLabStyle.Stretch(trail.rectTransform);
            trail.color = new Color(1f, .82f, .94f, 1f);
            trail.raycastTarget = false;
            var trailSettings = new SerializedObject(trail);
            trailSettings.FindProperty("lifetime").floatValue = .28f;
            trailSettings.FindProperty("trailWidth").floatValue = 2.2f;
            trailSettings.FindProperty("sparkleSize").floatValue = 3.4f;
            trailSettings.FindProperty("maxLength").floatValue = 76f;
            trailSettings.FindProperty("sampleSpacing").floatValue = 4f;
            trailSettings.FindProperty("sparkleEverySamples").intValue = 2;
            trailSettings.ApplyModifiedPropertiesWithoutUndo();
            var serialized = new SerializedObject(token);
            serialized.FindProperty("flightTrail").objectReferenceValue = trail;
            // Crystals now use the single Flight path; the shared old preset still serves coins.
            serialized.FindProperty("curvedAnimationPreset").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            var group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<CrystalView>();
        Require(saved.FlightTrail != null && !saved.FlightTrail.IsEmitting, "Idle prefab must contain an assigned, quiet trail");
    }

    public static void Review(Canvas canvas, Camera camera, bool collection, int boardCount)
    {
        var source = canvas.GetComponentsInChildren<RewardTokenView>(true).First();
        var target = collection
            ? canvas.GetComponentInChildren<VerticalCrystalProgressView>(true).GetCrystalIconRectTransform()
            : canvas.GetComponentInChildren<InitialCardAreaView>(true).GetComponentsInChildren<NormalCardItemView>(true).Last().GetRectTransform();
        GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas.transform, false);
        root.name = "CrystalFlightReview";
        var token = root.GetComponent<CrystalView>();
        RectTransform item = token.GetRectTransform();
        item.position = source.GetRectTransform().position;
        item.localScale = Vector3.one;
        token.SetStatus(true);
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        var trail = token.FlightTrail;
        Require(trail != null && !trail.raycastTarget && !trail.IsEmitting && trail.ActiveSamples == 0, "Trail is serialized, non-interactive and idle before flight");
        int children = root.GetComponentsInChildren<Transform>(true).Length;
        bool sourceActive = source.gameObject.activeSelf;
        source.gameObject.SetActive(false);
        Tween animation = null;
        try
        {
            Vector3 start = canvas.transform.InverseTransformPoint(item.position);
            Vector3 end = canvas.transform.InverseTransformPoint(target.position);
            Vector3 direction = end - start;
            animation = CrystalFlightAnimation.Create(token, target, collection).SetUpdate(UpdateType.Manual).Pause();
            animation.Play();
            float step = token.GetFlightDuration(collection) / 48f;
            float previous = -1f, maxBend = 0f;
            int peakSamples = 0;
            string name = collection ? "collection" : "hint";
            for (int i = 0; i <= 48; i++)
            {
                if (i > 0) DOTween.ManualUpdate(step, step);
                if (i == 48 && animation.IsActive()) animation.Complete(true);
                trail.Advance(i == 0 ? 0f : step);
                Vector3 current = canvas.transform.InverseTransformPoint(item.position);
                float progress = Vector3.Dot(current - start, direction) / direction.sqrMagnitude;
                Require(progress >= previous - .0001f && progress >= -.0001f && progress <= 1.0001f,
                    "Flight must advance toward the target without overshoot or reversal");
                maxBend = Mathf.Max(maxBend, Vector3.Cross(current - start, direction.normalized).magnitude);
                previous = progress;
                peakSamples = Mathf.Max(peakSamples, trail.ActiveSamples);
                if (boardCount == 5 && i % 4 == 0)
                    Capture(camera, Folder + "/crystal-flight-" + name + "-" + (i / 4).ToString("D2") + ".png");
            }
            Require(Vector3.Distance(item.position, target.position) < .05f, "Flight must arrive at the target");
            Require(maxBend > 1f && maxBend <= token.MaxArcOffset + .01f, "Flight has a short, bounded arc in canvas units");
            Require(peakSamples > 12 && peakSamples <= CrystalFlightTrail.MaxSamples && !trail.IsEmitting, "Flight emits a denser bounded trail and stops on arrival");
            trail.Advance(.6f);
            Require(trail.ActiveSamples == 0, "All particles fade after arrival");
            Require(root.GetComponentsInChildren<Transform>(true).Length == children, "Flying cannot create particle GameObjects");
            if (!collection) ValidateReturningCard(canvas, camera);
            ValidateCancellation(canvas);
            Debug.Log("FRUIT_LAB_CRYSTAL_FLIGHT_PASS: " + name + "; board=" + boardCount
                + "; monotonic arc, exact arrival, short non-raycast trail, bounded samples, fade and owner cancellation.");
        }
        finally
        {
            if (animation != null && animation.IsActive()) animation.Kill();
            if (root != null) Object.DestroyImmediate(root);
            source.gameObject.SetActive(sourceActive);
            Draw(camera); // Restore the ordinary preview after the separate flight frames.
        }
    }

    private static void ValidateReturningCard(Canvas canvas, Camera camera)
    {
        var original = canvas.GetComponentInChildren<InitialCardAreaView>(true)
            .GetComponentsInChildren<NormalCardItemView>(true).Last();
        var home = original.GetParent();
        foreach (bool reveal in new[] { false, true })
        {
            var card = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardItem.prefab"), canvas.transform, false)
                .GetComponent<NormalCardItemView>();
            var handler = new CardViewHandler(card, camera, null, new CardMoveHandler(null, 0),
                new CardItemData(home, (RectTransform)canvas.transform, 0, 1, (ProbabilityType)0, false, original.GetRectTransform().rect.size));
            var token = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas.transform, false).GetComponent<CrystalView>();
            var target = card.GetRectTransform();
            if (reveal) target.pivot = new Vector2(.2f, .8f);
            target.position = canvas.GetComponentInChildren<BoardHolderView>(true).GetRectTransform().position;
            token.GetRectTransform().position = canvas.GetComponentInChildren<LifeBarView>(true).GetRectTransform().position;
            Sequence animation = null;
            Sequence move = null;
            try
            {
                int hits = 0;
                var manager = new RewardTokenAnimationManager();
                animation = reveal ? manager.RevealCard(token, target, () => hits++) : manager.DestroyCard(token, target, () => hits++);
                animation.SetUpdate(UpdateType.Manual).Play();
                DOTween.ManualUpdate(.04f, .04f);
                handler.MoveToInitialParent(); // The real wrong-guess reset starts after the hint.
                move = (Sequence)typeof(CardViewHandler).GetField("_activeMoveSequence", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(handler);
                move.SetUpdate(UpdateType.Manual).Play();
                for (int i = 0; i < 48; i++) DOTween.ManualUpdate(1f / 60f, 1f / 60f);
                Require(target.parent == home && Vector3.Distance(target.position, home.position) < .05f, "Real card reset must finish at its initial holder");
                float miss = Vector3.Distance(token.GetRectTransform().TransformPoint(token.GetRectTransform().rect.center), target.TransformPoint(target.rect.center));
                Require(miss < .05f, "Hint must reach the returning card center before impact; reveal=" + reveal + "; miss=" + miss);
                Require(hits == 0 && animation.IsActive(), "Alignment must be checked during the visible impact, before fade/removal");
            }
            finally
            {
                if (animation != null && animation.IsActive()) animation.Kill();
                if (move != null && move.IsActive()) move.Kill();
                Object.DestroyImmediate(token.gameObject);
                Object.DestroyImmediate(card.gameObject);
            }
        }
        Debug.Log("FRUIT_LAB_MOVING_HINT_PASS: real card reset during both hint sequences; crystal centered at impact.");
    }

    private static void ValidateCancellation(Canvas canvas)
    {
        foreach (bool destroyTarget in new[] { false, true })
        {
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), canvas.transform, false);
            root.SetActive(true);
            var token = root.GetComponent<CrystalView>();
            var target = new GameObject("FlightCancellationTarget", typeof(RectTransform)).GetComponent<RectTransform>();
            target.SetParent(canvas.transform, false);
            target.anchoredPosition = new Vector2(80f, -100f);
            int hits = 0;
            Sequence sequence = new RewardTokenAnimationManager().RevealCard(token, target, () => hits++);
            sequence.SetUpdate(UpdateType.Manual).Play();
            DOTween.ManualUpdate(.1f, .1f);
            token.FlightTrail.Advance(.1f);
            Require(token.FlightTrail.IsEmitting, "Cancellation probe must actually be flying");
            if (destroyTarget) Object.DestroyImmediate(target.gameObject); else Object.DestroyImmediate(root);
            DOTween.ManualUpdate(2f, 2f);
            Require(!sequence.IsActive() && hits == 0, "Removing the crystal or target cancels the trail and pending hint");
            if (target != null) Object.DestroyImmediate(target.gameObject);
        }
    }

    private static void Capture(Camera camera, string path)
    {
        Draw(camera);
        RenderTexture previous = RenderTexture.active;
        var image = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = camera.targetTexture;
            image.ReadPixels(new Rect(0f, 0f, image.width, image.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; Object.DestroyImmediate(image); }
    }

    private static void Draw(Camera camera)
    {
        Canvas.ForceUpdateCanvases();
        if (GraphicsSettings.currentRenderPipeline != null)
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = camera.targetTexture });
        else camera.Render();
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Crystal flight: " + message); }
}

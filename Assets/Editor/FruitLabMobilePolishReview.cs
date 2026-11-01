using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Reflection;
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
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewLevelIntros(Canvas canvas, Camera camera)
    {
        var update = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        string profile = Profile(); var copies = new List<object>(); int layouts = 0;
        try
        {
            foreach (var mechanic in new[] { LevelMechanic.Helper,LevelMechanic.Pair,LevelMechanic.Split,LevelMechanic.Growing,LevelMechanic.Dynamite,LevelMechanic.Normal })
            {
                string key = mechanic == LevelMechanic.Helper ? TutorialProgress.HelperLevel
                    : mechanic == LevelMechanic.Pair ? TutorialProgress.PairLevel
                    : mechanic == LevelMechanic.Dynamite ? TutorialProgress.DynamiteLevel
                    : mechanic == LevelMechanic.Growing ? TutorialProgress.GrowingLevel
                    : mechanic == LevelMechanic.Split ? TutorialProgress.SplitLevel : TutorialProgress.CrystalHints;
                var flags = TutorialProgress.Keys.ToDictionary(name => name,name => 1); flags[key] = 0;
                using (TutorialProgress.UseTestValues(flags))
                {
                    ClearPreviewActors(canvas);
                    using (var game = new Fixture(canvas,camera,new LevelData { NumOfBoardHolders = mechanic == LevelMechanic.Growing ? 2 : 4,NumOfCards = 8,MaxNumOfTries = 10,Mechanic = mechanic }))
                    {
                        Field(game.Tracker,"_levelId",20); game.BuildActors();
                        Field(game.Bootstrap,"_levels",new MechanicLevel { Mechanic = mechanic });
                        if (mechanic == LevelMechanic.Pair)
                        {
                            var pair = new PairEffect(game.Round,game.Target,game._saves,game._info,game.Tracker);
                            Require(pair.TryApply(),"Pair intro has a real adjacent pair"); game.View.SetPairClue(pair.Current);
                        }
                        game.Bootstrap.PrepareRound(); game.Bootstrap.StartRound();
                        var message = game.Message;
                        Require(message != null && game.Gate.IsRestricted && !TutorialProgress.Seen(key),"Intro waits for acknowledgement");
                        var data = new SerializedObject(message);
                        var title = (TMP_Text)data.FindProperty("title").objectReferenceValue;
                        var body = (TMP_Text)data.FindProperty("text").objectReferenceValue;
                        var button = (BaseButtonView)data.FindProperty("nextButton").objectReferenceValue;
                        string heading = title.text, copy = body.text;
                        copies.Add(new { mechanic = mechanic.ToString(),heading,body = copy,completion_key = key });
                        foreach (float width in new[] { 320f,375f,430f })
                        foreach (float height in new[] { 568f,667f,812f })
                        {
                            var root = (RectTransform)canvas.transform; root.sizeDelta = new Vector2(width,height);
                            canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); Canvas.ForceUpdateCanvases(); message.ApplyLayout();
                            var rect = (RectTransform)message.transform;
                            Vector3 expected = root.TransformPoint(root.rect.center);
                            Require(Vector3.Distance(rect.TransformPoint(rect.rect.center),expected) < .1f,"Intro is centered on the actual game canvas");
                            Require(rect.rect.width <= width-23f && rect.rect.height <= height-24f,"Intro fits a small phone");
                            Require(message.GetComponent<Image>().color.grayscale > .8f && body.color.grayscale < .3f,
                                "Intro uses a bright panel with dark readable text");
                            Require(body.fontSize >= 19f && body.rectTransform.rect.height+.1f >= body.GetPreferredValues(copy,body.rectTransform.rect.width,0).y,
                                "Full intro copy fits without shrinking its font");
                            foreach (RectTransform child in new[] { title.rectTransform,body.rectTransform,button.GetButtonRectTransform() })
                            {
                                var corners = new Vector3[4]; child.GetWorldCorners(corners);
                                Require(corners.All(corner => rect.rect.Contains(rect.InverseTransformPoint(corner))),"Intro content stays inside its panel");
                            }
                            layouts++;
                        }
                        ((RectTransform)canvas.transform).sizeDelta = new Vector2(375,812);
                        canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); Canvas.ForceUpdateCanvases(); message.ApplyLayout();
                        // Reusing the view for a guided step must restore its original placement and colors.
                        message.Show(heading,copy,true);
                        Require(((RectTransform)message.transform).anchoredPosition.y == -90f
                            && message.GetComponent<Image>().color == FruitLabStyle.Panel,"Guided steps retain their top placement and dark style");
                        message.ShowAt(heading,copy,true,TutorialMessagePlacement.Center);
                        Layer(canvas.transform); Draw(camera);
                        Capture(camera,Folder+"/preview-level-intro-"+mechanic.ToString().ToLowerInvariant()+".png");
                        game.Next(); Require(TutorialProgress.Seen(key) && game.Message == null && !game.Gate.IsRestricted,"Tamam restores input and acknowledges only this intro");
                        game.Bootstrap.StartRound(); Require(game.Message == null,"Acknowledged intro does not repeat every round");
                        flags[key] = 0; game.Bootstrap.StartRound(); Require(game.Message != null,"An unacknowledged intro can start again");
                        game.Bootstrap.Cancel(); Require(!TutorialProgress.Seen(key),"Leaving before Tamam never acknowledges the intro");
                        game.View.SetPairClue(null);
                    }
                }
            }
            Require(Profile() == profile,"Intro checks preserve real player data");
            File.WriteAllText(Folder+"/level-intro-review.json",JsonConvert.SerializeObject(new {
                passed = true,layouts,copies,
                checks = new[] { "actual Bootstrapper messages", "canvas-centered placement", "bright panel and dark text", "54 mobile layouts", "full body copy", "Tamam restores input", "first encounter only", "cancel does not mark seen", "guided top style preserved", "unchanged PlayerPrefs" }
            },Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = update; }
    }

    public static void ReviewMobilePolish(Canvas canvas, Camera camera)
    {
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        var errors = new List<string>();
        Application.LogCallback collect = (message, stack, type) => {
            if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message);
        };
        Application.logMessageReceived += collect;
        try
        {
            Require(PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait
                && !PlayerSettings.allowedAutorotateToLandscapeLeft && !PlayerSettings.allowedAutorotateToLandscapeRight
                && !PlayerSettings.allowedAutorotateToPortraitUpsideDown, "Portrait-only native player settings");
            ValidateMechanicBindings();
            var a = new PairClue(4,1); var b = new PairClue(1,4);
            Require(a.First == b.First && a.Second == b.Second && a.Matches(new[] {2,4,1,3})
                && a.Matches(new[] {2,1,4,3}) && !a.Matches(new[] {1,3,4,2}), "Pair data reveals adjacency without direction");
            var simulation = new PairRopeSimulation();
            Vector2 anchorA = new Vector2(0,100), anchorB = new Vector2(180,100);
            simulation.Reset(anchorA,anchorB);
            for (int i = 0; i < 240; i++) simulation.Advance(anchorA,anchorB,1f/120f);
            Require(simulation.Points[12].y < 85f && simulation.Points[0] == anchorA && simulation.Points[23] == anchorB, "Rope sags under gravity with pinned endpoints");
            anchorB = new Vector2(330,170);
            for (int i = 0; i < 60; i++) simulation.Advance(anchorA,anchorB,1f/60f);
            Require(simulation.Tension > .9f && simulation.Points.All(p => !float.IsNaN(p.x) && !float.IsNaN(p.y)), "Stretch remains stable and visibly taut");

            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,new LevelData { NumOfBoardHolders = 5, NumOfCards = 9, MaxNumOfTries = 10 }))
            {
                Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear();
                game.Target.RevealMultiplayerAnswer(new[] {4,1,3,5,2});
                var view = game.View.GetPairView();
                view.Show(a); view.RefreshRope(1f/60f);
                AssertBottomAnchors(view,game);
                Require(view.GetComponentsInChildren<NonDraggableCardItemView>(true).Length == 0
                    && view.GetComponentsInChildren<TMP_Text>(true).Length == 0, "No extra cards, order caption or header panel");
                for (int i = 0; i < 90; i++) view.RefreshRope(1f/60f);
                Vector2 before = view.Simulation.Points[0];
                Require(game.Placement.TryPlaceCardOnBoard(0,0), "A linked fruit moves independently"); Tick(.4f);
                view.RefreshRope(1f/60f);
                Require(Vector2.Distance(before,view.Simulation.Points[0]) > 15f, "Rope follows the actual reparented card");
                AssertBottomAnchors(view,game);
                var visual = game.Initial.GetRectTransformOfCardItem(0).Find("AnimationProvider");
                var rotation = visual.localRotation;
                visual.localRotation = Quaternion.Euler(0,0,18f); view.RefreshRope(1f/60f);
                AssertBottomAnchors(view,game);
                visual.localRotation = rotation; view.RefreshRope(1f/60f);
                Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-pair-rope-one-board.png");
                var query = (IBoardPlacementQuery)Get(game.Placement,"_boardPlacementQuery");
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                bool ropeHit = false;
                for (int point = 3; point < PairRopeSimulation.NodeCount-3 && !ropeHit; point++)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera,view.transform.TransformPoint(view.Simulation.Points[point]));
                    var hits = new List<RaycastResult>();
                    raycaster.Raycast(new PointerEventData(EventSystem.current) { position = screen },hits);
                    ropeHit = hits.Count > 0 && hits[0].gameObject == view.Graphic.gameObject;
                }
                Require(ropeHit,"Actual canvas raycast can grab the visible cord");
                game.AssertHit(game.Initial.GetRectTransformOfCardItem(2),typeof(NormalCardItemView));
                var pointer = new PointerEventData(EventSystem.current) { pointerId = 9,
                    pointerPressRaycast = new RaycastResult { module = raycaster },
                    position = RectTransformUtility.WorldToScreenPoint(camera,view.transform.TransformPoint(view.Simulation.Points[12])) };
                view.BeginGroupDrag(pointer); Require(view.IsGroupDragging, "Rope can pick up both movable cards");
                pointer.position = RectTransformUtility.WorldToScreenPoint(camera,
                    (game.Board.GetRectTransformOfGarden(0).position+game.Board.GetRectTransformOfGarden(1).position)*.5f);
                view.DragGroup(pointer); view.EndGroupDrag(pointer); Tick(.4f); view.RefreshRope(1f/60f);
                Require(query.GetPlacedCardIndexesOnBoard().Count(index => index == 0 || index == 3) == 2
                    && !view.IsGroupDragging, "Both fruits drop together into adjacent board slots");
                for (int frame = 0; frame < 90; frame++) view.RefreshRope(1f/60f);
                AssertBottomAnchors(view,game);
                Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-pair-rope-both-board.png");
                foreach (var key in new[] { "checkButton", "resetButton", "settingsButton" })
                {
                    var control = (BaseButtonView)new SerializedObject(game.View).FindProperty(key).objectReferenceValue;
                    game.AssertHit(control.GetButtonRectTransform(),typeof(BaseButtonView));
                }
                game.UI.TriggerResetNumbers(); Tick(.4f);
                Require(game.Placement.TryPlacePairOnBoard(3,0,1) && query.GetPlacedCardIndexesOnBoard()[1] == 3
                    && query.GetPlacedCardIndexesOnBoard()[2] == 0, "Player can place the pair in the opposite order"); Tick(.4f);
                game.UI.TriggerResetNumbers(); Tick(.4f); game.Placement.TryPlaceCardOnBoard(1,1); Tick(.4f);
                Require(!game.Placement.TryPlacePairOnBoard(0,3,0) && query.GetPlacedCardIndexesOnBoard()[1] == 1
                    && query.GetPlacedCardIndexesOnBoard().Count(index => index == 0 || index == 3) == 0,
                    "Group placement cannot overwrite another card or partially commit");
                game.Placement.TryRevealAndLockCard(0,3); Tick(.3f);
                Require(!view.CanDragGroup && game.Placement.TryPlaceCardOnBoard(0,2), "Fixed endpoint stays locked while its partner can move"); Tick(.4f);
                view.RefreshRope(1f/60f); Require(view.gameObject.activeSelf, "Rope persists with one fixed card");
                view.Show(null);
            }

            TestRevealSelection(canvas,camera);

            ClearPreviewActors(canvas);
            using (var game = new Fixture(canvas,camera,new LevelData { NumOfBoardHolders = 3, NumOfCards = 7, MaxNumOfTries = 8 }))
            {
                Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear();
                game.Target.RevealMultiplayerAnswer(new[] {4,1,3});
                for (int n = 0; n < 3; n++) { for (int fruit = 0; fruit < 3; fruit++) game.Place(fruit); game.Check(); Tick(.5f); }
                game._powers.Refresh();
                Click((BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetLifePowerUpButton());
                Require(game.Round.GetRemainingGuessCount() == 8, "Life award remains immediate model state");
                Tick(.13f);
                var life = canvas.GetComponentInChildren<LifeBarView>(true);
                var gain = life.transform.Find("LifeGain").GetComponent<TMP_Text>();
                var glow = life.transform.Find("LifeGainGlow").GetComponent<Image>();
                Require(gain.text == "+3" && gain.alpha > .7f && glow.color.a > .4f, "Life gain has an explicit +3 and bright bar pulse");
                Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-life-gain.png");
                Tick(.7f);
                Require(gain.alpha < .01f && life.transform.Find("GuessesRemaining").GetComponent<TMP_Text>().alpha > .99f
                    && Vector3.Distance(life.transform.localScale,Vector3.one) < .001f, "Life feedback restores normal appearance");
            }

            var popup = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MessagePath),canvas.transform,false);
            try
            {
                var message = popup.GetComponent<TutorialMessagePopupView>(); message.Init();
                message.Show("Kristal İpuçları","Kazanamadığın kristaller oyun içinde sana yardım eder.",true);
                var texts = popup.GetComponentsInChildren<TMP_Text>(true);
                Require(popup.GetComponentsInChildren<BaseButtonView>(true).Length == 1 && texts.Any(text => text.text == "Tamam"), "Tutorial has exactly one Tamam button");
                Require(texts.Single(text => text.text.StartsWith("Kazanamadığın")).fontSize >= 19f, "Tutorial body font is larger");
                Layer(popup.transform); Draw(camera); Capture(camera,Folder+"/preview-short-tutorial.png");
            }
            finally { Object.DestroyImmediate(popup); }

            TestStaggeredCrystals(canvas);
            var bar = canvas.GetComponentInChildren<VerticalCrystalProgressView>(true); bar.ApplyLabStyle();
            Require(bar.transform.Find("CrystalTitle").GetComponent<TMP_Text>().text == "Güçlendirici Barı", "Shared powerup bar heading");
            var subtitle = bar.transform.Find("NextReward");
            foreach (var reward in PowerUpTypeUtility.OrderedRewardTypes)
            {
                bar.ShowRewardPreview(reward);
                Require(subtitle == null || !subtitle.gameObject.activeSelf,"Reward rotation never restores the old subtitle");
            }
            Require(errors.Count == 0,"No Unity/tween errors: "+string.Join("; ",errors));
            File.WriteAllText(Folder+"/mobile-polish-review.json",JsonConvert.SerializeObject(new {
                passed = true, scope = "single-player polish", crystal_launch_gap = .16f,
                checks = new[] { "portrait settings", "unordered pair", "gravity/stretch stability", "live card endpoints", "fixed bottom-center attachments including rotation", "single-card movement", "real cord/card raycasts", "two-card drag/drop", "reverse placement", "occupied-slot protection", "fixed-card protection", "life +3 glow/pulse", "single-button tutorial", "19pt body text", "overlapping crystal flights", "overflow reward preservation", "powerup bar heading without subtitle", "reveal selection pulse and real board raycast", "reveal consumption and cancellation cleanup" }
            },Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = previous; Application.logMessageReceived -= collect; }
    }

    private static void AssertBottomAnchors(PairClueView view, Fixture game)
    {
        int[] fruits = { view.Clue.First-1,view.Clue.Second-1 };
        for (int index = 0; index < fruits.Length; index++)
        {
            var card = game.Initial.GetRectTransformOfCardItem(fruits[index]);
            var visual = card.Find("AnimationProvider") as RectTransform ?? card;
            Vector2 expected = view.transform.InverseTransformPoint(visual.TransformPoint(new Vector3(visual.rect.center.x,visual.rect.yMin,0f)));
            Require(Vector2.Distance(expected,view.Simulation.Points[index == 0 ? 0 : PairRopeSimulation.NodeCount-1]) < .01f,
                "Rope attachment stays at the card's local bottom center");
        }
    }

    private static void TestRevealSelection(Canvas canvas, Camera camera)
    {
        ClearPreviewActors(canvas);
        using (var game = new Fixture(canvas,camera,new LevelData { NumOfBoardHolders = 3, NumOfCards = 7, MaxNumOfTries = 8 }))
        {
            Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Gate.Clear();
            game.Target.RevealMultiplayerAnswer(new[] {4,1,3});
            game.Placement.TryPlaceCardOnBoard(1,0); Tick(.4f);
            var holders = Enumerable.Range(0,3).Select(i => game.Board.GetRectTransformOfGarden(i).GetComponentInParent<BoardHolderView>()).ToArray();
            var surfaces = holders.Select(holder => holder.transform.Find("Image").GetComponent<Image>()).ToArray();
            var original = surfaces.Select(surface => surface.color).ToArray();
            var highlights = holders.Select(holder => (Image)new SerializedObject(holder).FindProperty("highlightImage").objectReferenceValue).ToArray();
            var button = (BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetRevealingPowerUpButton();
            game._powers.Refresh(); Click(button);
            Require(game.Reveal.IsActive && !highlights[0].gameObject.activeSelf
                && highlights[1].gameObject.activeSelf && highlights[2].gameObject.activeSelf,"Only empty reveal targets are highlighted");
            Require(surfaces[1].color.g > original[1].g+.1f && highlights[1].color.a > .9f,"Reveal slots have a brighter surface and rim");
            Tick(.22f);
            Require(highlights[1].color.a < .95f,"Reveal selection visibly pulses");
            Layer(canvas.transform); Draw(camera);
            var targetButton = (Button)new SerializedObject(holders[1]).FindProperty("button").objectReferenceValue;
            var hits = new List<RaycastResult>();
            holders[1].GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(camera,holders[1].GetRectTransform().position)
            },hits);
            Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == targetButton,
                "Reveal rim passes touches to the raised slot's button; hits="+hits.Count+"; top="+hits.FirstOrDefault().gameObject?.name);
            Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-reveal-selection.png");
            targetButton.onClick.Invoke(); Tick(.4f);
            Require(!game.Reveal.IsActive && game.Tracker.GetPowerUpCount(RewardType.Revealing) == 0,"A real board selection consumes exactly one reveal");
            for (int i = 0; i < holders.Length; i++)
                Require(!highlights[i].gameObject.activeSelf && surfaces[i].color == original[i]
                    && !holders[i].GetComponent<Canvas>().overrideSorting,"Reveal completion restores every slot; slot="+i
                    +"; highlight="+highlights[i].gameObject.activeSelf+"; color="+surfaces[i].color.ToString("F4")
                    +"; original="+original[i].ToString("F4")+"; sorting="+holders[i].GetComponent<Canvas>().overrideSorting);
            Field(game.Tracker,"_revealingPowerUpCount",1); game._powers.Refresh(); Click(button); Tick(.15f);
            Require(game.Reveal.IsActive && highlights[2].gameObject.activeSelf,"Reveal can start again after completing a selection");
            Click(button); Tick(.7f);
            Require(!game.Reveal.IsActive && !highlights[2].gameObject.activeSelf && surfaces[2].color == original[2]
                && game.Tracker.GetPowerUpCount(RewardType.Revealing) == 1,"Cancelling stops the pulse without consuming inventory");
        }
    }

    private static void TestStaggeredCrystals(Canvas canvas)
    {
        var root = new GameObject("CollectionTracker"); root.SetActive(false);
        var tokens = new List<CollectionToken>();
        try
        {
            var tracker = root.AddComponent<LevelTracker>(); tracker.SuppressPersistenceForTests = true; Field(tracker,"_levelId",20);
            var view = canvas.GetComponentInChildren<VerticalCrystalProgressView>(true); view.ApplyLabStyle();
            view.SetStatus(true); view.SetAlpha(1);
            var controller = new VerticalCrystalProgressController(view,tracker,null);
            controller.PrepareForCollection(ConstantValues.NUM_OF_STARS_FOR_WILD-1,RewardType.Revealing);
            var destination = view.GetCrystalIconRectTransform();
            for (int i = 0; i < 3; i++) tokens.Add(new CollectionToken(canvas.transform));
            var sequence = controller.AddCrystals(tokens.Cast<IRewardTokenView>().ToArray(),3); sequence.Play();
            var started = new float[3];
            for (int frame = 0; frame < 180; frame++)
            {
                Tick(.01f);
                for (int i = 0; i < 3; i++) if (started[i] == 0 && tokens[i].Rect.parent == destination) started[i] = (frame+1)*.01f;
            }
            Require(started[0] > 0 && Mathf.Abs(started[1]-started[0]-.16f)<.025f && Mathf.Abs(started[2]-started[1]-.16f)<.025f,
                "Crystal flights overlap at 0.16-second launch intervals");
            Require(tokens.All(token => token.Destroyed),"Every staggered crystal finishes cleanup");
            Require(view.transform.Find("CrystalCount").GetComponent<TMP_Text>().text == "2 / "+ConstantValues.NUM_OF_STARS_FOR_WILD,
                "Overflow resets once and keeps both remaining crystals");
            sequence.Kill();
        }
        finally { foreach (var token in tokens) Object.DestroyImmediate(token.Rect.gameObject); Object.DestroyImmediate(root); }
    }

    private sealed class CollectionToken : IRewardTokenView
    {
        public readonly RectTransform Rect;
        public bool Destroyed;
        public CollectionToken(Transform parent) { Rect = new GameObject("CollectionToken",typeof(RectTransform)).GetComponent<RectTransform>(); Rect.SetParent(parent,false); Rect.sizeDelta = Vector2.one*16f; }
        public void SetLocalPosition(Vector2 p) => Rect.localPosition = p;
        public void SetLocalScale(Vector3 p) => Rect.localScale = p;
        public void SetStatus(bool value) { }
        public void SetSize(Vector2 size) => Rect.sizeDelta = size;
        public RectTransform GetRectTransform() => Rect;
        public void SetParent(RectTransform parent) => Rect.SetParent(parent);
        public void Destroy() => Destroyed = true;
        public CurvedAnimationPreset GetCurvedAnimationPreset() => null;
        public Sequence AnimateFadeOut(float duration) => DOTween.Sequence().AppendInterval(duration);
        public Tween AnimatePunchScale(Vector3 punch,float duration,int vibrato,float elasticity) => Rect.DOPunchScale(punch,duration,vibrato,elasticity);
    }

    public static void RefreshPlaytestSettings()
    {
        var cached = Resources.Load<TextAsset>("LevelPlaytestConfig");
        if (cached != null) Resources.UnloadAsset(cached);
        AssetDatabase.ImportAsset("Assets/Resources/LevelPlaytestConfig.json",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        typeof(LevelPlaytestSchedule).GetField("_settings",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
    }

    public static string BakePowerupPolish()
    {
        const string path = "Assets/Prefabs/UI/CrystalProgressBar.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { prefab.GetComponent<VerticalCrystalProgressView>().ApplyLabStyle(); PrefabUtility.SaveAsPrefabAsset(prefab,path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        foreach (string scenePath in new[] { "Assets/Scenes/Game.unity","Assets/Scenes/Menu.unity" })
        {
            var scene = SceneManager.GetSceneByPath(scenePath); bool close = !scene.isLoaded;
            if (close) scene = EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var layout in root.GetComponentsInChildren<FruitLabGameLayout>(true)) layout.ApplyLayout();
                    foreach (var layout in root.GetComponentsInChildren<FruitLabMenuLayout>(true)) layout.ApplyLayout();
                    foreach (var powers in root.GetComponentsInChildren<PowerUpListHolderView>(true))
                    {
                        powers.ApplyLabLayout(powers.GetRectTransform().rect.width,powers.GetRectTransform().rect.height);
                        RecordTree(powers);
                    }
                    foreach (var bar in root.GetComponentsInChildren<VerticalCrystalProgressView>(true)) { bar.ApplyLabStyle(); RecordTree(bar); }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (close) EditorSceneManager.CloseScene(scene,true); }
        }
        return "Saved six powerup buttons and Split reward preview in Game, Menu and the shared prefab";
    }
    private static void RecordTree(Component root)
    {
        foreach (var item in root.GetComponentsInChildren<Component>(true))
        {
            EditorUtility.SetDirty(item);
            if (PrefabUtility.IsPartOfPrefabInstance(item)) PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }
    }

    public static void ReviewPowerupPolish(Canvas canvas,Camera camera)
    {
        var previous = DOTween.defaultUpdateType; DOTween.defaultUpdateType = UpdateType.Manual;
        string profile = Profile(); var errors = new List<string>();
        Application.LogCallback collect = (message,stack,type) => { if (type == LogType.Error || type == LogType.Exception || message.Contains("DOTWEEN ►")) errors.Add(message); };
        Application.logMessageReceived += collect;
        try
        {
            ValidateMechanicBindings();
            Require(PowerUpTypeUtility.Name(RewardType.Pair) == "Bağlı" && PowerUpTypeUtility.Name(RewardType.Split) == "Bölme","Powerup names");
            Require((int)RewardType.Pair == 4 && (int)RewardType.Split == 5,"Existing reward IDs stay unchanged");
            int ropeFrames = CheckHangingRope(canvas);
            CheckVerticalLinkedCards(canvas,camera);
            int layouts = 0;
            foreach (int count in new[] { 4,5 })
            foreach (int oldGuesses in new[] { 0,2 })
            {
                ClearPreviewActors(canvas);
                var definition = new LevelData { NumOfBoardHolders = count,NumOfCards = count+4,MaxNumOfTries = 10 };
                LevelSaveData checkpoint = null;
                using (var game = new Fixture(canvas,camera,definition))
                {
                    Field(game.Tracker,"_levelId",20); Field(game.Tracker,"_splitPowerUpCount",2);
                    game.BuildActors(); game.Gate.Clear(); var answer = Enumerable.Range(1,count).ToArray(); game.Target.RevealMultiplayerAnswer(answer);
                    var split = (BoardSplitService)Get(game.Results,"_split");
                    var helper = new HelperEffect(game.Round,game.Target,game._saves); Field(helper,"_split",split);
                    var provider = new GameSaveSnapshotProvider(game.Target,game._info,new PlayingFlow(),game._counts,game.Round);
                    Field(provider,"_saveData",game._saves); Field(provider,"_levels",new TestLevel(definition)); Field(provider,"_tracker",game.Tracker);
                    using (var rules = new LevelMechanicsController(game.Results,helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker,null,split,game.Board))
                    {
                        rules.Attach(game.View,() => provider.TryCreateSnapshot(out checkpoint)); rules.Begin(LevelMechanic.Normal);
                        if (oldGuesses > 0)
                        {
                            SubmitSplit(game,new[] {3,4,1,2,5}.Take(count).ToArray()); SubmitSplit(game,answer.Reverse().ToArray());
                            Require(helper.TryApply(),"An existing detail hint can precede Split");
                        }
                        int remaining = game.Round.GetRemainingGuessCount(); game._powers.Refresh();
                        var button = (BaseButtonView)canvas.GetComponentInChildren<PowerUpListHolderView>(true).GetPowerUpButton(RewardType.Split);
                        Click(button);
                        Require(split.IsActive && game._saves.GetLevelSaveData().BoardSplitApplied && game.Tracker.GetPowerUpCount(RewardType.Split) == 1,"A real Split button consumes once and activates");
                        Require(game.Round.GetRemainingGuessCount() == remaining && game.Round.GetTriedCardsList().Count == oldGuesses,"Split preserves lives and raw history");
                        Require(checkpoint != null && checkpoint.BoardSplitApplied,"Activation checkpoints even before the first guess");
                        var area = canvas.GetComponentInChildren<ResultAreaView>(true);
                        Require(area.GetComponentsInChildren<ResultBlockView>().Length == oldGuesses
                            && area.GetComponentsInChildren<ResultBlockView>().All(row => row.IsSplit),"Earlier rows are rebuilt with separate counters");
                        Click(button); Require(game.Tracker.GetPowerUpCount(RewardType.Split) == 1 && !split.CanApply,"A repeated press cannot consume stock");
                        if (count == 5 && oldGuesses == 2)
                        {
                            var powers = canvas.GetComponentInChildren<PowerUpListHolderView>(true);
                            foreach (float width in new[] {320f,375f,430f})
                            foreach (float height in new[] {568f,667f,812f})
                            {
                                ((RectTransform)canvas.transform).sizeDelta = new Vector2(width,height);
                                canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); game.Board.RefreshLayout(); Canvas.ForceUpdateCanvases();
                                foreach (var reward in PowerUpTypeUtility.OrderedRewardTypes)
                                {
                                    var control = (PowerUpButtonView)powers.GetPowerUpButton(reward);
                                    Require(control.GetButtonRectTransform().rect.width >= 43f,"Six powerup buttons keep a usable touch width");
                                    var label = control.transform.Find("PowerUpLabel").GetComponent<TMP_Text>(); label.ForceMeshUpdate();
                                    Require(label.text == PowerUpTypeUtility.Name(reward) && !label.isTextOverflowing,"Every powerup label fits");
                                }
                                layouts++;
                            }
                            ((RectTransform)canvas.transform).sizeDelta = new Vector2(375,812); canvas.GetComponent<FruitLabGameLayout>().ApplyLayout(); game.Board.RefreshLayout();
                            Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-split-powerup.png");
                        }
                    }
                }
                ClearPreviewActors(canvas);
                var loaded = JsonConvert.DeserializeObject<LevelSaveData>(JsonConvert.SerializeObject(checkpoint));
                using (var game = new Fixture(canvas,camera,definition))
                {
                    Field(game.Tracker,"_levelId",20); Field(game.Tracker,"_splitPowerUpCount",1);
                    game._saves.SetLevelSaveDataAsSaved(loaded); game.Round.Initialize(); game.BuildActors(); game.Target.RevealMultiplayerAnswer(loaded.TargetCards.ToArray());
                    game.Results.TryAddTriedCards(); var split = (BoardSplitService)Get(game.Results,"_split");
                    Require(split.IsActive && !split.CanApply && split.PartitionFor(count) != null,"A normal level resumes its saved manual partition");
                    game._saves.CreateDefaultLevelSaveData(); game.Round.Initialize(); game.Board.RefreshLayout();
                    Require(!split.IsActive && split.PartitionFor(count) == null,"Retry resets the manual effect");
                    Field(game.Tracker,"_splitPowerUpCount",0);
                    Require(!split.TryApply(() => game.Tracker.TryConsumePowerUp(RewardType.Split)) && !split.IsActive,"No-stock activation cannot change state");
                    Field(game.Tracker,"_gameOption",GameOption.MultiPlayer); Require(!split.CanApply,"Split remains single-player");
                    Field(game.Tracker,"_gameOption",GameOption.SinglePlayer);
                    Field(game.Round,"_remainingGuessCount",0); Require(!split.CanApply,"A failed round cannot activate Split");
                    Field(game.Tracker,"_currentRewardType",RewardType.Split); Field(game.Tracker,"_giftStarCount",ConstantValues.NUM_OF_STARS_FOR_WILD-1);
                    game.Tracker.IncrementLevelIdWithRewards(0,1);
                    Require(game.Tracker.GetPowerUpCount(RewardType.Split) == 1,"Crystal rewards can grant Split inventory");
                }
            }
            Require(profile == Profile() && errors.Count == 0,"Player data and Unity/tween checks: "+string.Join("; ",errors));
            File.WriteAllText(Folder+"/powerup-polish-review.json",JsonConvert.SerializeObject(new { passed = true,ropeFrames,layouts,
                checks = new[] { "stable reward IDs", "real Split button", "single inventory consumption", "no extra guess/life", "retroactive grouped history", "existing Helper details", "save before first guess", "manual split resume", "retry resets effect", "zero-stock/failure/multiplayer guards", "Split crystal reward", "six readable buttons", "gravity, inertial lag and damping", "no self-intersection during dragging", "continuous rope above cards", "card and control raycasts preserved", "unchanged PlayerPrefs" }
            },Formatting.Indented));
        }
        finally { DOTween.defaultUpdateType = previous; Application.logMessageReceived -= collect; }
    }

    private static void CheckVerticalLinkedCards(Canvas canvas,Camera camera)
    {
        ClearPreviewActors(canvas);
        using (var game = new Fixture(canvas,camera,new LevelData { NumOfBoardHolders = 4,NumOfCards = 8,MaxNumOfTries = 10 }))
        {
            Field(game.Tracker,"_levelId",20); game.BuildActors(); game.Target.RevealMultiplayerAnswer(new[] {2,6,1,3});
            game.View.SetPairClue(new PairClue(2,6)); var view = game.View.GetPairView();
            for (int frame = 0; frame < 120; frame++) view.RefreshRope(1f/60f);
            AssertBottomAnchors(view,game);
            AssertRopeHasNoCrossing(view.Simulation.Points);
            var card = game.Initial.GetRectTransformOfCardItem(5);
            Transform cardLayer = card;
            while (cardLayer != null && cardLayer.parent != view.transform.parent) cardLayer = cardLayer.parent;
            Require(cardLayer != null && view.transform.GetSiblingIndex() > cardLayer.GetSiblingIndex(),"Rope renders above the actual card layer");
            game.AssertHit(card,typeof(NormalCardItemView));
            Layer(canvas.transform); Draw(camera); Capture(camera,Folder+"/preview-linked-vertical.png");
            view.Show(null);
        }
    }

    private static int CheckHangingRope(Canvas canvas)
    {
        var root = new GameObject("RopeConstraintReview",typeof(RectTransform),typeof(CanvasRenderer),typeof(PairRopeGraphic));
        root.transform.SetParent(canvas.transform,false); var graphic = root.GetComponent<PairRopeGraphic>();
        var populate = typeof(PairRopeGraphic).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[] {typeof(VertexHelper)},null);
        var mesh = new Mesh(); var simulation = new PairRopeSimulation();
        try
        {
            for (int frame = 0; frame < 480; frame++)
            {
                Vector2 a = new Vector2(Mathf.Sin(frame*.047f)*140f,100f+Mathf.Cos(frame*.037f)*100f);
                Vector2 b = new Vector2(Mathf.Cos(frame*.051f)*140f,-30f+Mathf.Sin(frame*.033f)*110f);
                if (frame >= 240 && frame < 300) b.x = a.x;
                if (frame == 320) b = a;
                simulation.Advance(a,b,1f/60f);
                Require(simulation.Points[0] == a && simulation.Points[PairRopeSimulation.NodeCount-1] == b,"Moving endpoints remain pinned");
                Require(simulation.Points.All(p => !float.IsNaN(p.x) && !float.IsNaN(p.y) && p.sqrMagnitude < 1000000f),"The rope stays finite during fast crossed/vertical dragging");
                AssertRopeHasNoCrossing(simulation.Points);
                if (frame%12 != 0) continue;
                graphic.UpdateCurve(simulation);
                using (var vertices = new VertexHelper())
                {
                    populate.Invoke(graphic,new object[] {vertices}); mesh.Clear(); vertices.FillMesh(mesh);
                    Require(mesh.vertexCount > 0,"A continuous visible rope is generated");
                }
            }
            Vector2 anchorA = new Vector2(0,100), anchorB = new Vector2(180,100);
            simulation.Reset(anchorA,anchorB);
            for (int frame = 0; frame < 240; frame++) simulation.Advance(anchorA,anchorB,1f/120f);
            Vector2 center = simulation.Points[12];
            anchorA.y += 25; anchorB.y += 25;
            simulation.Advance(anchorA,anchorB,1f/60f);
            Require(simulation.Points[12].y < center.y+23f,"Moving cards leave visible inertial lag in the rope");
            float earlyMotion = 0f, lateMotion = 0f;
            for (int frame = 0; frame < 240; frame++)
            {
                center = simulation.Points[12];
                simulation.Advance(anchorA,anchorB,1f/60f);
                float movement = Vector2.Distance(center,simulation.Points[12]);
                if (frame < 30) earlyMotion += movement;
                if (frame >= 210) lateMotion += movement;
            }
            Require(lateMotion < earlyMotion*.3f,"Free oscillation settles through damping");
            return 480;
        }
        finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(root); }
    }

    private static void AssertRopeHasNoCrossing(Vector2[] points)
    {
        for (int a = 0; a < points.Length-1; a++)
        for (int b = a+2; b < points.Length-1; b++)
        {
            Vector2 u = points[a+1]-points[a], v = points[b+1]-points[b], between = points[b]-points[a];
            float cross = u.x*v.y-u.y*v.x;
            if (Mathf.Abs(cross) < .00001f) continue;
            float t = (between.x*v.y-between.y*v.x)/cross, s = (between.x*u.y-between.y*u.x)/cross;
            Require(t <= .001f || t >= .999f || s <= .001f || s >= .999f,"The rope must not form a loop around a card");
        }
    }

}

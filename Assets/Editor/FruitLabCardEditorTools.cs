using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using Game;
using Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FruitLabCardEditorTools
{
    public static void PrepareAssets()
    {
        const string framePath = "Assets/Resources/Art/FruitLabResultFrame.png";
        AssetDatabase.ImportAsset(framePath, ImportAssetOptions.ForceUpdate);
        var frameImporter = (TextureImporter)AssetImporter.GetAtPath(framePath);
        frameImporter.textureType = TextureImporterType.Sprite;
        frameImporter.spriteImportMode = SpriteImportMode.Single;
        var frameSettings = new TextureImporterSettings();
        frameImporter.ReadTextureSettings(frameSettings);
        frameSettings.spriteMeshType = SpriteMeshType.FullRect;
        frameImporter.SetTextureSettings(frameSettings);
        frameImporter.spritePixelsPerUnit = 100f;
        frameImporter.spriteBorder = new Vector4(14f, 14f, 14f, 14f);
        frameImporter.mipmapEnabled = false;
        frameImporter.alphaIsTransparency = true;
        frameImporter.filterMode = FilterMode.Bilinear;
        frameImporter.wrapMode = TextureWrapMode.Clamp;
        frameImporter.maxTextureSize = 64;
        frameImporter.textureCompression = TextureImporterCompression.Uncompressed;
        frameImporter.SaveAndReimport();
        FruitLabStyle.ResetCachedAssets();
        Sprite resultFrame = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
        Require(resultFrame != null, "Thicker feedback frame must import as a sliced sprite");
        const string shaderPath = "Assets/Resources/Art/FruitLabGhost.shader";
        const string materialPath = "Assets/Resources/Art/FruitLabGhost.mat";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Ghost shader must compile");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) AssetDatabase.CreateAsset(new Material(shader), materialPath);
        else { material.shader = shader; EditorUtility.SetDirty(material); }
        AssetDatabase.SaveAssets();
        foreach (string path in new[] { "Assets/Prefabs/CardItem.prefab", "Assets/Prefabs/NonDraggableCardItem.prefab", "Assets/Prefabs/InitialHolder.prefab", "Assets/Prefabs/BoardHolder.prefab" })
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (contents.TryGetComponent(out BaseCardItemView card))
                {
                    card.Init(1); card.SetAlpha(1f);
                    Image glow = PrepareResultImage(contents.transform, "ResultGlow", resultFrame, .9f, .32f, 0f);
                    Image outline = PrepareResultImage(contents.transform, "ResultOutline", resultFrame, 1.2f, 1f, .7f);
                    foreach(var duplicate in contents.GetComponentsInChildren<Image>(true).Where(image=>
                        (image.name=="ResultGlow" && image!=glow) || (image.name=="ResultOutline" && image!=outline)).ToArray())
                        Object.DestroyImmediate(duplicate.gameObject);
                    var serialized = new SerializedObject(card);
                    serialized.FindProperty("resultGlow").objectReferenceValue = glow;
                    serialized.FindProperty("resultOutline").objectReferenceValue = outline;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    card.SetResultOutline(CardResultState.Unrevealed);
                    if(card is NormalCardItemView normal)normal.PreparePresenceBadge();
                }
                else if (contents.TryGetComponent(out BoardHolderView board))
                {
                    foreach(var name in new[]{"SuccessFrame","Image/LabRim"})
                    {
                        var old=contents.transform.Find(name);if(old!=null)Object.DestroyImmediate(old.gameObject);
                    }
                    board.ApplyLabStyle();
                    board.SetHighlightStatus(false);
                }
                else
                {
                    var holder = contents.GetComponent<InitialHolderView>();
                    holder.ApplyLabStyle();
                    holder.SetFruit(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardItem.prefab")
                        .GetComponent<BaseCardItemView>().GetFruitSprite(1));
                }
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        ValidateAuthoredPrefabs();
    }

    private static Image PrepareResultImage(Transform parent, string name, Sprite sprite, float ppu, float alpha, float inset)
    {
        Image image = FruitLabStyle.Detail(parent, name, new Color(FruitLabStyle.Correct.r, FruitLabStyle.Correct.g, FruitLabStyle.Correct.b, alpha), true);
        image.sprite = sprite;
        image.pixelsPerUnitMultiplier = ppu;
        FruitLabStyle.Stretch(image.rectTransform, inset);
        image.transform.SetAsLastSibling();
        image.gameObject.SetActive(false);
        return image;
    }

    public static void ValidateAuthoredPrefabs()
    {
        foreach (string path in new[] { "Assets/Prefabs/CardItem.prefab", "Assets/Prefabs/NonDraggableCardItem.prefab" })
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var card = contents.GetComponent<BaseCardItemView>();
                var serialized = new SerializedObject(card);
                var glow = (Image)serialized.FindProperty("resultGlow").objectReferenceValue;
                var outline = (Image)serialized.FindProperty("resultOutline").objectReferenceValue;
                Require(glow != null && outline != null && glow.transform.parent == contents.transform && outline.transform.parent == contents.transform,
                    "Feedback images must be authored and assigned in the prefab before Init: " + path);
                Require(!glow.gameObject.activeSelf && !outline.gameObject.activeSelf, "Feedback starts hidden in the prefab");
                Require(contents.GetComponentsInChildren<Image>(true).Count(image=>image.name=="ResultGlow")==1
                    && contents.GetComponentsInChildren<Image>(true).Count(image=>image.name=="ResultOutline")==1,"One authored frame/glow pair per card");
                Require(Mathf.Approximately(glow.pixelsPerUnitMultiplier, .9f) && Mathf.Approximately(outline.pixelsPerUnitMultiplier, 1.2f), "Authored feedback PPU must be 0.9 / 1.2");
                Require(glow.sprite != null && glow.sprite.name == "FruitLabResultFrame" && glow.sprite == outline.sprite
                    && glow.type == Image.Type.Sliced && outline.type == Image.Type.Sliced, "Feedback must use the new thicker frame");
                Require(!glow.raycastTarget && !outline.raycastTarget, "Feedback must not block card or history input");
                int count = contents.GetComponentsInChildren<Transform>(true).Length;
                // Inspector edits on an instance must survive initialization and state changes.
                glow.pixelsPerUnitMultiplier = 1.07f; outline.pixelsPerUnitMultiplier = 1.37f;
                Sprite authoredSprite = outline.sprite;
                card.Init(2);
                foreach (CardResultState state in new[] { CardResultState.Correct, CardResultState.Misplaced, CardResultState.Absent, CardResultState.Unrevealed })
                    card.SetResultOutline(state);
                Require(contents.GetComponentsInChildren<Transform>(true).Length == count, "Init/result changes cannot add new card children");
                Require(Mathf.Approximately(glow.pixelsPerUnitMultiplier, 1.07f) && Mathf.Approximately(outline.pixelsPerUnitMultiplier, 1.37f)
                    && outline.sprite == authoredSprite, "Runtime state changes must preserve authored sprite and PPU");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        var boardObject = PrefabUtility.LoadPrefabContents("Assets/Prefabs/BoardHolder.prefab");
        try
        {
            var board = boardObject.GetComponent<BoardHolderView>();
            var surface = board.transform.Find("Image").GetComponent<Image>();
            Require(surface.sprite == FruitLabStyle.Inset && surface.color == FruitLabStyle.Recess
                && surface.transform.Find("LabRim") == null && board.transform.Find("SuccessFrame") == null,
                "Board prefab contains a plain recess with no second success or neutral border");
            int count = boardObject.GetComponentsInChildren<Transform>(true).Length;
            Color color=surface.color;
            board.PlaySuccessFeedback(0f).Complete(true);
            Require(boardObject.GetComponentsInChildren<Transform>(true).Length == count && surface.color==color,
                "Success only lights the existing ground temporarily and returns it to its original color");
        }
        finally { PrefabUtility.UnloadPrefabContents(boardObject); }
        Debug.Log("FRUIT_LAB_AUTHORED_PREFABS_PASS: serialized result images, exact PPU/thicker sprite, no runtime children, preserved Inspector values and authored board style.");
    }

    public static void ValidateHolder(IInitialCardHolderController holder, Sprite expected)
    {
        var root = holder.GetView().GetInitialHolderRectTransform();
        var stamp = root.Find("FruitGhost")?.GetComponent<Image>();
        Require(root.GetComponentsInChildren<TMPro.TMP_Text>().All(text => !text.gameObject.activeInHierarchy), "Holder numbers must stay hidden");
        Require(stamp != null && stamp.sprite == expected && stamp.color.a > 0f && stamp.color.a < .5f,
            "Holder must retain the matching faint fruit stamp");
        Require(stamp.material != null && stamp.material.shader.name == "FruitLab/UI/FruitGhost" && !ShaderUtil.ShaderHasError(stamp.material.shader),
            "Holder stamp must use the compiled monochrome material");
        Require(!stamp.raycastTarget, "Decorative stamp must not intercept card input");
    }

    public static void EliminateForReview(InitialCardAreaView view, InitialCardAreaLayoutManager layout, InitialCardAreaFactory factory,
        IInitialCardHolderController[] holders, INormalCardItemController[] cards, int index)
    {
        var controller = new InitialCardAreaController(view, null, null, null, null, null, layout, factory);
        var root = holders[index].GetView().GetInitialHolderRectTransform();
        var position = root.localPosition;
        cards[index] = new ImmediateCardRemoval(cards[index]);
        Field(controller, "_normalCardHolderControllerList", holders);
        Field(controller, "_normalCardItemControllerList", cards);
        Field(controller, "_numOfInitialHolders", holders.Length);
        controller.DestroyCard(index);
        controller.DestroyCard(index);
        Require(root != null && root.gameObject.activeSelf && holders[index] != null && root.localPosition == position,
            "Elimination must remove only the fruit card and leave its holder in the same grid slot");
        Require(cards[index] == null && root.GetComponentInChildren<BaseCardItemView>() == null, "Eliminated card must be removed");
        ValidateHolder(holders[index], view.GetFruitSprite(index + 1));
        controller.RefreshLayout();
        Require(root != null && root.localPosition == position, "Resizing after an elimination must preserve the holder");
    }

    private static void Field(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Fruit card review: " + message); }

    // Edit-mode adapter uses immediate Unity destruction; the real area controller
    // still owns the removal decision, collection updates and holder lifetime.
    private sealed class ImmediateCardRemoval : INormalCardItemController
    {
        private readonly INormalCardItemController _card;
        public ImmediateCardRemoval(INormalCardItemController card) => _card = card;
        public void DestroyObject() => Object.DestroyImmediate(((Component)_card.GetView()).gameObject);
        public INormalCardItemView GetView() => _card.GetView();
        public RectTransform GetRectTransform() => _card.GetRectTransform();
        public ICardViewHandler GetCardViewHandler() => _card.GetCardViewHandler();
        public ICardMoveHandler GetCardMoveHandler() => _card.GetCardMoveHandler();
        public void BackFlipAnimation(float delay, bool right, string number) => throw new NotSupportedException();
        public void SetCardAnimation(bool status) => throw new NotSupportedException();
        public void SetProbability(ProbabilityType probability, bool locked) => throw new NotSupportedException();
        public void AnimateProbabilityChange(float duration, ProbabilityType probability, bool locked) => throw new NotSupportedException();
        public void SuccessAnimation(float delay) => throw new NotSupportedException();
    }
}

public static partial class FruitLabTutorialEditorTools
{
    private static void CardFeedbackFrames(float seconds)
    {
        for(float elapsed=0;elapsed<seconds;)
        {float step=Mathf.Min(1f/60f,seconds-elapsed);DOTween.ManualUpdate(step,step);elapsed+=step;}
    }
    private static bool CardKnowledgeFrame(NormalCardItemView card) =>
        card.GetComponentsInChildren<Image>(true).Single(image=>image.name=="ResultOutline").gameObject.activeSelf;

    public static void ReviewCardKnowledge(Canvas canvas,Camera camera)
    {
        var previous=DOTween.defaultUpdateType;DOTween.defaultUpdateType=UpdateType.Manual;
        var errors=new List<string>();string prefs=Profile();
        Application.LogCallback collect=(message,stack,type)=>
        {
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert ||
                (type==LogType.Warning && message.IndexOf("DOTWEEN",StringComparison.OrdinalIgnoreCase)>=0))errors.Add(message);
        };
        Application.logMessageReceived+=collect;
        try
        {
            foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
            foreach(int slots in new[]{2,3,4,5})
            {
                ClearPreviewActors(canvas);((RectTransform)canvas.transform).sizeDelta=size;
                camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));canvas.GetComponent<FruitLabGameLayout>().ApplyLayout();
                using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=slots,NumOfCards=9,MaxNumOfTries=9}))
                {
                    game.BuildActors();game.Gate.Clear();game.Target.RevealMultiplayerAnswer(Enumerable.Range(1,slots).ToArray());
                    var manual=game.Initial.GetRectTransformOfCardItem(0).GetComponent<NormalCardItemView>();
                    Require(!manual.HasPresenceBadge && !CardKnowledgeFrame(manual),"Unknown fruit has no presence or position claim");
                    manual.SetAnnotation(CardAnnotation.Candidate);
                    Require(manual.HasPresenceBadge && !CardKnowledgeFrame(manual) && manual.CanAnnotate,"Manual present means badge only, editable and movable");
                    var corners=new Vector3[4];manual.GetRibbonImage().rectTransform.GetWorldCorners(corners);
                    Require(corners.All(point=>manual.GetRectTransform().rect.Contains((Vector2)manual.transform.InverseTransformPoint(point))),"Badge stays in the bottom-right corner of its card");
                    Require(manual.GetComponentsInChildren<Image>(true).Count(image=>image.name=="PresenceBadge")==1,"A card owns one authored shared badge");
                    Require(manual.GetRibbonImage().GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(graphic=>!graphic.raycastTarget),"The badge never intercepts card input");
                    manual.SetAnnotation(CardAnnotation.None);Require(!manual.HasPresenceBadge,"Neutral note removes the presence badge");
                    manual.SetPositionDeduction(FruitPresence.Present);Require(manual.HasPresenceBadge && !CardKnowledgeFrame(manual),"Deduced membership alone never claims a correct position");
                    manual.SetPositionDeduction(FruitPresence.Unknown);manual.SetAnnotation(CardAnnotation.Candidate);
                    game.Placement.TryPlaceCardOnBoard(0,0);CardFeedbackFrames(.4f);
                    Require(manual.HasPresenceBadge && !CardKnowledgeFrame(manual),"A movable note keeps the same badge on the board without a frame");
                    var inferred=game.Initial.GetRectTransformOfCardItem(1).GetComponent<NormalCardItemView>();
                    inferred.SetPositionDeduction(FruitPresence.Present);
                    Require(inferred.HasPresenceBadge && !CardKnowledgeFrame(inferred),"Inferred membership uses the same badge on an initial fruit");
                    if(slots==5){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-card-knowledge-notes-"+(int)size.y+".png");}
                    Require(game.Placement.TryRevealAndLockCard(0,0),"Actual reveal locks the selected position");CardFeedbackFrames(.5f);
                    Require(manual.HasPresenceBadge && CardKnowledgeFrame(manual) && !manual.CanAnnotate,"A revealed card has the shared badge and green card frame");
                    var parent=manual.transform.parent;var position=manual.transform.localPosition;
                    Require(position.sqrMagnitude<.01f && manual.transform.localScale==Vector3.one,"Lock feedback settles without changing the authoritative placement");
                    foreach(var holder in canvas.GetComponentsInChildren<BoardHolderView>(true))
                        Require(holder.transform.Find("SuccessFrame")==null && holder.transform.Find("Image/LabRim")==null,"No holder has a second border");
                    if(slots==5){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-card-knowledge-lock-"+(int)size.y+".png");}
                    for(int slot=1;slot<slots;slot++)Require(game.Placement.TryPlaceCardOnBoard(slot,slot),"Arrange remaining answer cards");CardFeedbackFrames(.4f);
                    var win=new LevelSuccessBoardAnimationController(game.Board,game.Placement).Play(.1f,.24f,.16f);CardFeedbackFrames(1.5f);
                    foreach(var item in game.Placement.GetCardsOnBoard())
                    {
                        var card=(NormalCardItemView)item.GetView();
                        Require(card.HasPresenceBadge && CardKnowledgeFrame(card),"All winning answer cards receive badge plus card frame");
                        Require(card.transform.localScale==Vector3.one && card.transform.localPosition.sqrMagnitude<.01f,"Winning cards settle without perpetual movement");
                    }
                    foreach(var holder in canvas.GetComponentsInChildren<BoardHolderView>())
                        Require(holder.transform.Find("Image").GetComponent<Image>().color==FruitLabStyle.Recess,"Holder ground returns to its normal color after the flash");
                    Require(parent==manual.transform.parent && manual.transform.localPosition==position,"Celebration preserves card parents and board positions");
                    if(slots==5){Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-card-knowledge-win-"+(int)size.y+".png");}
                    // A quick scene/retry cancellation cannot leave a partly scaled card.
                    manual.AnimateLockImage(.25f);CardFeedbackFrames(.04f);manual.gameObject.SetActive(false);CardFeedbackFrames(.4f);
                    Require(manual.transform.localScale==Vector3.one,"Disable cancels feedback and restores its scale");
                    manual.gameObject.SetActive(true);manual.Init(1);
                    Require(!manual.HasPresenceBadge && !CardKnowledgeFrame(manual),"Reinitializing clears the old badge and frame");
                }
            }
            ReviewHelperCardIsolation(canvas,camera);
            Require(prefs==Profile(),"Card feedback tests preserve real player preferences");
            Require(errors.Count==0,"No card feedback errors or tween warnings: "+string.Join("; ",errors));
            System.IO.File.WriteAllText(Folder+"/card-knowledge-review.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {
                passed=true,slots=new[]{2,3,4,5},phone_sizes=new[]{568,667,812,932},
                checks=new[]{"manual and Ultra membership badge only","one authored bottom-right badge","movable noted cards","reveal card frame and badge","no holder borders","finite settling/ground flash","left-to-right win","helper and bonus hint affect history only","cancellation/reset","real preferences preserved"}
            },Newtonsoft.Json.Formatting.Indented));
        }
        finally{Application.logMessageReceived-=collect;DOTween.defaultUpdateType=previous;}
    }

    private static void ReviewHelperCardIsolation(Canvas canvas,Camera camera)
    {
        foreach(var mechanic in new[]{LevelMechanic.Normal,LevelMechanic.Helper})
        {
            ClearPreviewActors(canvas);
            var definition=new LevelData {NumOfBoardHolders=3,NumOfCards=7,MaxNumOfTries=9,Mechanic=mechanic};
            using(var game=new Fixture(canvas,camera,definition))
            {
                game.BuildActors();game.Target.RevealMultiplayerAnswer(new[]{1,2,3});
                var helper=new HelperEffect(game.Round,game.Target,game._saves);
                using(var rules=new LevelMechanicsController(game.Results,helper,game.Round,game._saves,game.Target,game._area,game._powers,game.Tracker))
                using(var notes=AssistanceFor(game,definition,out var noteView,false))
                {
                    rules.Attach(new LevelMechanicsPresenter(game.View,game.Board),null);rules.Begin(mechanic);notes.Refresh();
                    for(int number=0;number<(mechanic==LevelMechanic.Helper?3:1);number++)
                    {game.Place(0);game.Place(2);game.Place(6);game.Check();CardFeedbackFrames(.6f);notes.Refresh();}
                    if(mechanic==LevelMechanic.Normal)Require(helper.TryApply(),"Apply the manual Helper effect to the last guess");
                    notes.Refresh();CardFeedbackFrames(.3f);
                    var row=((IResultHistorySource)game._area).GetHistoryEntry(((IResultHistorySource)game._area).HistoryCount-1);
                    Require(row.hintStates!=null && row.hintStates.Contains(CardResultState.Correct) && row.hintStates.Contains(CardResultState.Misplaced),"Both hint paths reveal green/yellow details in history");
                    foreach(var card in canvas.GetComponentsInChildren<NormalCardItemView>())
                        Require(!card.HasPresenceBadge && !CardKnowledgeFrame(card),"Helper details do not mark or frame the playable fruit cards while Ultra is off");
                    Require(game._info.GetCardItemInfoList().All(info=>!info.isLocked),"Helper does not fix a card onto the board");
                    var resultRow=canvas.GetComponentInChildren<ResultAreaView>(true).GetVisibleRow(((IResultHistorySource)game._area).HistoryCount-1);
                    Require(resultRow!=null && resultRow.GetComponentsInChildren<NonDraggableCardItemView>().Any(card=>card.GetComponentsInChildren<Image>().Any(image=>image.name=="ResultOutline")),"Hint frames remain in the result area");
                    Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-card-knowledge-helper-"+mechanic+".png");
                }
            }
        }
    }
}

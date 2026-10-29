using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
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
                    var serialized = new SerializedObject(card);
                    serialized.FindProperty("resultGlow").objectReferenceValue = glow;
                    serialized.FindProperty("resultOutline").objectReferenceValue = outline;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    card.SetResultOutline(CardResultState.Unrevealed);
                }
                else if (contents.TryGetComponent(out BoardHolderView board))
                {
                    board.ApplyLabStyle();
                    board.SetHighlightStatus(false);
                    board.SetSuccessFrameStatus(false);
                    var success = (Image)new SerializedObject(board).FindProperty("successFrameImage").objectReferenceValue;
                    success.color = FruitLabStyle.Correct; // Hidden by the GameObject; easy to preview in Prefab mode.
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
            var success = (Image)new SerializedObject(board).FindProperty("successFrameImage").objectReferenceValue;
            Require(surface.sprite == FruitLabStyle.Inset && surface.color == FruitLabStyle.Recess && surface.transform.Find("LabRim") != null,
                "Board prefab itself must contain the in-game recess and rim");
            Require(success.sprite.name == "FruitLabResultFrame" && Mathf.Approximately(success.pixelsPerUnitMultiplier, 1.2f) && !success.raycastTarget,
                "Board success frame must be authored with the thicker sliced frame");
            int count = boardObject.GetComponentsInChildren<Transform>(true).Length;
            success.pixelsPerUnitMultiplier = 1.37f;
            board.SetSuccessFrameStatus(true);
            board.PlaySuccessFrameAnimation(0f).Complete(true);
            board.SetSuccessFrameStatus(false);
            Require(boardObject.GetComponentsInChildren<Transform>(true).Length == count && Mathf.Approximately(success.pixelsPerUnitMultiplier, 1.37f),
                "Board state changes must preserve prefab structure and inspector styling");
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

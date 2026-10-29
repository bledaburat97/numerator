using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FruitLabCardEditorTools
{
    public static void PrepareAssets()
    {
        const string shaderPath = "Assets/Resources/Art/FruitLabGhost.shader";
        const string materialPath = "Assets/Resources/Art/FruitLabGhost.mat";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Ghost shader must compile");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) AssetDatabase.CreateAsset(new Material(shader), materialPath);
        else { material.shader = shader; EditorUtility.SetDirty(material); }
        AssetDatabase.SaveAssets();
        foreach (string path in new[] { "Assets/Prefabs/CardItem.prefab", "Assets/Prefabs/NonDraggableCardItem.prefab", "Assets/Prefabs/InitialHolder.prefab" })
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (contents.TryGetComponent(out BaseCardItemView card))
                {
                    card.Init(1); card.SetAlpha(1f);
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

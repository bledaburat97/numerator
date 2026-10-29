using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FruitLabLayoutValidation
{
    // Preview scenes leave the user's open scenes and unsaved changes untouched.
    [MenuItem("Tools/Fruit Lab/Validate Layout Matrix")]
    public static void Validate()
    {
        int configurations = 0;
        string[] expected = { "Apple", "Banana", "Strawberry", "Orange", "Pear", "Grape", "Blueberry", "Watermelon", "Kiwi", "Bomb", "Reveal", "Life", "Coin", "Crystal", "Settings" };
        Sprite[] sprites = Resources.LoadAll<Sprite>("Art/FruitLabIconsV2");
        Require(sprites.Length == 15 && expected.All(name => sprites.Any(sprite => sprite.name == name)), "All 15 atlas sprites must import");
        foreach (string name in expected.Take(9))
        {
            var config = AssetDatabase.LoadAssetAtPath<FruitColorConfig>("Assets/ScriptableObjects/Fruits/" + name + ".asset");
            Require(config != null && config.fruitImage != null && config.fruitImage.name == name, "Fruit config: " + name);
        }

        foreach (float height in new[] { 580f, 647f, 667f, 780f, 812f, 854f })
        for (int boardCount = 2; boardCount <= 5; boardCount++)
        for (int fruitCount = 5; fruitCount <= 9; fruitCount++)
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
            var errors = new List<string>();
            Application.LogCallback collect = (message, stack, type) => {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
            };
            Application.logMessageReceived += collect;
            try
            {
                var roots = scene.GetRootGameObjects();
                T Find<T>() where T : Component => roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).First();
                var presentation = Find<FruitLabGameLayout>();
                var canvas = presentation.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var canvasRect = (RectTransform)canvas.transform;
                canvasRect.localScale = Vector3.one;
                canvasRect.sizeDelta = new Vector2(375f, height);
                FruitLabStyle.Font = Find<GameUIView>().GetUserText().font;
                presentation.ApplyLayout();
                Canvas.ForceUpdateCanvases();
                var gameView = Find<GameUIView>();
                foreach (IBaseButtonView button in new[] { gameView.GetCheckButton(), gameView.GetResetButton(), gameView.GetSettingsButton() })
                {
                    Require(button.GetButtonRectTransform() != null, "Scene button root must be resolved from its actual prefab");
                    button.SetButtonDown(); button.SetButtonUp();
                }
                var middle = (RectTransform)canvasRect.Find("SafeArea/MiddleArea");
                var header = (RectTransform)canvasRect.Find("SafeArea/TopArea");
                Require(header.GetSiblingIndex() > middle.GetSiblingIndex(), "Full-screen middle background must not cover the header");
                Require(canvasRect.Find("SafeArea/FadePanel").GetSiblingIndex() > header.GetSiblingIndex(),
                    "Transition overlay must stay above both the header and game area");
                Require(!middle.Find("ScrollArea/Background").gameObject.activeSelf, "Old history decoration must be hidden");
                Require(!Find<InitialCardAreaView>().transform.Find("Background").gameObject.activeSelf, "Old fruit decoration must be hidden");

                var board = Find<BoardAreaView>();
                var sizes = new SizeManager();
                var boardLayout = new BoardLayoutManager(board, sizes);
                boardLayout.Initialize(boardCount);
                for (int index = 0; index < boardCount; index++)
                {
                    var slot = board.CreateBoardHolderView();
                    slot.SetLocalScale();
                    slot.SetSize(boardLayout.GetSizeOfBoardHolder());
                    slot.SetLocalPosition(boardLayout.GetBoardHolderSceneLocalPositionList()[index]);
                    AssertInside(slot.GetRectTransform(), board.GetRectTransform(), "board");
                }

                var initial = Find<InitialCardAreaView>();
                AssertSideActions(gameView, initial.GetRectTransform());
                var initialLayout = new InitialCardAreaLayoutManager(initial, sizes);
                var factory = new InitialCardAreaFactory(initial, null, initialLayout);
                var info = Enumerable.Range(0, fruitCount).Select(_ => new CardItemInfo { isExisted = true }).ToList();
                // A resumed level can already contain an eliminated fruit.
                if (height == 580f && boardCount == 2 && fruitCount == 5) info[fruitCount - 1].isExisted = false;
                var holders = factory.CreateCardHolders(fruitCount, info, new List<Vector2>());
                var items = factory.CreateCardItems(holders, info);
                if (!info[fruitCount - 1].isExisted) Require(items[fruitCount - 1] == null, "Resumed eliminated fruit must retain only its stamped holder");
                foreach (var holder in holders)
                {
                    AssertInside(holder.GetView().GetRectTransform(), (RectTransform)initial.transform, "fruit tray");
                    FruitLabCardEditorTools.ValidateHolder(holder, initial.GetFruitSprite(Array.IndexOf(holders, holder) + 1));
                }

                var results = Find<ResultAreaView>();
                var rows = new List<ResultBlockView>();
                for (int index = 0; index < 8; index++)
                {
                    var view = (ResultBlockView)results.CreateResultBlock();
                    new ResultBlockController().Initialize(view, new ResultBlockModel {
                        finalNumbers = Enumerable.Range(1, boardCount).ToList(), correctPosCount = 1, wrongPosCount = 1
                    }, results.GetWidth(), results.GetRowHeight());
                    rows.Add(view);
                }
                var scroll = results.GetComponentInParent<ScrollRect>();
                var mask = scroll.GetComponent<Mask>();
                Require(mask == null || (!mask.showMaskGraphic && scroll.GetComponent<Image>().color.a > .99f),
                    "History mask must remain opaque while its background is hidden");
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                scroll.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases();
                foreach (ResultBlockView row in rows.Skip(2))
                    AssertInside(row.GetRectTransform(), scroll.viewport, "six latest guesses");
                foreach (ResultBlockView row in rows)
                foreach (NonDraggableCardItemView fruit in row.GetComponentsInChildren<NonDraggableCardItemView>())
                {
                    AssertInside(fruit.GetComponent<RectTransform>(), (RectTransform)fruit.transform.parent, "history card before feedback");
                    Require(fruit.transform.Find("CardBorder")?.GetComponent<Image>() != null,
                        "Guessed fruit must retain its visible card frame");
                }
                Find<LevelFinishPopupView>().SetOutcome(true);
                Canvas.ForceUpdateCanvases();
                FruitLabCompletionValidation.ValidatePanelBounds(canvas);
                Find<LevelFinishPopupView>().SetOutcome(false);
                Canvas.ForceUpdateCanvases();
                FruitLabFailureValidation.ValidatePanelBounds(canvas);
                Require(errors.Count == 0, "Unity console error: " + string.Join("; ", errors));
                configurations++;
            }
            finally
            {
                Application.logMessageReceived -= collect;
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        Debug.Log("FRUIT_LAB_VALIDATION_PASS: " + configurations + " real scene/prefab configurations; 15 sprites and fruit references valid.");
    }

    [MenuItem("Tools/Fruit Lab/Validate Layout Matrix", true)]
    private static bool CanValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

    private static void AssertSideActions(GameUIView view, RectTransform tray)
    {
        RectTransform reset = view.GetResetButton().GetButtonRectTransform();
        RectTransform submit = view.GetCheckButton().GetButtonRectTransform();
        var trayCorners = new Vector3[4];
        var resetCorners = new Vector3[4];
        var submitCorners = new Vector3[4];
        tray.GetWorldCorners(trayCorners);
        reset.GetWorldCorners(resetCorners);
        submit.GetWorldCorners(submitCorners);
        Require(reset.rect.height > reset.rect.width && submit.rect.height > submit.rect.width,
            "Both side actions must be vertical");
        Require(resetCorners[2].x < trayCorners[0].x && submitCorners[0].x > trayCorners[2].x,
            "Reset stays left of the fruit tray; submit stays right, with a gap");
        Require(Mathf.Abs(reset.position.y - tray.position.y) < .2f && Mathf.Abs(submit.position.y - tray.position.y) < .2f,
            "Side actions align with the two fruit rows");
        AssertInside(reset, (RectTransform)tray.parent, "reset button");
        AssertInside(submit, (RectTransform)tray.parent, "submit button");
    }

    private static void AssertInside(RectTransform child, RectTransform parent, string description)
    {
        var corners = new Vector3[4];
        child.GetWorldCorners(corners);
        Rect limits = parent.rect;
        foreach (Vector3 corner in corners)
        {
            Vector3 point = parent.InverseTransformPoint(corner);
            Require(point.x >= limits.xMin - .2f && point.x <= limits.xMax + .2f
                && point.y >= limits.yMin - .2f && point.y <= limits.yMax + .2f,
                description + " overflow at " + point + " in " + limits);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Fruit Lab validation failed: " + message);
    }
}

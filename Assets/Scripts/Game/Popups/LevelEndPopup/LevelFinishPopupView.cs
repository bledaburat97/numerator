using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class LevelFinishPopupView : MonoBehaviour, ILevelFinishPopupView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private List<LevelFinishButtonModel> buttonModels;
        [SerializeField] private VerticalCrystalProgressView verticalCrystalProgressView;
        [SerializeField] private RectTransform failureArea;
        private bool _success = true;

        private void OnEnable() => ApplyLabLayout();

        public void ApplyLabLayout()
        {
            RectTransform root = (RectTransform)transform;
            RectTransform parent = (RectTransform)root.parent;
            if (failureArea == null)
            {
                var tray = GetComponentInParent<Canvas>().GetComponentInChildren<InitialCardAreaView>(true);
                if (tray != null) failureArea = (RectTransform)tray.transform;
            }
            float width = Mathf.Min(360f, parent.rect.width - (_success ? 32f : 24f));
            float height = 384f;
            float left = (parent.rect.width - width) * .5f, bottom = (parent.rect.height - height) * .5f;
            if (_success && failureArea != null)
            {
                var board = GetComponentInParent<Canvas>().GetComponentInChildren<BoardAreaView>(true).GetRectTransform();
                var gameArea = (RectTransform)failureArea.parent;
                float top = parent.InverseTransformPoint(board.TransformPoint(new Vector3(0f, board.rect.yMin))).y - parent.rect.yMin - 6f;
                float lower = parent.InverseTransformPoint(gameArea.TransformPoint(new Vector3(0f, gameArea.rect.yMin))).y - parent.rect.yMin + 4f;
                height = Mathf.Min(160f, top - lower);
                bottom = top - height;
            }
            else if (!_success)
            {
                if (failureArea != null)
                {
                    height = Mathf.Min(106f, Mathf.Max(72f, failureArea.rect.height - 4f));
                    Vector3 center = parent.InverseTransformPoint(failureArea.TransformPoint(failureArea.rect.center));
                    left = center.x - parent.rect.xMin - width * .5f;
                    bottom = center.y - parent.rect.yMin - height * .5f;
                }
                else { height = 96f; bottom = 52f; }
            }
            FruitLabStyle.Place(root, parent, left, bottom, width, height);
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Panel);
            FruitLabStyle.Rim(root);
            if (_success) root.Find("LabRim").GetComponent<Image>().color = new Color(FruitLabStyle.Correct.r, FruitLabStyle.Correct.g, FruitLabStyle.Correct.b, .55f);
            Vector3 titleScale = text.rectTransform.localScale;
            FruitLabStyle.Place(text.rectTransform, text.transform.parent, 12f,
                height - (_success ? 34f : 30f), width - (_success ? 72f : 24f), _success ? 26f : 24f);
            text.rectTransform.localScale = titleScale;
            text.fontSize = _success ? (height >= 144f ? 24f : 22f) : 18f;
            text.color = _success ? FruitLabStyle.Correct : FruitLabStyle.Text;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            text.alignment = _success ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
            TMP_Text subtitle = transform.Find("ResultSubtitle")?.GetComponent<TMP_Text>();
            if (subtitle == null) subtitle = FruitLabStyle.CreateText("ResultSubtitle", transform, 12f, FruitLabStyle.Muted);
            subtitle.text = _success ? "Bölüm tamamlandı." : "Sonucu incele, tekrar dene.";
            subtitle.gameObject.SetActive(_success ? height >= 144f : height >= 100f);
            subtitle.fontSize = _success ? 11f : 10f;
            subtitle.alignment = _success ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
            FruitLabStyle.Place(subtitle.rectTransform, transform, 12f, height - (_success ? 52f : 48f), width - (_success ? 72f : 24f), 14f);
            RectTransform stage = transform.Find("RewardStage") as RectTransform;
            if (stage == null)
            {
                stage = new GameObject("RewardStage", typeof(RectTransform)).GetComponent<RectTransform>();
                stage.SetParent(transform, false);
            }
            FruitLabStyle.Place(stage, transform, width - 52f, height - 48f, 40f, 40f);
            stage.gameObject.SetActive(_success);
            FruitLabGlyph mark = stage.Find("Outcome")?.GetComponent<FruitLabGlyph>();
            if (mark == null)
            {
                mark = FruitLabGlyph.Create(stage, FruitLabGlyph.Symbol.Check, FruitLabStyle.Mint);
                mark.name = "Outcome";
            }
            mark.Type = _success ? FruitLabGlyph.Symbol.Check : FruitLabGlyph.Symbol.Reset;
            mark.color = _success ? FruitLabStyle.Correct : FruitLabStyle.Gold;
            mark.SetAllDirty();
            FruitLabStyle.Stretch(mark.rectTransform, 6f);
            if (verticalCrystalProgressView != null)
            {
                float crystalHeight = height - 104f;
                FruitLabStyle.Place(verticalCrystalProgressView.GetRectTransform(), transform, 12f, 44f, width - 24f, Mathf.Max(28f, crystalHeight));
                verticalCrystalProgressView.SetPresentationTarget(stage);
                verticalCrystalProgressView.ApplyLabStyle();
            }
            foreach (LevelFinishButtonModel button in buttonModels)
            {
                if (button.buttonView == null) continue;
                RectTransform rect = (RectTransform)button.buttonView.transform;
                bool menu = button.buttonType == LevelFinishButtonType.Menu;
                float x = menu ? 12f : 116f;
                float buttonWidth = menu ? 96f : width - 128f;
                if (button.buttonType == LevelFinishButtonType.Claim) { x = 12f; buttonWidth = width - 24f; }
                if (!_success)
                {
                    x = menu ? 12f : 116f;
                    buttonWidth = menu ? 96f : width - 128f;
                }
                FruitLabStyle.Place(rect, rect.parent, x, 8f, buttonWidth,
                    _success ? 32f : Mathf.Min(40f, height * .44f));
                button.buttonView.GetComponent<BaseButtonView>().ApplyLabActionStyle(!menu);
            }
        }

        public void SetOutcome(bool success)
        {
            _success = success;
            ApplyLabLayout();
            Transform mark = transform.Find("RewardStage/Outcome");
            if (mark != null) mark.gameObject.SetActive(true);
            if (verticalCrystalProgressView != null)
            {
                verticalCrystalProgressView.SetStatus(success);
                verticalCrystalProgressView.SetAlpha(1f);
            }
        }

        public void SetRewardPresentation()
        {
            text.text = "Kazandın!";
            Transform mark = transform.Find("RewardStage/Outcome");
            if (mark != null) mark.gameObject.SetActive(false);
            TMP_Text subtitle = transform.Find("ResultSubtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null) subtitle.text = "Yeni powerup kazandın!";
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }

        public IFadeButtonView GetButton(LevelFinishButtonType buttonType)
        {
            foreach (LevelFinishButtonModel model in buttonModels)
            {
                if (model.buttonType == buttonType)
                {
                    return model.buttonView;
                }
            }
            
            return null;
        }

        public void SetText(string text)
        {
            this.text.SetText(text);
        }

        public TMP_Text GetText()
        {
            return text;
        }

        public IVerticalCrystalProgressView GetVerticalCrystalProgressView()
        {
            return verticalCrystalProgressView;
        }
    }

    public interface ILevelFinishPopupView
    {
        IFadeButtonView GetButton(LevelFinishButtonType buttonType);
        void SetText(string text);
        TMP_Text GetText();
        IVerticalCrystalProgressView GetVerticalCrystalProgressView();
        void SetStatus(bool status);
    }
    
    public enum LevelFinishButtonType
    {
        Game,
        Menu,
        Claim
    }

    [Serializable]
    public class LevelFinishButtonModel
    {
        public LevelFinishButtonType buttonType;
        public FadeButtonView buttonView;
    }
}

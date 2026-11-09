using DG.Tweening;
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
        [SerializeField] private RectTransform rewardDetails;
        [SerializeField] private CanvasGroup rewardDetailsGroup;
        [SerializeField] private FruitCollectionAwardView fruitAward;
        private IReadOnlyList<int> _earnedFruits=Array.Empty<int>();
        private bool _rewardsRevealed=true;
        private Sequence _rewardIntro;
        private bool _success = true;
        private bool _hasCrystalReward;
        private bool _multiplayer;
        private string _multiplayerSubtitle;
        private Game.RewardType? _multiplayerReward;
        private string _achievementTarget;
        public void SetAchievementTarget(string value){_achievementTarget=value;ApplyLabLayout();}
        public TMP_Text GetSubtitle()=>rewardDetails?.Find("ResultSubtitle")?.GetComponent<TMP_Text>() ?? transform.Find("ResultSubtitle")?.GetComponent<TMP_Text>();
        public void PrepareRewardDetails(IReadOnlyList<int> fruits)
        {
            _rewardIntro?.Kill();_earnedFruits=new List<int>(fruits??Array.Empty<int>()).ToArray();
            _rewardsRevealed=false;ApplyLabLayout();
        }
        public Sequence RevealRewardDetails(float duration)
        {
            EnsureRewardDetails();
            _rewardIntro?.Kill();
            return _rewardIntro=DOTween.Sequence().AppendCallback(()=>_rewardsRevealed=true)
                .Append(rewardDetailsGroup.DOFade(1,duration)).SetLink(gameObject);
        }
        private void EnsureRewardDetails()
        {
            if(rewardDetails==null)rewardDetails=transform.Find("RewardDetails") as RectTransform;
            if(rewardDetails==null){rewardDetails=new GameObject("RewardDetails",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();rewardDetails.SetParent(transform,false);}
            rewardDetails.gameObject.layer=gameObject.layer;
            FruitLabStyle.Stretch(rewardDetails);
            if(rewardDetailsGroup==null)rewardDetailsGroup=rewardDetails.GetComponent<CanvasGroup>();
            if(rewardDetailsGroup==null)rewardDetailsGroup=rewardDetails.gameObject.AddComponent<CanvasGroup>();
            rewardDetailsGroup.interactable=rewardDetailsGroup.blocksRaycasts=false;
            if(fruitAward==null)fruitAward=rewardDetails.GetComponentInChildren<FruitCollectionAwardView>(true);
            if(fruitAward==null)
            {
                var item=new GameObject("CollectionAward",typeof(RectTransform),typeof(FruitCollectionAwardView));
                item.transform.SetParent(rewardDetails,false);fruitAward=item.GetComponent<FruitCollectionAwardView>();
            }
            fruitAward.gameObject.layer=gameObject.layer;
        }

        public void SetMultiplayerResult(bool won, bool disconnected, Game.RewardType? reward)
        {
            _rewardIntro?.Kill();_earnedFruits=Array.Empty<int>();_rewardsRevealed=true;
            _multiplayer = true; _success = won; _multiplayerReward = won && !disconnected ? reward : null;
            _multiplayerSubtitle = disconnected ? "Bağlantı kesildi. Yeni bir oda açabilirsin."
                : won && reward.HasValue ? "+1 " + PowerUpTypeUtility.Name(reward.Value) + " powerup envanterine eklendi."
                : "Doğru cevabı ve tahminleri inceleyebilirsin.";
            ApplyLabLayout();
        }

        public void SetMultiplayerOpponentLeft(string message = null)
        {
            _multiplayerSubtitle = message ?? "Yeterli oyuncu kalmadı. Maç sona erdi.";
            ApplyLabLayout();
        }

        private void OnEnable() => ApplyLabLayout();
        private void OnDisable(){_rewardIntro?.Kill();_rewardIntro=null;}

        public void ApplyLabLayout()
        {
            RectTransform root = (RectTransform)transform;
            RectTransform parent = (RectTransform)root.parent;
            bool hasFruits=_success && !_multiplayer && _earnedFruits.Count>0;
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
                height = Mathf.Min(hasFruits?196f:160f, top - lower);
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
            EnsureRewardDetails();rewardDetailsGroup.alpha=_rewardsRevealed?1:0;
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
            TMP_Text subtitle = rewardDetails.Find("ResultSubtitle")?.GetComponent<TMP_Text>() ?? transform.Find("ResultSubtitle")?.GetComponent<TMP_Text>();
            if (subtitle == null) subtitle = FruitLabStyle.CreateText("ResultSubtitle", rewardDetails, 12f, FruitLabStyle.Muted);
            bool hasTarget=_success && !_multiplayer && !string.IsNullOrEmpty(_achievementTarget);
            subtitle.text = _multiplayer ? _multiplayerSubtitle : hasTarget?_achievementTarget : _success ? "Bölüm tamamlandı." : "Sonucu incele, tekrar dene.";
            subtitle.gameObject.SetActive(_multiplayer ? height >= 90f : _success ? hasTarget || (!hasFruits && height >= 144f) : height >= 100f);
            subtitle.fontSize = hasTarget?16f:_success ? 11f : 10f;
            subtitle.alignment = _success ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Midline;
            subtitle.enableAutoSizing=false;
            FruitLabStyle.Place(subtitle.rectTransform, rewardDetails, 12f, height - (hasFruits?86f:hasTarget?70f:_success ? 52f : 48f), width - (hasTarget?24f:_success ? 72f : 24f), hasTarget?20f:14f);
            if (_multiplayer) { subtitle.enableAutoSizing = true; subtitle.fontSizeMin = 9f; subtitle.fontSizeMax = 11f; }
            fruitAward.Render(_earnedFruits);fruitAward.gameObject.SetActive(hasFruits);
            FruitLabStyle.Place((RectTransform)fruitAward.transform,rewardDetails,12,height-64,width-24,26);fruitAward.ApplyLayout();
            RectTransform stage = transform.Find("RewardStage") as RectTransform;
            if (stage == null)
            {
                stage = new GameObject("RewardStage", typeof(RectTransform)).GetComponent<RectTransform>();
                stage.SetParent(transform, false);
            }
            FruitLabStyle.Place(stage, transform, width - (hasFruits?44f:52f), height - (hasFruits?38f:48f), hasFruits?32f:40f, hasFruits?32f:40f);
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
            mark.gameObject.SetActive(!_multiplayerReward.HasValue);
            var rewardIcon = stage.Find("MultiplayerReward")?.GetComponent<Image>();
            if (_multiplayerReward.HasValue)
            {
                if (rewardIcon == null) rewardIcon = FruitLabStyle.Detail(stage, "MultiplayerReward", Color.white);
                FruitLabStyle.SetIcon(rewardIcon, PowerUpTypeUtility.IconName(_multiplayerReward.Value));
                FruitLabStyle.Stretch(rewardIcon.rectTransform, 2f);
            }
            if (rewardIcon != null) rewardIcon.gameObject.SetActive(_multiplayerReward.HasValue);
            if (verticalCrystalProgressView != null)
            {
                float crystalHeight = height - (hasFruits?(hasTarget?146f:124f):hasTarget?120f:104f);
                FruitLabStyle.Place(verticalCrystalProgressView.GetRectTransform(), rewardDetails, 12f, hasFruits?56f:44f, width - 24f, Mathf.Max(hasFruits?24f:28f, crystalHeight));
                verticalCrystalProgressView.SetPresentationTarget((RectTransform)GetComponentInParent<Canvas>().rootCanvas.transform);
                verticalCrystalProgressView.ApplyLabStyle();
                verticalCrystalProgressView.SetStatus(_success && !_multiplayer && _hasCrystalReward);
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
                    _success ? (hasFruits?44f:32f) : Mathf.Min(40f, height * .44f));
                button.buttonView.GetComponent<BaseButtonView>().ApplyLabActionStyle(!menu);
            }
        }

        public void ResetTitle()
        {
            if (text == null) return;
            text.gameObject.SetActive(false); text.alpha = 1;
        }
        public void PrepareTitle(string value)
        {
            if (text == null) return;
            text.gameObject.SetActive(true); text.alpha = 1; SetText(value);
            text.rectTransform.localScale = Vector3.zero;
        }
        public Sequence PlayTitleIntro(float duration) => text == null ? DOTween.Sequence()
            : DOTween.Sequence().Append(text.rectTransform.DOScale(1, duration)).SetLink(gameObject);
        public Sequence FadeButton(LevelFinishButtonType type, float duration, float alpha)
        {
            var group = GetButton(type)?.GetCanvasGroup();
            if(group==null)return DOTween.Sequence();
            return DOTween.Sequence().AppendCallback(()=>group.interactable=group.blocksRaycasts=alpha>.01f)
                .Append(group.DOFade(alpha,duration)).SetLink(gameObject);
        }
        public Sequence FadeButtons(float duration, float alpha) => DOTween.Sequence()
            .Join(FadeButton(LevelFinishButtonType.Game, duration, alpha))
            .Join(FadeButton(LevelFinishButtonType.Menu, duration, alpha)).SetLink(gameObject);
        public void SetButtonStatus(LevelFinishButtonType type, bool active) => GetButton(type)?.SetButtonStatus(active);
        public void SetButtonAlpha(LevelFinishButtonType type, float alpha) => GetButton(type)?.SetAlpha(alpha);
        public void ResetRewardProgress()
        {
            if (verticalCrystalProgressView == null) return;
            verticalCrystalProgressView.SetAlpha(0); verticalCrystalProgressView.ResetFill(); verticalCrystalProgressView.SetStatus(false);
        }

        public void SetOutcome(bool success, int earnedCrystalCount = 0)
        {
            _rewardIntro?.Kill();_earnedFruits=Array.Empty<int>();_rewardsRevealed=true;
            _achievementTarget=null;
            _multiplayer = false; _multiplayerReward = null;
            _success = success;
            _hasCrystalReward = success && earnedCrystalCount > 0;
            ApplyLabLayout();
            Transform mark = transform.Find("RewardStage/Outcome");
            if (mark != null) mark.gameObject.SetActive(true);
            if (verticalCrystalProgressView != null)
            {
                verticalCrystalProgressView.SetAlpha(1f);
            }
        }

        public void SetRewardPresentation()
        {
            text.text = _multiplayer?"Kazandın!":"Çözdün!";
            Transform mark = transform.Find("RewardStage/Outcome");
            if (mark != null) mark.gameObject.SetActive(false);
            TMP_Text subtitle = rewardDetails?.Find("ResultSubtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null && string.IsNullOrEmpty(_achievementTarget)) subtitle.text = "Yeni powerup kazandın!";
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

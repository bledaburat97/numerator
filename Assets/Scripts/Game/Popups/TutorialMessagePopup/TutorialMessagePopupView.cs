using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public enum TutorialMessagePlacement { Top, Bottom, Center }

    public class TutorialMessagePopupView : MonoBehaviour, ITutorialMessagePopupView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TMP_Text title;
        [SerializeField] private BaseButtonView nextButton;
        [SerializeField] private BaseButtonView secondaryButton;
        private TutorialMessagePlacement _placement;
        private Image _surface, _rim;
        private Vector2 _lastParentSize;
        private RectTransform _currencyFlow;
        private Tween _intro;
        public void FadeIn(float duration)
        {
            _intro?.Kill();
            var group=GetComponent<CanvasGroup>()??gameObject.AddComponent<CanvasGroup>();
            group.alpha=0;group.interactable=false;
            _intro=group.DOFade(1,duration).OnComplete(()=>group.interactable=true).SetLink(gameObject,LinkBehaviour.KillOnDestroy);
        }
        private void OnDestroy()=>_intro?.Kill();
        public void ShowCrystalFlow()
        {
            if(_currencyFlow!=null)return;
            _currencyFlow=FruitLabOnlineLayout.Rect("CurrencyFlow",transform);
            var token=FruitLabStyle.Detail(_currencyFlow,"Crystal",Color.white);FruitLabStyle.SetIcon(token,"Crystal");
            var arrow=FruitLabStyle.CreateText("Arrow",_currencyFlow,25,FruitLabStyle.Background);arrow.text="→";arrow.alignment=TextAlignmentOptions.Center;
            var power=FruitLabStyle.Detail(_currencyFlow,"Power",Color.white);FruitLabStyle.SetIcon(power,"Reveal");
            ApplyLayout();
        }
        public void Init()
        {
            transform.localScale = Vector3.one;
            if (secondaryButton != null) secondaryButton.SetButtonStatus(false);
            transform.SetAsLastSibling(); ApplyLayout();
        }
        public void Bind(BaseButtonControllerFactory factory, Action next)
        {
            var nextControl = factory.Create(nextButton, next); nextControl.SetClickOnRelease(true);
            if (secondaryButton != null) secondaryButton.SetButtonStatus(false);
            ApplyLayout();
        }
        public void Show(string heading, string body, bool canContinue, bool bottom = false, string continueText = "Tamam")
            => ShowAt(heading, body, canContinue, bottom ? TutorialMessagePlacement.Bottom : TutorialMessagePlacement.Top, continueText);

        public void ShowAt(string heading, string body, bool canContinue, TutorialMessagePlacement placement, string continueText = "Tamam")
        {
            title.text = heading; text.text = body; _placement = placement;
            nextButton.SetButtonStatus(canContinue); nextButton.SetText(continueText);
            ApplyLayout();
        }
        public void Destroy() => TutorialObjectUtility.Destroy(gameObject);
        public void SetText(string value) => text.SetText(value);
        private void LateUpdate()
        {
            if (transform.parent is RectTransform parent && (parent.rect.size - _lastParentSize).sqrMagnitude > .01f) ApplyLayout();
        }
        public void ApplyLayout()
        {
            var parent = transform.parent as RectTransform;
            _lastParentSize = parent != null ? parent.rect.size : Vector2.zero;
            float available = parent != null && parent.rect.width > 1 ? parent.rect.width : 375f;
            var board=GetComponentInParent<Canvas>()?.rootCanvas.GetComponentInChildren<BoardAreaView>(true)?.GetRectTransform();
            var corners=new Vector3[4];Rect boardBounds=default;
            if(board!=null && parent!=null)
            {
                board.GetWorldCorners(corners);var min=parent.InverseTransformPoint(corners[0]);var max=parent.InverseTransformPoint(corners[2]);
                boardBounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            }
            float width = Mathf.Min(available - 16f,board!=null?boardBounds.width:available-24f);
            var bodyFont=Resources.Load<TMP_FontAsset>("Fonts/Tutorial/TutorialBody");
            var headingFont=Resources.Load<TMP_FontAsset>("Fonts/Tutorial/TutorialTitle");
            if(bodyFont!=null)text.font=bodyFont;
            if(headingFont!=null)title.font=headingFont;
            if (text != null) { text.fontSize = 19f; text.fontStyle=FontStyles.Normal; text.enableAutoSizing = false; text.lineSpacing = 1f; text.textWrappingMode=TextWrappingModes.Normal; }
            if (title != null) { title.fontSize = 24f; title.enableAutoSizing = false; title.fontStyle = FontStyles.Normal; title.textWrappingMode=TextWrappingModes.Normal; }
            if (nextButton != null)
                foreach (TMP_Text buttonLabel in nextButton.GetComponentsInChildren<TMP_Text>(true)) { if(bodyFont!=null)buttonLabel.font=bodyFont;buttonLabel.fontStyle=FontStyles.Normal;buttonLabel.fontSize = 19f; buttonLabel.enableAutoSizing = false; }
            float bodyHeight = text != null ? Mathf.Max(48f, text.GetPreferredValues(text.text, width - 48f, 0f).y + 4f) : 48f;
            float titleHeight=title!=null?Mathf.Max(28f,title.GetPreferredValues(title.text,width-48f,0f).y+2f):28f;
            bool actions = nextButton != null && nextButton.gameObject.activeSelf;
            float height = bodyHeight + titleHeight + (actions ? 124f : 68f)+(_currencyFlow!=null?58:0);
            bool centered = _placement == TutorialMessagePlacement.Center;
            bool bottom = _placement == TutorialMessagePlacement.Bottom;
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, centered ? .5f : bottom ? 0f : 1f);
            rectTransform.pivot = rectTransform.anchorMin;
            rectTransform.anchoredPosition = centered ? Vector2.zero : new Vector2(0f, bottom ? 12f : -90f);
            rectTransform.sizeDelta = new Vector2(width, height);
            if(board!=null && parent!=null)
            {
                float lower=_placement==TutorialMessagePlacement.Top?parent.rect.yMax-90-height:boardBounds.yMax+6;
                lower=Mathf.Max(lower,boardBounds.yMax+6);
                rectTransform.anchorMin=rectTransform.anchorMax=new Vector2(.5f,0);
                rectTransform.pivot=new Vector2(.5f,0);
                rectTransform.anchoredPosition=new Vector2(boardBounds.center.x-parent.rect.center.x,lower-parent.rect.yMin);
            }
            ApplyAppearance();
            if (title == null || nextButton == null) return;
            FruitLabStyle.Place(title.rectTransform, rectTransform, 24f, height - 36f - titleHeight, width - 48f, titleHeight);
            FruitLabStyle.Place(text.rectTransform, rectTransform, 24f, height - 40f - titleHeight - bodyHeight, width - 48f, bodyHeight);
            FruitLabStyle.Place(nextButton.GetButtonRectTransform(), rectTransform, 24f, 28f, width - 48f, 44f);
            if(_currencyFlow!=null)
            {
                FruitLabStyle.Place(_currencyFlow,rectTransform,24,84,width-48,50);
                float x=(width-48-112)*.5f;
                FruitLabStyle.Place((RectTransform)_currencyFlow.Find("Crystal"),_currencyFlow,x,7,36,36);
                FruitLabStyle.Place((RectTransform)_currencyFlow.Find("Arrow"),_currencyFlow,x+40,8,28,34);
                FruitLabStyle.Place((RectTransform)_currencyFlow.Find("Power"),_currencyFlow,x+76,7,36,36);
            }
        }

        private void ApplyAppearance()
        {
            if (_surface == null) _surface = GetComponent<Image>();
            if (_rim == null) _rim = transform.Find("LabRim")?.GetComponent<Image>();
            PopupArtStyle.Apply(gameObject,true);
            if (_rim != null) _rim.gameObject.SetActive(false);
            if (title != null)
            {
                title.color = PopupArtStyle.Ink;
                title.alignment = TextAlignmentOptions.Midline;
            }
            if (text != null) text.color = PopupArtStyle.Body;
        }
    }
    public interface ITutorialMessagePopupView
    {
        void Init();
        void Destroy();
        void SetText(string text);
    }
}

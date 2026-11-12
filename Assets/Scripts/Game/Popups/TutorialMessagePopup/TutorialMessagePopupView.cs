using System;
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
        public void ShowCurrencyFlow()
        {
            if(_currencyFlow!=null)return;
            _currencyFlow=FruitLabOnlineLayout.Rect("CurrencyFlow",transform);
            for(int group=0;group<2;group++)
            {
                var token=FruitLabStyle.Detail(_currencyFlow,group==0?"Coin":"Crystal",Color.white);
                FruitLabStyle.SetIcon(token,group==0?"Coin":"Crystal");
                var arrow=FruitLabStyle.CreateText("Arrow"+group,_currencyFlow,25,FruitLabStyle.Background);arrow.text="→";arrow.alignment=TextAlignmentOptions.Center;
                if(group==0){var seed=FruitLabGlyph.Create(_currencyFlow,FruitLabGlyph.Symbol.Seed,FruitLabStyle.Background);seed.name="Seed";}
                else {var power=FruitLabStyle.Detail(_currencyFlow,"Power",Color.white);FruitLabStyle.SetIcon(power,"Reveal");}
            }
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
            float width = Mathf.Min(335f, available - 24f);
            if (text != null) { text.fontSize = 19f; text.enableAutoSizing = false; text.lineSpacing = 3f; text.textWrappingMode=TextWrappingModes.Normal; }
            if (title != null) { title.fontSize = 21f; title.enableAutoSizing = false; title.fontStyle = FontStyles.Bold; title.textWrappingMode=TextWrappingModes.Normal; }
            if (nextButton != null)
                foreach (TMP_Text buttonLabel in nextButton.GetComponentsInChildren<TMP_Text>(true)) { buttonLabel.fontSize = 18f; buttonLabel.enableAutoSizing = false; }
            float bodyHeight = text != null ? Mathf.Max(56f, text.GetPreferredValues(text.text, width - 32f, 0f).y + 4f) : 56f;
            float titleHeight=title!=null?Mathf.Max(28f,title.GetPreferredValues(title.text,width-32f,0f).y+2f):28f;
            bool actions = nextButton != null && nextButton.gameObject.activeSelf;
            float height = bodyHeight + titleHeight + (actions ? 90f : 42f)+(_currencyFlow!=null?58:0);
            bool centered = _placement == TutorialMessagePlacement.Center;
            bool bottom = _placement == TutorialMessagePlacement.Bottom;
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(.5f, centered ? .5f : bottom ? 0f : 1f);
            rectTransform.pivot = rectTransform.anchorMin;
            rectTransform.anchoredPosition = centered ? Vector2.zero : new Vector2(0f, bottom ? 12f : -90f);
            rectTransform.sizeDelta = new Vector2(width, height);
            ApplyAppearance();
            if (title == null || nextButton == null) return;
            FruitLabStyle.Place(title.rectTransform, rectTransform, 16f, height - 16f - titleHeight, width - 32f, titleHeight);
            FruitLabStyle.Place(text.rectTransform, rectTransform, 16f, height - 24f - titleHeight - bodyHeight, width - 32f, bodyHeight);
            FruitLabStyle.Place(nextButton.GetButtonRectTransform(), rectTransform, 16f, 12f, width - 32f, 44f);
            if(_currencyFlow!=null)
            {
                FruitLabStyle.Place(_currencyFlow,rectTransform,16,62,width-32,50);
                float half=(width-32)*.5f;
                for(int group=0;group<2;group++)
                {
                    float x=group*half+(half-112)*.5f;
                    FruitLabStyle.Place((RectTransform)_currencyFlow.Find(group==0?"Coin":"Crystal"),_currencyFlow,x,7,36,36);
                    FruitLabStyle.Place((RectTransform)_currencyFlow.Find("Arrow"+group),_currencyFlow,x+40,8,28,34);
                    FruitLabStyle.Place((RectTransform)_currencyFlow.Find(group==0?"Seed":"Power"),_currencyFlow,x+76,7,36,36);
                }
            }
        }

        private void ApplyAppearance()
        {
            if (_surface == null) _surface = GetComponent<Image>();
            if (_rim == null) _rim = transform.Find("LabRim")?.GetComponent<Image>();
            if (_surface != null) _surface.color = Color.Lerp(FruitLabStyle.Text,FruitLabStyle.Mint,.15f);
            if (_rim != null) _rim.color = FruitLabStyle.Mint;
            if (title != null)
            {
                title.color = FruitLabStyle.Background;
                title.alignment = TextAlignmentOptions.Midline;
            }
            if (text != null) text.color = FruitLabStyle.Background;
        }
    }
    public interface ITutorialMessagePopupView
    {
        void Init();
        void Destroy();
        void SetText(string text);
    }
}

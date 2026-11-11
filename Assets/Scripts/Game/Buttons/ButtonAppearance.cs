using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public enum ButtonVisualKind { Standard, Label, Confirm, Clear, Settings }

    // Explicit presentation policy. The shared pointer view knows neither roles nor level data.
    [DisallowMultipleComponent]
    public sealed class ButtonAppearance : MonoBehaviour,IButtonPresentation
    {
        [SerializeField] private BaseButtonView button;
        [SerializeField] private ButtonVisualKind kind;
        [SerializeField] private bool primary;
        [SerializeField] private Image frame;
        [SerializeField] private FruitLabGlyph actionIcon;
        [SerializeField] private FruitLabGlyph levelBadge;
        private LevelData _levelIdentity;
        public ButtonVisualKind Kind=>kind;
        public bool IsPrimary=>primary;
        public BaseButtonView Button=>button;
        public FruitLabGlyph ActionIcon=>actionIcon;
        public FruitLabGlyph LevelBadge=>levelBadge;
        public void ConfigureGlyphs(FruitLabGlyph action,FruitLabGlyph badge){actionIcon=action;levelBadge=badge;}
        public static ButtonAppearance For(BaseButtonView view)
        {
            var appearance=view.Presentation as ButtonAppearance;
            if(appearance!=null)return appearance;
            appearance=view.GetComponent<ButtonAppearance>()??view.gameObject.AddComponent<ButtonAppearance>();
            appearance.button=view;view.SetPresentation(appearance);return appearance;
        }
        public void Configure(BaseButtonView view,ButtonVisualKind role,bool isPrimary,Image rim=null)
        {button=view;kind=role;primary=isPrimary;frame=rim;view.SetPresentation(this);}
        public void SetActionStyle(bool isPrimary){kind=ButtonVisualKind.Label;primary=isPrimary;Apply();}
        public void SetSettingsStyle(){kind=ButtonVisualKind.Settings;primary=false;Apply();}
        public void SetIconAction(ButtonVisualKind role,bool isPrimary){kind=role;primary=isPrimary;Apply();}
        public void ApplyLevelIdentity(LevelData definition){_levelIdentity=definition?.Copy();Apply();}
        public void ApplyActionAccent(Color tint)
        {FruitLabStyle.Surface(button.PressedSurface,tint);FruitLabStyle.Surface(button.OuterSurface,tint*.72f);}
        public void Apply()
        {
            if(button==null)return;
            FruitLabStyle.Surface(button.OuterSurface,primary?FruitLabStyle.Mint*.7f:FruitLabStyle.Border);
            FruitLabStyle.Surface(button.PressedSurface,primary?FruitLabStyle.Mint:FruitLabStyle.Tile);
            FruitLabStyle.Surface(button.Shadow,new Color(0,0,0,.22f));
            if(frame!=null)FruitLabStyle.Surface(frame,FruitLabStyle.Border,true);
            if(actionIcon!=null)actionIcon.gameObject.SetActive(kind==ButtonVisualKind.Confirm || kind==ButtonVisualKind.Clear);
            if(levelBadge!=null)levelBadge.gameObject.SetActive(kind==ButtonVisualKind.Label && _levelIdentity!=null);
            if(kind==ButtonVisualKind.Settings)
            {
                button.SetTextStatus(false);
                if(button.Icon==null)button.SetIconReference(FruitLabStyle.Detail(transform,"SettingsIcon",Color.white));
                button.Icon.gameObject.SetActive(true);button.Icon.enabled=true;button.Icon.raycastTarget=false;
                FruitLabStyle.SetIcon(button.Icon,"Settings");
                SetIconInset(9f);return;
            }
            if(kind==ButtonVisualKind.Label)
            {
                button.SetImageStatus(false);
                var label=button.Label;
                if(label==null)
                {
                    label=FruitLabStyle.CreateText("Label",transform,15,FruitLabStyle.Text);
                    button.ConfigureReferences(button.GetButtonRectTransform(),label,button.OuterSurface,button.Shadow,button.PressedSurface,button.Icon);
                }
                label.gameObject.SetActive(true);label.color=primary?FruitLabStyle.Background:FruitLabStyle.Text;
                FruitLabStyle.TextStyle(label,18);label.enableAutoSizing=false;label.fontStyle=FontStyles.Bold;
                label.alignment=TextAlignmentOptions.Midline;label.raycastTarget=false;FruitLabStyle.Stretch(label.rectTransform,3);
                if(_levelIdentity!=null)
                {
                    var identity=LevelIdentity.For(_levelIdentity);ApplyActionAccent(identity.Accent);label.color=FruitLabStyle.Background;
                    if(levelBadge==null){levelBadge=FruitLabGlyph.Create(transform,identity.Symbol,FruitLabStyle.Background);levelBadge.name="LevelKind";}
                    levelBadge.gameObject.SetActive(true);levelBadge.Type=identity.Symbol;levelBadge.color=FruitLabStyle.Background;levelBadge.SetAllDirty();
                    FruitLabStyle.Place(levelBadge.rectTransform,transform,12,(button.GetButtonRectTransform().rect.height-26)*.5f,26,26);
                    label.rectTransform.offsetMin=new Vector2(44,3);label.rectTransform.offsetMax=new Vector2(-6,-3);
                }
                return;
            }
            if(kind!=ButtonVisualKind.Confirm && kind!=ButtonVisualKind.Clear)return;
            button.SetTextStatus(false);button.SetImageStatus(false);
            var symbol=kind==ButtonVisualKind.Confirm?FruitLabGlyph.Symbol.Check:FruitLabGlyph.Symbol.Eraser;
            if(actionIcon==null){actionIcon=FruitLabGlyph.Create(transform,symbol,Color.white);actionIcon.name="ActionIcon";}
            actionIcon.Type=symbol;actionIcon.color=primary?FruitLabStyle.Background:FruitLabStyle.Text;
            actionIcon.SetAllDirty();actionIcon.gameObject.SetActive(true);
            var rect=button.GetButtonRectTransform().rect;float size=Mathf.Min(38,Mathf.Min(rect.width-10,rect.height-10));
            FruitLabStyle.Place(actionIcon.rectTransform,transform,(rect.width-size)*.5f,(rect.height-size)*.5f,size,size);
        }
        public void SetIconInset(float inset)
        {
            if(button.Icon==null)return;
            var root=button.GetButtonRectTransform();var parent=button.Icon.rectTransform.parent as RectTransform;if(parent==null)return;
            Rect bounds=root.rect;
            Vector2 min=parent.InverseTransformPoint(root.TransformPoint(new Vector3(bounds.xMin+inset,bounds.yMin+inset)));
            Vector2 max=parent.InverseTransformPoint(root.TransformPoint(new Vector3(bounds.xMax-inset,bounds.yMax-inset)));
            FruitLabStyle.Place(button.Icon.rectTransform,parent,min.x-parent.rect.xMin,min.y-parent.rect.yMin,max.x-min.x,max.y-min.y);
        }
    }
}

using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public sealed class PowerUpButtonView : BaseButtonView
    {
        [SerializeField] private RewardType type;
        [SerializeField] private RectTransform countHolder;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private FruitLabGlyph powerGlyph;
        public RewardType Type=>type;
        public TMP_Text CountText=>countText;
        public RectTransform CountHolder=>countHolder;
        public FruitLabGlyph PowerGlyph=>powerGlyph;
        public void ConfigureParts(Image picture,Image background,RectTransform holder,TMP_Text count,FruitLabGlyph glyph)
        {
            ConfigureReferences((RectTransform)transform,null,null,null,background,picture);
            SetPresentation(null);countHolder=holder;countText=count;powerGlyph=glyph;
        }
        public void ConfigureType(RewardType value)
        {
            type=value;var icon=PowerUpDefinition.Get(type).IconName;
            bool custom=type==RewardType.Helper || type==RewardType.Pair || type==RewardType.Split || type==RewardType.Ultra;
            FruitLabStyle.SetIcon(image,icon);image.enabled=!custom;image.raycastTarget=false;
            powerGlyph.gameObject.SetActive(custom);
            if(!custom)return;
            powerGlyph.Type=type==RewardType.Helper?FruitLabGlyph.Symbol.Helper:type==RewardType.Pair?FruitLabGlyph.Symbol.Pair:
                type==RewardType.Split?FruitLabGlyph.Symbol.Split:FruitLabGlyph.Symbol.Ultra;
            powerGlyph.color=Color.white;powerGlyph.SetAllDirty();FruitLabStyle.Stretch(powerGlyph.rectTransform);
        }
        public void ApplyPreview(RewardType value)
        {
            ConfigureType(value);countHolder.gameObject.SetActive(false);FruitLabStyle.Stretch(image.rectTransform,5);
            FruitLabStyle.Stretch(PressedSurface.rectTransform);FruitLabStyle.Surface(PressedSurface,FruitLabStyle.Recess);
        }
        public void ApplyLayout(RewardType value,float width,float height)
        {
            ConfigureType(value);FruitLabStyle.Stretch(PressedSurface.rectTransform);FruitLabStyle.Surface(PressedSurface,FruitLabStyle.Tile);
            float size=Mathf.Min(40,Mathf.Min(width-10,height-10));
            FruitLabStyle.Place(image.rectTransform,transform,(width-size)*.5f,(height-size)*.5f+3,size,size);
            float badgeWidth=Mathf.Min(width-4,30);countHolder.gameObject.SetActive(true);
            FruitLabStyle.Place(countHolder,transform,width-badgeWidth-1,0,badgeWidth,20);
            FruitLabStyle.Surface(countHolder.GetComponent<Image>(),FruitLabStyle.Recess);countHolder.SetAsLastSibling();
            FruitLabStyle.Stretch(countText.rectTransform);countText.gameObject.SetActive(true);
            FruitLabStyle.TextStyle(countText,16);countText.color=FruitLabStyle.Text;
            countText.alignment=TextAlignmentOptions.Midline;countText.raycastTarget=false;
        }
    }
}

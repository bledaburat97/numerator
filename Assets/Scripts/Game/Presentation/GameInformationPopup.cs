using System;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public static class LevelInformation
    {
        public static string Title(LevelMechanic mechanic)
        {
            switch(mechanic)
            {
                case LevelMechanic.Helper:return "Bonus İpucu";
                case LevelMechanic.Pair:return "Ardışık Bağlı Meyveler";
                case LevelMechanic.Split:return "Bölünmüş Tahta";
                case LevelMechanic.Growing:return "Büyüyen Tahta";
                case LevelMechanic.Dynamite:return "Dinamitli Tahta";
                default:return null;
            }
        }
        public static string Description(LevelMechanic mechanic)
        {
            switch(mechanic)
            {
                case LevelMechanic.Helper:return "Her 3 yanlış tahminde bir tahmindeki meyvelerin detaylı bilgisi verilir.";
                case LevelMechanic.Pair:return "Cevapta yan yana bulunan iki meyve iple bağlıdır. Sıralarını sen bulursun.";
                case LevelMechanic.Split:return "İki grubun sonuçları ayrı hesaplanır, yerleştirilen meyveler kendi grubunda değerlendirilir.";
                case LevelMechanic.Growing:return "Her 2 yanlışta bir alan açılır. 4 alana ulaşınca 4 ek hak kazanırsın.";
                case LevelMechanic.Dynamite:return "İlk 4 hamlede dinamitli alan için doğru meyveyi bulmalısın yoksa oyun kaybedilir.";
                default:return null;
            }
        }
    }

    // One scrollable reference page. It never marks a tutorial seen or spends a power.
    public sealed class GameInformationPopup : MonoBehaviour
    {
        public static GameInformationPopup Open(Transform parent,LevelMechanic mechanic,Action closed)
        {
            var root=new GameObject("GameInformation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
            root.transform.SetParent(parent,false);FruitLabStyle.Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color=new Color(0,0,0,.55f);
            var popup=root.AddComponent<GameInformationPopup>();
            void Close(){if(popup==null)return;TutorialObjectUtility.Destroy(root);closed?.Invoke();}
            root.GetComponent<Button>().onClick.AddListener(Close);
            var available=(RectTransform)parent;
            float width=Mathf.Min(360,Mathf.Max(260,available.rect.width-24));
            float height=Mathf.Min(650,Mathf.Max(300,available.rect.height-32));
            var panel=(RectTransform)new GameObject("Panel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)).transform;
            panel.SetParent(root.transform,false);panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one*.5f;panel.sizeDelta=new Vector2(width,height);
            FruitLabStyle.AddSurface(panel.gameObject,FruitLabStyle.Panel).raycastTarget=true;FruitLabStyle.Rim(panel,.45f);
            var title=FruitLabStyle.CreateText("Title",panel,22,FruitLabStyle.Text);title.text="Oyun bilgisi";
            FruitLabStyle.Place(title.rectTransform,panel,16,height-52,width-32,36);
            var scroll=(RectTransform)new GameObject("Explanations",typeof(RectTransform),typeof(ScrollRect)).transform;
            scroll.SetParent(panel,false);FruitLabStyle.Place(scroll,panel,12,74,width-24,height-134);
            var viewport=(RectTransform)new GameObject("Viewport",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RectMask2D)).transform;
            viewport.SetParent(scroll,false);FruitLabStyle.Stretch(viewport);viewport.GetComponent<Image>().color=Color.clear;
            var content=(RectTransform)new GameObject("Content",typeof(RectTransform)).transform;
            content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
            var scroller=scroll.GetComponent<ScrollRect>();scroller.viewport=viewport;scroller.content=content;scroller.horizontal=false;
            scroller.movementType=ScrollRect.MovementType.Clamped;scroller.scrollSensitivity=28;
            float y=0;
            void Row(string name,string text,RewardType? power)
            {
                float rowHeight=power.HasValue?102:112;
                var row=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;row.SetParent(content,false);
                row.anchorMin=row.anchorMax=new Vector2(0,1);row.pivot=new Vector2(0,1);row.anchoredPosition=new Vector2(0,-y);row.sizeDelta=new Vector2(width-24,rowHeight);
                float left=power.HasValue?48:8;
                if(power.HasValue)
                {
                    var type=power.Value;
                    if(type==RewardType.Bomb || type==RewardType.Revealing || type==RewardType.Life)
                    {
                        var icon=(RectTransform)new GameObject("Icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).transform;
                        icon.SetParent(row,false);FruitLabStyle.SetIcon(icon.GetComponent<Image>(),PowerUpTypeUtility.IconName(type));
                        icon.GetComponent<Image>().raycastTarget=false;FruitLabStyle.Place(icon,row,5,rowHeight-45,36,36);
                    }
                    else
                    {
                        var symbol=type==RewardType.Ultra?FruitLabGlyph.Symbol.Ultra:type==RewardType.Pair?FruitLabGlyph.Symbol.Pair:type==RewardType.Split?FruitLabGlyph.Symbol.Split:FruitLabGlyph.Symbol.Helper;
                        var glyph=FruitLabGlyph.Create(row,symbol,type==RewardType.Ultra?FruitLabStyle.Pink:FruitLabStyle.Mint);
                        FruitLabStyle.Place(glyph.rectTransform,row,8,rowHeight-42,30,30);
                    }
                }
                var heading=FruitLabStyle.CreateText("Name",row,18,FruitLabStyle.Text);heading.text=name;heading.alignment=TextAlignmentOptions.MidlineLeft;
                FruitLabStyle.Place(heading.rectTransform,row,left,rowHeight-32,width-40-left,28);
                var body=FruitLabStyle.CreateText("Description",row,16,FruitLabStyle.Muted);body.text=text;body.alignment=TextAlignmentOptions.TopLeft;
                body.fontStyle=FontStyles.Normal;body.textWrappingMode=TextWrappingModes.Normal;FruitLabStyle.Place(body.rectTransform,row,left,4,width-40-left,rowHeight-36);
                y+=rowHeight+6;
            }
            if(LevelInformation.Title(mechanic)!=null)Row(LevelInformation.Title(mechanic),LevelInformation.Description(mechanic),null);
            foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)
                Row(PowerUpTypeUtility.Name(type),PowerUpTypeUtility.Description(type),type);
            content.sizeDelta=new Vector2(0,y);content.anchoredPosition=Vector2.zero;scroller.verticalNormalizedPosition=1;
            var close=TelemetryPrivacyPanel.AddButton(panel,"Close","Tamam",Close);
            FruitLabStyle.Place((RectTransform)close.transform,panel,16,16,width-32,48);
            return popup;
        }
    }
}

using Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    internal static class GardenUi
    {
        public static RectTransform Rect(Transform parent,string name)
        {
            var existing=parent.Find(name) as RectTransform;
            if(existing!=null){existing.gameObject.layer=parent.gameObject.layer;return existing;}
            var item=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            item.SetParent(parent,false);item.gameObject.layer=parent.gameObject.layer;return item;
        }
        public static TMP_Text Text(Transform parent,string name,float size,Color color,bool normal=false)
        {
            var text=parent.Find(name)?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText(name,parent,size,color);
            FruitLabStyle.TextStyle(text,size,!normal);text.color=color;text.gameObject.layer=parent.gameObject.layer;return text;
        }
        public static Button Button(Transform parent,string name,string label,bool primary=false)
        {
            var rect=Rect(parent,name);
            var depth=FruitLabStyle.AddSurface(rect.gameObject,primary?FruitLabStyle.Mint*.65f:FruitLabStyle.Border);depth.raycastTarget=true;
            var face=FruitLabStyle.Detail(rect,"Face",primary?FruitLabStyle.Mint:FruitLabStyle.Tile);
            face.rectTransform.anchorMin=Vector2.zero;face.rectTransform.anchorMax=Vector2.one;
            face.rectTransform.offsetMin=new Vector2(1,3);face.rectTransform.offsetMax=new Vector2(-1,-1);
            var button=rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();button.targetGraphic=face;
            button.navigation=new Navigation {mode=Navigation.Mode.None};
            var colors=button.colors;colors.normalColor=colors.selectedColor=Color.white;colors.pressedColor=new Color(.78f,.85f,.85f);
            colors.disabledColor=new Color(.6f,.65f,.65f,.65f);colors.fadeDuration=.1f;button.colors=colors;
            if(label!=null)
            {
                var text=Text(face.transform,"Label",18,primary?FruitLabStyle.Background:FruitLabStyle.Text);
                text.text=label;FruitLabStyle.Stretch(text.rectTransform,3);
            }
            return button;
        }
        public static Image Icon(Transform parent,string name,string artwork)
        {
            var rect=Rect(parent,name);var image=rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            FruitLabStyle.SetIcon(image,artwork);image.raycastTarget=false;return image;
        }
        public static ScrollRect Scroll(Transform parent,string name)
        {
            var rect=Rect(parent,name);var scroll=rect.GetComponent<ScrollRect>() ?? rect.gameObject.AddComponent<ScrollRect>();
            var viewport=Rect(rect,"Viewport");FruitLabStyle.Stretch(viewport);
            var hit=viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            if(viewport.GetComponent<RectMask2D>()==null)viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect(viewport,"Content");content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
            scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;return scroll;
        }
        public static void Content(ScrollRect scroll,float height)
        {
            scroll.content.sizeDelta=new Vector2(0,height);scroll.content.ForceUpdateRectTransforms();
        }
    }
}

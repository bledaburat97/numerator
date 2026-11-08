using System;
using System.Collections.Generic;
using Scripts.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // Presentation only. The round's fruit award has already been saved.
    public sealed class FruitCollectionAwardView:MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image[] fruitIcons;
        private int _count;
        public int Count=>_count;
        public void EnsureUi()
        {
            if(label==null)label=transform.Find("Label")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("Label",transform,16,FruitLabStyle.Text);
            FruitLabStyle.TextStyle(label,16);label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;
            label.gameObject.layer=gameObject.layer;
            if(fruitIcons==null || fruitIcons.Length!=5)
            {
                fruitIcons=new Image[5];
                for(int i=0;i<fruitIcons.Length;i++)
                    fruitIcons[i]=transform.Find("Fruit"+i)?.GetComponent<Image>() ?? FruitLabStyle.Detail(transform,"Fruit"+i,Color.white);
            }
            foreach(var icon in fruitIcons){icon.raycastTarget=false;icon.gameObject.layer=gameObject.layer;}
        }
        public void Render(IReadOnlyList<int> fruits)
        {
            EnsureUi();_count=Mathf.Min(5,fruits?.Count??0);label.text="+"+_count+" meyve";
            for(int i=0;i<fruitIcons.Length;i++)
            {
                fruitIcons[i].gameObject.SetActive(i<_count);
                if(i<_count)FruitLabStyle.SetIcon(fruitIcons[i],((FruitId)fruits[i]).ToString());
            }
            ApplyLayout();
        }
        public void ApplyLayout()
        {
            EnsureUi();var rect=(RectTransform)transform;float width=rect.rect.width,height=rect.rect.height;
            float labelWidth=Mathf.Min(82,width*.34f),gap=4;
            FruitLabStyle.Place(label.rectTransform,transform,0,0,labelWidth,height);
            float side=Mathf.Max(1,Mathf.Min(28,Mathf.Min(height,(width-labelWidth-8-gap*Mathf.Max(0,_count-1))/Mathf.Max(1,_count))));
            for(int i=0;i<_count;i++)FruitLabStyle.Place(fruitIcons[i].rectTransform,transform,labelWidth+8+i*(side+gap),(height-side)*.5f,side,side);
        }
    }
}

using System;
using DG.Tweening;
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
        private FruitLabGlyph _collection;
        private Sequence _transfer;
        private readonly List<GameObject> _proxies=new List<GameObject>();
        public void PlayTransfer()
        {
            CancelTransfer();if(_count==0)return;
            var canvas=GetComponentInParent<Canvas>().rootCanvas;
            _transfer=DOTween.Sequence();
            for(int i=0;i<_count;i++)
            {
                var root=new GameObject("CollectedFruit",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));root.transform.SetParent(canvas.transform,false);root.layer=gameObject.layer;_proxies.Add(root);
                var image=root.GetComponent<Image>();image.sprite=fruitIcons[i].sprite;image.preserveAspect=true;image.raycastTarget=false;
                var rect=(RectTransform)root.transform;rect.position=fruitIcons[i].transform.position;rect.sizeDelta=Vector2.one*24;
                _transfer.Insert(i*.035f,DOTween.Sequence().Append(rect.DOMove(_collection.transform.position,.32f).SetEase(Ease.InOutCubic))
                    .Join(rect.DOSizeDelta(Vector2.one*10,.32f)).Join(image.DOFade(0,.32f)));
            }
            _transfer.Append(label.rectTransform.DOPunchScale(Vector3.one*.10f,.16f,3,.6f)).OnKill(ClearProxies).SetLink(gameObject,LinkBehaviour.KillOnDisable);
        }
        private void ClearProxies(){foreach(var item in _proxies)if(item!=null)TutorialObjectUtility.Destroy(item);_proxies.Clear();}
        private void CancelTransfer(){_transfer?.Kill();_transfer=null;ClearProxies();}
        private void OnDisable()=>CancelTransfer();
        public int Count=>_count;
        public void EnsureUi()
        {
            if(label==null)label=transform.Find("Label")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("Label",transform,16,FruitLabStyle.Text);
            FruitLabStyle.TextStyle(label,16);label.alignment=TextAlignmentOptions.MidlineLeft;label.raycastTarget=false;
            label.gameObject.layer=gameObject.layer;
            if(_collection==null)_collection=transform.Find("Collection")?.GetComponent<FruitLabGlyph>() ?? FruitLabGlyph.Create(transform,FruitLabGlyph.Symbol.Collection,FruitLabStyle.Mint);
            _collection.name="Collection";_collection.gameObject.layer=gameObject.layer;
            _collection.gameObject.SetActive(false);
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
            float labelWidth=Mathf.Min(108,width*.41f),gap=4;
            FruitLabStyle.Place(_collection.rectTransform,transform,0,(height-22)*.5f,22,22);
            FruitLabStyle.Place(label.rectTransform,transform,0,0,labelWidth,height);
            float side=Mathf.Max(1,Mathf.Min(28,Mathf.Min(height,(width-labelWidth-8-gap*Mathf.Max(0,_count-1))/Mathf.Max(1,_count))));
            for(int i=0;i<_count;i++)FruitLabStyle.Place(fruitIcons[i].rectTransform,transform,labelWidth+8+i*(side+gap),(height-side)*.5f,side,side);
        }
    }
}

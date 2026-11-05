using System;
using System.Collections.Generic;
using System.Globalization;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEngine;

namespace Menu
{
    [Serializable]
    public sealed class FruitCollectionCell
    {
        public FruitId Fruit;
        public RectTransform Root;
        public NonDraggableCardItemView Card;
        public TMP_Text Name;
        public TMP_Text Amount;
    }

    public interface IFruitCollectionView
    {
        IBaseButtonView CloseButton {get;}
        bool IsVisible {get;}
        void SetVisible(bool visible);
        void Render(IReadOnlyDictionary<FruitId,FruitBalance> fruits);
    }

    public sealed class FruitCollectionView : MonoBehaviour,IFruitCollectionView
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private TMP_Text title,summary,section;
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private FruitCollectionCell[] cells;
        private bool _initialized;
        private Vector2 _lastSize;
        private static readonly CultureInfo Turkish=CultureInfo.GetCultureInfo("tr-TR");
        public IBaseButtonView CloseButton=>closeButton;
        public bool IsVisible=>gameObject.activeSelf;
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if(!visible)return;
            transform.SetAsLastSibling();Initialize();ApplyLayout();
        }
        private void Initialize()
        {
            if(_initialized)return;
            foreach(var cell in cells)
            {
                cell.Card.Init((int)cell.Fruit);cell.Name.text=FruitName(cell.Fruit);
                foreach(var graphic in cell.Card.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
            }
            _initialized=true;
        }
        public void Render(IReadOnlyDictionary<FruitId,FruitBalance> fruits)
        {
            Initialize();decimal total=0;
            foreach(var cell in cells)
            {
                long amount=fruits[cell.Fruit].Current;total+=amount;
                cell.Amount.text=FormatCount(amount);cell.Card.SetAlpha(amount>0?1f:.5f);
            }
            summary.text="Toplam "+FormatCount(total)+" meyve";
        }
        private void LateUpdate() { if((rect.rect.size-_lastSize).sqrMagnitude>.01f)ApplyLayout(); }
        public void ApplyLayout()
        {
            _lastSize=rect.rect.size;float width=rect.rect.width,height=rect.rect.height;
            FruitLabStyle.Place(title.rectTransform,rect,20,height-62,width-40,32);
            FruitLabStyle.Place(section.rectTransform,rect,20,height-96,width-40,24);
            FruitLabStyle.Place(summary.rectTransform,rect,20,height-128,width-40,24);
            float cellWidth=(width-56f)/3f;
            float cellHeight=Mathf.Min(cellWidth+44f,(height-244f)/3f);
            float top=height-148f;
            for(int index=0;index<cells.Length;index++)
            {
                var cell=cells[index];
                FruitLabStyle.Place(cell.Root,rect,20+(index%3)*(cellWidth+8),top-(index/3+1)*cellHeight-(index/3)*8,cellWidth,cellHeight);
                float cardSize=Mathf.Min(cellWidth-20,cellHeight-66);
                FruitLabStyle.Place((RectTransform)cell.Card.transform,cell.Root,(cellWidth-cardSize)*.5f,cellHeight-cardSize-10,cardSize,cardSize);
                FruitLabStyle.Place(cell.Name.rectTransform,cell.Root,4,31,cellWidth-8,30);
                FruitLabStyle.Place(cell.Amount.rectTransform,cell.Root,4,6,cellWidth-8,25);
            }
            FruitLabStyle.Place(closeButton.GetButtonRectTransform(),rect,20,18,width-40,46);
        }
        private static string FormatCount(decimal count)
        {
            if(count<1000000m)return count.ToString("N0",Turkish);
            foreach(var unit in new[]{(1000000000000000000m," Kent."),(1000000000000000m," Katr."),(1000000000000m," Tr."),(1000000000m," Mr."),(1000000m," Mn")})
                if(count>=unit.Item1)return (count/unit.Item1).ToString("0.#",Turkish)+unit.Item2;
            return count.ToString("N0",Turkish);
        }
        public static string FruitName(FruitId fruit)
        {
            switch(fruit)
            {
                case FruitId.Apple:return "Elma";case FruitId.Banana:return "Muz";case FruitId.Strawberry:return "Çilek";
                case FruitId.Orange:return "Portakal";case FruitId.Grape:return "Üzüm";case FruitId.Blueberry:return "Yaban mersini";
                case FruitId.Pear:return "Armut";case FruitId.Watermelon:return "Karpuz";case FruitId.Kiwi:return "Kivi";
                default:throw new ArgumentOutOfRangeException(nameof(fruit));
            }
        }
    }
}

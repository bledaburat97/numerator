using System;
using Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    // Inventory is rendered by the existing collection presenter. This modal only
    // owns the nine cell visuals and closing behavior; it never changes a tab.
    public sealed class FruitInventoryPopupView : MonoBehaviour
    {
        [NonSerialized] private RectTransform _panel;
        private ScrollRect _grid;
        private FruitCollectionCell[] _cells;
        private Button _close;
        private Vector2 _size;
        public bool IsVisible => gameObject.activeSelf;
        public Button CloseButton => _close;
        public void Configure(FruitCollectionCell[] cells)
        {
            _cells=cells;
            if(_panel==null)
            {
                var dim=GetComponent<Image>() ?? gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.65f);dim.raycastTarget=true;
                var outside=GetComponent<Button>() ?? gameObject.AddComponent<Button>();outside.transition=Selectable.Transition.None;
                outside.onClick.RemoveAllListeners();outside.onClick.AddListener(()=>SetVisible(false));
                _panel=GardenUi.Rect(transform,"Panel");PopupArtStyle.Apply(_panel.gameObject);
                if(_panel.GetComponent<Button>()==null)_panel.gameObject.AddComponent<Button>().transition=Selectable.Transition.None;
                GardenUi.Text(_panel,"Title",24,PopupArtStyle.Ink).text="Koleksiyon";
                _grid=GardenUi.Scroll(_panel,"Fruits");
                _close=GardenUi.Button(_panel,"Close","Kapat");_close.onClick.RemoveAllListeners();_close.onClick.AddListener(()=>SetVisible(false));
            }
            foreach(var cell in cells){cell.Root.SetParent(_grid.content,false);cell.Root.gameObject.SetActive(true);cell.Name.gameObject.SetActive(false);}
            ApplyLayout();
        }
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if(visible){transform.SetAsLastSibling();ApplyLayout();_grid.verticalNormalizedPosition=1;}
        }
        public void ApplyLayout()
        {
            if(_panel==null)return;
            var root=(RectTransform)transform;_size=root.rect.size;
            float width=Mathf.Min(350,_size.x-24),height=Mathf.Min(436,_size.y-16);
            FruitLabStyle.Place(_panel,root,(_size.x-width)*.5f,_size.y-height-8,width,height);
            FruitLabStyle.Place((RectTransform)_panel.Find("Title"),_panel,16,height-48,width-32,32);
            PopupArtStyle.Apply(_panel.gameObject);
            FruitLabStyle.Place((RectTransform)_grid.transform,_panel,26,78,width-52,Mathf.Max(1,height-142));
            float cellWidth=(width-68)/3,cellHeight=Mathf.Clamp((height-158)/3,80,94);
            GardenUi.Content(_grid,3*(cellHeight+8)-8);
            for(int i=0;i<_cells.Length;i++)
            {
                var cell=_cells[i];cell.Root.anchorMin=cell.Root.anchorMax=new Vector2(0,1);cell.Root.pivot=new Vector2(0,1);
                cell.Root.anchoredPosition=new Vector2((i%3)*(cellWidth+8),-(i/3)*(cellHeight+8));cell.Root.sizeDelta=new Vector2(cellWidth,cellHeight);
                float side=Mathf.Min(60,Mathf.Min(cellWidth-12,cellHeight-34));
                FruitLabStyle.Place((RectTransform)cell.Card.transform,cell.Root,(cellWidth-side)*.5f,cellHeight-side-4,side,side);
                FruitLabStyle.Place(cell.Amount.rectTransform,cell.Root,2,3,cellWidth-4,25);GardenUi.Quantity(cell.Amount,22);cell.Amount.color=PopupArtStyle.Ink;
                FruitLabStyle.Surface(cell.Root.GetComponent<Image>(),new Color(.42f,.50f,.26f,.10f));
            }
            FruitLabStyle.Place((RectTransform)_close.transform,_panel,28,26,width-56,44);
        }
        private void LateUpdate(){if(((RectTransform)transform).rect.size!=_size)ApplyLayout();}
    }
}

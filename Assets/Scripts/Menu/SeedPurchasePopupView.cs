using System;
using System.Collections.Generic;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public interface ISeedPurchasePopupView
    {
        event Action<FruitId> FruitSelected;
        event Action<string> QuantityChanged;
        event Action<int> AdjustQuantity;
        event Action Confirm;
        event Action Cancel;
        void SetVisible(bool visible);
        void SetSelection(FruitId fruit,string quantity);
        void RenderQuote(SeedPurchaseQuote quote,string error,bool busy);
    }

    public sealed class SeedPurchasePopupView : MonoBehaviour,ISeedPurchasePopupView
    {
        public event Action<FruitId> FruitSelected;
        public event Action<string> QuantityChanged;
        public event Action<int> AdjustQuantity;
        public event Action Confirm;
        public event Action Cancel;
        private RectTransform _panel;
        private ScrollRect _body;
        private TMP_Text _balance,_selected,_total,_remaining,_error;
        private Button _confirm,_cancel,_minus,_plus;
        private TMP_InputField _quantity;
        private readonly Dictionary<FruitId,Button> _fruits=new Dictionary<FruitId,Button>();
        private Vector2 _lastSize;
        public TMP_InputField QuantityInput {get {EnsureUi();return _quantity;}}
        public Button ConfirmButton {get {EnsureUi();return _confirm;}}
        public Button CancelButton {get {EnsureUi();return _cancel;}}
        public Button FruitButton(FruitId fruit) {EnsureUi();return _fruits[fruit];}
        public void EnsureUi()
        {
            if(_panel!=null)return;
            var backdrop=GetComponent<Image>() ?? gameObject.AddComponent<Image>();backdrop.color=new Color(0,0,0,.65f);backdrop.raycastTarget=true;
            var outside=GetComponent<Button>() ?? gameObject.AddComponent<Button>();outside.transition=Selectable.Transition.None;outside.onClick.AddListener(()=>Cancel?.Invoke());
            _panel=GardenUi.Rect(transform,"Panel");FruitLabStyle.AddSurface(_panel.gameObject,FruitLabStyle.Panel).raycastTarget=true;
            // Consume taps on panel whitespace rather than dismissing through it.
            if(_panel.GetComponent<Button>()==null)_panel.gameObject.AddComponent<Button>().transition=Selectable.Transition.None;
            var title=GardenUi.Text(_panel,"Title",24,FruitLabStyle.Text);title.text="Tohum al";
            _balance=GardenUi.Text(_panel,"Balance",18,FruitLabStyle.Gold);
            _body=GardenUi.Scroll(_panel,"Options");
            foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))
            {
                var button=GardenUi.Button(_body.content,fruit.ToString(),null);var selected=fruit;
                button.onClick.AddListener(()=>FruitSelected?.Invoke(selected));_fruits.Add(fruit,button);
                GardenUi.Icon(button.transform,"Fruit",fruit.ToString());
                var rim=FruitLabStyle.Detail(button.transform,"Selected",FruitLabStyle.Mint,true);rim.gameObject.SetActive(false);
            }
            _selected=GardenUi.Text(_body.content,"SelectedFruit",18,FruitLabStyle.Text);
            _minus=GardenUi.Button(_body.content,"Minus","−");_minus.onClick.AddListener(()=>AdjustQuantity?.Invoke(-1));
            _plus=GardenUi.Button(_body.content,"Plus","+");_plus.onClick.AddListener(()=>AdjustQuantity?.Invoke(1));
            var inputRect=GardenUi.Rect(_body.content,"Quantity");FruitLabStyle.AddSurface(inputRect.gameObject,FruitLabStyle.Recess).raycastTarget=true;
            _quantity=inputRect.GetComponent<TMP_InputField>() ?? inputRect.gameObject.AddComponent<TMP_InputField>();
            var viewport=GardenUi.Rect(inputRect,"Viewport");FruitLabStyle.Stretch(viewport,4);
            if(viewport.GetComponent<RectMask2D>()==null)viewport.gameObject.AddComponent<RectMask2D>();
            var number=GardenUi.Text(viewport,"Text",22,FruitLabStyle.Text);FruitLabStyle.Stretch(number.rectTransform);
            var placeholder=GardenUi.Text(viewport,"Placeholder",18,FruitLabStyle.Muted,true);placeholder.text="Adet";FruitLabStyle.Stretch(placeholder.rectTransform);
            _quantity.textViewport=viewport;_quantity.textComponent=number;_quantity.placeholder=placeholder;
            _quantity.contentType=TMP_InputField.ContentType.IntegerNumber;_quantity.keyboardType=TouchScreenKeyboardType.NumberPad;
            _quantity.characterLimit=19;_quantity.lineType=TMP_InputField.LineType.SingleLine;
            _quantity.onValueChanged.AddListener(value=>QuantityChanged?.Invoke(value));
            _total=GardenUi.Text(_body.content,"Total",18,FruitLabStyle.Text);
            _remaining=GardenUi.Text(_body.content,"Remaining",16,FruitLabStyle.Muted,true);
            _error=GardenUi.Text(_panel,"Error",16,FruitLabStyle.OpponentPlayer,true);_error.textWrappingMode=TextWrappingModes.Normal;
            _confirm=GardenUi.Button(_panel,"Confirm","Satın al",true);_confirm.onClick.AddListener(()=>Confirm?.Invoke());
            _cancel=GardenUi.Button(_panel,"Cancel","Vazgeç");_cancel.onClick.AddListener(()=>Cancel?.Invoke());
        }
        public void SetVisible(bool visible)
        {
            if(this==null)return;
            EnsureUi();if(!visible)_quantity.DeactivateInputField();gameObject.SetActive(visible);
            if(visible){transform.SetAsLastSibling();ApplyLayout();_body.verticalNormalizedPosition=1;}
        }
        public void SetSelection(FruitId fruit,string quantity)
        {
            EnsureUi();_selected.text=FruitCollectionView.FruitName(fruit)+" tohumu";
            _quantity.SetTextWithoutNotify(quantity);
            foreach(var item in _fruits)item.Value.transform.Find("Selected").gameObject.SetActive(item.Key==fruit);
        }
        public void RenderQuote(SeedPurchaseQuote quote,string error,bool busy)
        {
            EnsureUi();_balance.text=quote==null?"":FruitCollectionView.FormatCount(quote.AvailableCoins)+" coin";
            _total.text=quote==null?"Toplam: —":"Toplam: "+FruitCollectionView.FormatCount(quote.TotalCoinPrice)+" coin";
            _remaining.text=quote==null?"":quote.CanAfford?"Kalan: "+FruitCollectionView.FormatCount(quote.AvailableCoins-quote.TotalCoinPrice)+" coin":"Bakiye yetersiz";
            _error.text=error??"";_confirm.interactable=!busy && quote!=null && quote.CanAfford;
            _cancel.interactable=_minus.interactable=_plus.interactable=_quantity.interactable=!busy;
            foreach(var fruit in _fruits.Values)fruit.interactable=!busy;
        }
        private void LateUpdate(){if(((RectTransform)transform).rect.size!=_lastSize)ApplyLayout();}
        public void ApplyLayout()
        {
            EnsureUi();var rect=(RectTransform)transform;_lastSize=rect.rect.size;
            float width=Mathf.Min(350,rect.rect.width-24),height=Mathf.Min(580,rect.rect.height-24);
            _panel.anchorMin=_panel.anchorMax=_panel.pivot=Vector2.one*.5f;_panel.anchoredPosition=Vector2.zero;_panel.sizeDelta=new Vector2(width,height);
            FruitLabStyle.Place((RectTransform)_panel.Find("Title"),_panel,16,height-50,width-32,32);
            FruitLabStyle.Place(_balance.rectTransform,_panel,16,height-80,width-32,24);
            FruitLabStyle.Place((RectTransform)_body.transform,_panel,16,154,width-32,Mathf.Max(1,height-244));
            GardenUi.Content(_body,300);
            float bodyWidth=width-32,buttonWidth=(bodyWidth-12)/3;
            int index=0;
            foreach(var item in _fruits)
            {
                FruitLabStyle.Place((RectTransform)item.Value.transform,_body.content,(index%3)*(buttonWidth+6),300-48-(index/3)*54,buttonWidth,48);
                var icon=(RectTransform)item.Value.transform.Find("Fruit");FruitLabStyle.Place(icon,item.Value.transform,(buttonWidth-42)*.5f,3,42,42);
                FruitLabStyle.Stretch((RectTransform)item.Value.transform.Find("Selected"),1);index++;
            }
            FruitLabStyle.Place(_selected.rectTransform,_body.content,0,114,bodyWidth,24);
            FruitLabStyle.Place((RectTransform)_minus.transform,_body.content,0,56,48,48);
            FruitLabStyle.Place(_quantity.GetComponent<RectTransform>(),_body.content,56,56,bodyWidth-112,48);
            FruitLabStyle.Place((RectTransform)_plus.transform,_body.content,bodyWidth-48,56,48,48);
            FruitLabStyle.Place(_total.rectTransform,_body.content,0,26,bodyWidth,24);
            FruitLabStyle.Place(_remaining.rectTransform,_body.content,0,0,bodyWidth,22);
            FruitLabStyle.Place(_error.rectTransform,_panel,16,116,width-32,30);
            FruitLabStyle.Place((RectTransform)_confirm.transform,_panel,16,66,width-32,48);
            FruitLabStyle.Place((RectTransform)_cancel.transform,_panel,16,12,width-32,44);
        }
    }
}

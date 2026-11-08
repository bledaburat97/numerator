using System;
using System.Globalization;
using System.IO;
using Scripts.Economy;
using UnityEngine;

namespace Menu
{
    public interface IFruitCollectionPresenter { void Open();void Close();void ShowPendingNotice(); }

    public sealed class FruitCollectionPresenter : IFruitCollectionPresenter,IDisposable
    {
        private readonly IFruitInventoryService _inventory;
        private readonly IPlayerProfileRepository _profiles;
        private readonly IFruitCollectionView _view;
        private readonly IGardenService _garden;
        private readonly GardenVisitCoordinator _visits;
        private readonly IGardenPanelView _gardenView;
        private readonly ISeedPurchasePopupView _purchase;
        private readonly IGardenNoticeView _notice;
        private bool _disposed,_purchaseOpen,_busy,_menuReady;
        private FruitId _fruit=FruitId.Apple;
        private string _quantity="1",_purchaseId,_error;

        public FruitCollectionPresenter(IFruitInventoryService inventory,IPlayerProfileRepository profiles,
            IFruitCollectionView view,Scripts.BaseButtonControllerFactory buttons,IGardenService garden,
            GardenVisitCoordinator visits,IGardenNoticeView notice)
        {
            _inventory=inventory;_profiles=profiles;_view=view;_garden=garden;_visits=visits;_notice=notice;
            var close=buttons.Create(view.CloseButton,Close);close.SetClickOnRelease(true);
            _gardenView=view.Garden;_purchase=view.Purchase;
            _gardenView.BuySeeds+=OpenPurchase;_purchase.FruitSelected+=SelectFruit;_purchase.QuantityChanged+=ChangeQuantity;
            _purchase.AdjustQuantity+=AdjustQuantity;_purchase.Confirm+=Purchase;_purchase.Cancel+=CancelPurchase;
            _view.TabChanged+=TabChanged;_profiles.Changed+=Refresh;_visits.NoticeAvailable+=OnNoticeAvailable;
            _view.SetVisible(false);_notice.Hide();
        }
        public void Open()
        {
            if(_disposed)return;
            _notice.Hide();_view.SetVisible(true);_view.SelectTab(GardenTab.Garden);Refresh();
        }
        public void Close()
        {
            if(_disposed || _busy)return;
            CancelPurchase();_view.SetVisible(false);FlushNotice();
        }
        public void ShowPendingNotice(){_menuReady=true;FlushNotice();}
        private void OnNoticeAvailable(){if(_menuReady)FlushNotice();}
        private void FlushNotice()
        {
            if(_disposed || !_menuReady || _view.IsVisible)return;
            var pending=_visits.TakeNotice();if(pending!=null)_notice.ShowProgress(pending);
        }
        private void TabChanged(GardenTab tab)=>Refresh();
        private void Refresh()
        {
            if(_disposed || !_view.IsVisible)return;
            _view.Render(_inventory.GetCollection());_gardenView.Render(_garden.GetBatches());
            if(_purchaseOpen)RefreshQuote();
        }
        private void OpenPurchase()
        {
            if(_disposed || !_view.IsVisible || _busy)return;
            _notice.Hide();_purchaseOpen=true;_fruit=FruitId.Apple;_quantity="1";NewIntent();
            _purchase.SetVisible(true);_purchase.SetSelection(_fruit,_quantity);RefreshQuote();
        }
        private void CancelPurchase()
        {
            if(_busy)return;
            _purchaseOpen=false;_purchase.SetVisible(false);
        }
        private void NewIntent(){_purchaseId=Guid.NewGuid().ToString("N");_error=null;}
        private void SelectFruit(FruitId fruit)
        {
            if(!_purchaseOpen || _busy)return;
            _fruit=fruit;NewIntent();_purchase.SetSelection(_fruit,_quantity);RefreshQuote();
        }
        private void ChangeQuantity(string value)
        {
            if(!_purchaseOpen || _busy)return;
            _quantity=value;NewIntent();RefreshQuote();
        }
        private void AdjustQuantity(int delta)
        {
            if(!_purchaseOpen || _busy)return;
            if(!TryQuantity(out long quantity))quantity=1;
            if(delta>0 && quantity<long.MaxValue)quantity++;
            if(delta<0 && quantity>1)quantity--;
            _quantity=quantity.ToString(CultureInfo.InvariantCulture);NewIntent();
            _purchase.SetSelection(_fruit,_quantity);RefreshQuote();
        }
        private bool TryQuantity(out long quantity)=>long.TryParse(_quantity,NumberStyles.None,CultureInfo.InvariantCulture,out quantity) && quantity>0;
        private void RefreshQuote()
        {
            if(!TryQuantity(out long quantity)){_purchase.RenderQuote(null,"En az 1 tohum seç.",_busy);return;}
            try {_purchase.RenderQuote(_garden.QuoteSeeds(_fruit,quantity),_error,_busy);}
            catch(ArgumentOutOfRangeException){_purchase.RenderQuote(null,"Adet çok yüksek.",_busy);}
        }
        private void Purchase()
        {
            if(_disposed || !_purchaseOpen || _busy || !TryQuantity(out long quantity))return;
            _busy=true;_error=null;
            try
            {
                RefreshQuote();var result=_garden.PurchaseSeeds(_purchaseId,_fruit,quantity);
                if(result.Status==ProfileCommitResult.Rejected){_error="Bakiyen yetersiz.";return;}
                _purchaseOpen=false;_purchase.SetVisible(false);_view.SelectTab(GardenTab.Garden);Refresh();
                _notice.ShowPurchase(result.Fruit,result.Quantity);
            }
            catch(Exception error) when(error is IOException || error is InvalidOperationException || error is ArgumentOutOfRangeException || error is OverflowException)
            {
                _error="Kaydedilemedi. Tekrar dene.";
                Debug.LogWarning("Seed purchase deferred: "+error.GetType().Name);
            }
            finally {_busy=false;if(_purchaseOpen)RefreshQuote();}
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            _profiles.Changed-=Refresh;_visits.NoticeAvailable-=OnNoticeAvailable;_view.TabChanged-=TabChanged;
            _gardenView.BuySeeds-=OpenPurchase;_purchase.FruitSelected-=SelectFruit;_purchase.QuantityChanged-=ChangeQuantity;
            _purchase.AdjustQuantity-=AdjustQuantity;_purchase.Confirm-=Purchase;_purchase.Cancel-=CancelPurchase;
            _purchase.SetVisible(false);_notice.Hide();
        }
    }
}

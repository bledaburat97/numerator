using System;
using System.Globalization;
using System.IO;
using Scripts.Economy;
using UnityEngine;

namespace Menu
{
    public interface IFruitCollectionPresenter { void Open();void Open(GardenTab tab);void Close();void ShowPendingNotice();void FocusBadge(string id); }

    public sealed class FruitCollectionPresenter : IFruitCollectionPresenter,IDisposable
    {
        private readonly IAchievementService _achievements;
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
            GardenVisitCoordinator visits,IGardenNoticeView notice,IAchievementService achievements)
        {
            _achievements=achievements;_inventory=inventory;_profiles=profiles;_view=view;_garden=garden;_visits=visits;_notice=notice;
            var close=buttons.Create(view.CloseButton,Close);close.SetClickOnRelease(true);
            _gardenView=view.Garden;_purchase=view.Purchase;
            _gardenView.BuySeeds+=OpenPurchase;_purchase.FruitSelected+=SelectFruit;_purchase.QuantityChanged+=ChangeQuantity;
            _gardenView.HarvestRequested+=Harvest;
            _purchase.AdjustQuantity+=AdjustQuantity;_purchase.Confirm+=Purchase;_purchase.Cancel+=CancelPurchase;
            _view.TabChanged+=TabChanged;_profiles.Updated+=OnProfileChanged;_visits.NoticeAvailable+=OnNoticeAvailable;
            _view.SetVisible(false);_notice.Hide();
        }
        public void Open() => Open(GardenTab.Garden);
        public void Open(GardenTab tab)
        {
            if(_disposed || _busy)return;
            CancelPurchase();_notice.Hide();
            try{_achievements.Reconcile();}
            catch(Exception error) when(error is IOException || error is InvalidOperationException || error is OverflowException)
            { Debug.LogWarning("Achievement reconciliation deferred: "+error.GetType().Name); }
            _view.SetVisible(true);_view.SelectTab(tab);Refresh();
            if(tab==GardenTab.Collection)_view.FocusBadge(null);
        }
        public void FocusBadge(string id){if(!_disposed && _view.IsVisible)_view.FocusBadge(id);}
        public void Close()
        {
            if(_disposed || _busy)return;
            CancelPurchase();_view.SetVisible(false);FlushNotice();
        }
        public void ShowPendingNotice(){_menuReady=true;FlushNotice();}
        private void OnNoticeAvailable(){if(_menuReady)FlushNotice();Refresh();}
        private void FlushNotice()
        {
            if(_disposed || !_menuReady || _view.IsVisible)return;
            var pending=_visits.TakeNotice();if(pending!=null)_notice.ShowProgress(pending);
        }
        private void TabChanged(GardenTab tab)=>Refresh();
        private void OnProfileChanged(ProfileChange change)
        {
            if(_disposed || !_view.IsVisible)return;
            if(change.Includes(ProfileArea.Collection))_view.Render(_inventory.GetCollection());
            if(change.Includes(ProfileArea.Collection|ProfileArea.Achievements))_view.RenderAchievements(_achievements.GetAchievements());
            if(change.Includes(ProfileArea.Garden))_gardenView.Render(_garden.GetBatches());
            if(change.Includes(ProfileArea.Coins))_gardenView.SetCoins(_profiles.Snapshot().Wallet.Coins);
            if(_purchaseOpen && change.Includes(ProfileArea.Coins|ProfileArea.Garden))RefreshQuote();
        }
        private void Refresh()
        {
            if(_disposed || !_view.IsVisible)return;
            _view.Render(_inventory.GetCollection());_view.RenderAchievements(_achievements.GetAchievements());_gardenView.Render(_garden.GetBatches());
            _gardenView.SetCoins(_profiles.Snapshot().Wallet.Coins);
            if(_gardenView.IsVisible)_gardenView.PlayGrowth(_visits.TakeVisualChanges());
            if(_purchaseOpen)RefreshQuote();
        }
        private void OpenPurchase()
        {
            if(_disposed || !_view.IsVisible || _busy)return;
            _notice.Hide();_purchaseOpen=true;_fruit=FruitId.Apple;_quantity=_garden.SeedsPerPack.ToString(CultureInfo.InvariantCulture);NewIntent();
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
            long step=_garden.SeedsPerPack;
            if(!TryQuantity(out long quantity))quantity=step;
            if(delta>0)
            {if(quantity/step>=long.MaxValue/step)return;quantity=(quantity/step+1)*step;}
            if(delta<0)quantity=Math.Max(step,(quantity-1)/step*step);
            _quantity=quantity.ToString(CultureInfo.InvariantCulture);NewIntent();
            _purchase.SetSelection(_fruit,_quantity);RefreshQuote();
        }
        private bool TryQuantity(out long quantity)=>long.TryParse(_quantity,NumberStyles.None,CultureInfo.InvariantCulture,out quantity) && quantity>0;
        private void RefreshQuote()
        {
            if(!TryQuantity(out long quantity) || quantity%_garden.SeedsPerPack!=0)
            {_purchase.RenderQuote(null,_garden.SeedsPerPack+"’in katlarını seç.",_busy);return;}
            try {var quote=_garden.QuoteSeeds(_fruit,quantity);_purchase.RenderQuote(quote,_error ?? PurchaseError(quote.Failure),_busy);}
            catch(ArgumentOutOfRangeException){_purchase.RenderQuote(null,"Adet çok yüksek.",_busy);}
        }
        private void Purchase()
        {
            if(_disposed || !_purchaseOpen || _busy || !TryQuantity(out long quantity))return;
            _busy=true;_error=null;
            try
            {
                RefreshQuote();var result=_garden.PurchaseSeeds(_purchaseId,_fruit,quantity);
                if(result.Status==ProfileCommitResult.Rejected){_error=PurchaseError(result.Failure);return;}
                _purchaseOpen=false;_purchase.SetVisible(false);_view.SelectTab(GardenTab.Garden);Refresh();
                _gardenView.FocusFruit(result.Fruit);
                if(result.Status==ProfileCommitResult.Applied)_gardenView.PlayPlanting(result.BatchId);
                _notice.ShowPurchase(result.Fruit,result.Quantity);
            }
            catch(Exception error) when(error is IOException || error is InvalidOperationException || error is ArgumentOutOfRangeException || error is OverflowException)
            {
                _error="Kaydedilemedi. Tekrar dene.";
                Debug.LogWarning("Seed purchase deferred: "+error.GetType().Name);
            }
            finally {_busy=false;if(_purchaseOpen)RefreshQuote();}
        }
        private string PurchaseError(SeedPurchaseFailure failure)
        {
            switch(failure)
            {
                case SeedPurchaseFailure.PlotFull:return "Parsel dolu. Olgun meyveleri topla.";
                case SeedPurchaseFailure.InvalidQuantity:return _garden.SeedsPerPack+"’in katlarını seç.";
                case SeedPurchaseFailure.InsufficientCoins:return "Bakiyen yetersiz.";
                case SeedPurchaseFailure.QuantityTooLarge:return "Adet çok yüksek.";
                default:return null;
            }
        }
        private void Harvest(string batchId)
        {
            if(_disposed || _busy || _purchaseOpen || !_view.IsVisible)return;
            _busy=true;
            var visual=_gardenView.CaptureHarvest(batchId);
            try
            {
                var result=_garden.Harvest(batchId);
                if(result.Status==ProfileCommitResult.Applied){_gardenView.PlayHarvest(visual);_notice.ShowHarvest(result.Fruit,result.Quantity);}
                else Refresh();
            }
            catch(Exception error) when(error is IOException || error is InvalidOperationException || error is OverflowException)
            {
                _notice.ShowError("Toplanamadı. Yeniden dokun.");
                Debug.LogWarning("Garden harvest deferred: "+error.GetType().Name);
            }
            finally {_busy=false;}
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            _profiles.Updated-=OnProfileChanged;_visits.NoticeAvailable-=OnNoticeAvailable;_view.TabChanged-=TabChanged;
            _gardenView.BuySeeds-=OpenPurchase;_purchase.FruitSelected-=SelectFruit;_purchase.QuantityChanged-=ChangeQuantity;
            _gardenView.HarvestRequested-=Harvest;
            _purchase.AdjustQuantity-=AdjustQuantity;_purchase.Confirm-=Purchase;_purchase.Cancel-=CancelPurchase;
            _purchase.SetVisible(false);_notice.Hide();
        }
    }
}

using System;
using Scripts.Economy;

namespace Menu
{
    public interface IFruitCollectionPresenter { void Open();void Close(); }

    public sealed class FruitCollectionPresenter : IFruitCollectionPresenter,IDisposable
    {
        private readonly IFruitInventoryService _inventory;
        private readonly IPlayerProfileRepository _profiles;
        private readonly IFruitCollectionView _view;
        private bool _disposed;
        public FruitCollectionPresenter(IFruitInventoryService inventory,IPlayerProfileRepository profiles,
            IFruitCollectionView view,Scripts.BaseButtonControllerFactory buttons)
        {
            _inventory=inventory;_profiles=profiles;_view=view;
            var close=buttons.Create(view.CloseButton,Close);close.SetClickOnRelease(true);
            _profiles.Changed+=Refresh;_view.SetVisible(false);
        }
        public void Open()
        {
            if(_disposed)return;
            _view.SetVisible(true);Refresh();
        }
        public void Close()=>_view.SetVisible(false);
        private void Refresh() { if(!_disposed && _view.IsVisible)_view.Render(_inventory.GetCollection()); }
        public void Dispose() { if(_disposed)return;_disposed=true;_profiles.Changed-=Refresh; }
    }
}

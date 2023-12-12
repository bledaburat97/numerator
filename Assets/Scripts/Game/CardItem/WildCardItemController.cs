using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class WildCardItemController : DraggableCardItemController, IWildCardItemController
    {
        private IWildCardItemView _view;
        private Func<int, LockedCardInfo> _onDragComplete;
        private Func<int, int, RectTransform> _afterDragComplete;
        private Action<LockedCardInfo> _setLockedCard;
        private Action _slideCardHolders;
        private Action _backSlideCardHolders;
        private bool _isDragStart;
        private Camera _cam;
        
        public void Initialize(IWildCardItemView view, CardItemData data, ICardItemLocator cardItemLocator, Action<LockedCardInfo> setLockedCard, Action slideCardHolders, Action backSlideCardHolders, Camera cam)
        {
            _view = view;
            _cam = cam;
            _cardItemData = data;
            _view.SetLocalPositionGap(data.cardItemIndex);
            _view.InitLocalScale();
            _view.SetLocalPosition(Vector3.zero, 0f);
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            _view.SetOnPointerDown(OnPointerDown);
            _view.SetSize(data.parent.sizeDelta);
            SetOnDragContinue(cardItemLocator.OnDragContinue);
            SetOnDragComplete(cardItemLocator.OnWildDragComplete);
            SetGetLockedCard(setLockedCard);
            _slideCardHolders += slideCardHolders;
            _backSlideCardHolders += backSlideCardHolders;
        }

        private void SetOnDragComplete(Func<int, LockedCardInfo> func)
        {
            _onDragComplete += func;
        }

        private void SetGetLockedCard(Action<LockedCardInfo> func)
        {
            _setLockedCard = func;
        }

        private void OnDrag(PointerEventData data)
        {
            _view.SetParent(_cardItemData.tempParent);
            _view.SetSize(new Vector2(ConstantValues.BOARD_CARD_HOLDER_WIDTH, ConstantValues.BOARD_CARD_HOLDER_HEIGHT));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_view.GetParent(), data.position,
                _cam, out Vector2 localPosition);
            _view.SetAnchoredPosition(localPosition);
            _onDragContinue(data.position, _cardItemData.cardItemIndex);
            if (!_isDragStart && _cardItemData.cardItemIndex == 0)
            {
                _slideCardHolders.Invoke();
            }

            _isDragStart = true;
        }

        private void OnPointerUp(PointerEventData data)
        {
            LockedCardInfo lockedCardInfo = _onDragComplete(_cardItemData.cardItemIndex);
            
            if (lockedCardInfo != null)
            {
               _setLockedCard.Invoke(lockedCardInfo);
               _view.Destroy();
            }
            else
            {
                if (_cardItemData.cardItemIndex == 0) _backSlideCardHolders.Invoke();
                _view.SetParent(_cardItemData.parent);
                _view.InitLocalScale();
                _view.SetLocalPosition(Vector3.zero, 0.3f);
                _view.SetSize(_cardItemData.parent.sizeDelta);
            }
        }

        private void OnPointerDown(PointerEventData data)
        {
            _isDragStart = false;
        }

    }

    public interface IWildCardItemController
    {
        void Initialize(IWildCardItemView view, CardItemData data, ICardItemLocator cardItemLocator, Action<LockedCardInfo> getLockedCard, Action slideCardHolders, Action backSlideCardHolders, Camera cam);
    }
}
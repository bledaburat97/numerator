using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class WildCardItemController : IWildCardItemController
    {
        private IWildCardItemView _view;
        private CardItemData _cardItemData;
        private Action<int> _onDragStart;
        private Action<Vector2, int> _onDragContinue;
        private Func<int, LockedCardInfo> _onDragComplete;
        private Func<int, int, RectTransform> _afterDragComplete;
        private Action<LockedCardInfo> _setLockedCard;
        
        public void Initialize(IWildCardItemView view, CardItemData data, ICardItemLocator cardItemLocator, Action<LockedCardInfo> setLockedCard)
        {
            _view = view;
            _cardItemData = data;
            _view.InitPosition();
            _view.SetOnDrag(OnDrag);
            _view.SetOnPointerUp(OnPointerUp);
            SetOnDragContinue(cardItemLocator.OnDragContinue);
            SetOnDragComplete(cardItemLocator.OnWildDragComplete);
            SetGetLockedCard(setLockedCard);
        }

        private void SetOnDragContinue(Action<Vector2, int> action)
        {
            _onDragContinue += action;
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
                null, out Vector2 localPosition);
            _view.SetAnchoredPosition(localPosition);
            _onDragContinue(data.position, _cardItemData.cardItemIndex);
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
                _view.SetParent(_cardItemData.parent);
                _view.InitPosition();
                _view.SetSize(_cardItemData.parent.sizeDelta);
            }
        }

    }

    public interface IWildCardItemController
    {
        void Initialize(IWildCardItemView view, CardItemData data, ICardItemLocator cardItemLocator, Action<LockedCardInfo> getLockedCard);
    }
}
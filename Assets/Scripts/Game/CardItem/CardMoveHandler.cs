using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
public class CardMoveHandler : ICardMoveHandler
{
    private readonly IHapticController _hapticController;
    private readonly ITutorialAbilityManager _tutorialAbilityManager;
    private readonly ICardViewHandler _cardViewHandler;
    private readonly IBoardAreaController _boardAreaController;
    private Action _onDragStart;
    private Action<Vector2, int> _onDragContinue;
    private bool _isDragStart;
    private CardItemData _cardItemData;
    
    public CardMoveHandler(IHapticController hapticController, ITutorialAbilityManager tutorialAbilityManager, ICardViewHandler cardViewHandler, IBoardAreaController boardAreaController, Action onDragStart, Action<Vector2, int> onDragContinue)
    {
        _hapticController = hapticController;
        _tutorialAbilityManager = tutorialAbilityManager;
        _cardViewHandler = cardViewHandler;
        _boardAreaController = boardAreaController;
        _onDragStart = onDragStart;
        _onDragContinue = onDragContinue;
    }

    public void Initialize(CardItemData cardItemData)
    {
        _cardItemData = cardItemData;
    }

    public void HandleDrag(PointerEventData data)
    {
        if(!_tutorialAbilityManager.IsCardDraggable(_cardItemData.CardItemIndex)) return;
        
        if (!_isDragStart)
        {
            _hapticController.Vibrate(HapticType.CardGrab);
            _onDragStart?.Invoke();
            _cardViewHandler.InitializeDrag(_cardItemData.TempParent);
        }

        _isDragStart = true;
        Vector2 localPosition = _cardViewHandler.CalculateAnchoredPosition(data.position);
        _cardViewHandler.UpdatePosition(localPosition);
        _onDragContinue(data.position, _cardItemData.CardItemIndex);
    }

    public void MoveCardToBoard(int boardCardHolderIndex, RectTransform tempParent)
    {
        _onDragStart?.Invoke();
        RectTransform cardHolderTransform = _boardAreaController.GetRectTransformOfBoardHolder(boardCardHolderIndex);
        _cardViewHandler.SetParent(tempParent);
        _cardViewHandler.PlaceCard(cardHolderTransform.position, cardHolderTransform);
    }

    public bool IsDragStarted()
    {
        return _isDragStart;
    }

    public void OnPointerDown(PointerEventData data)
    {
        _isDragStart = false;
    }
}

public interface ICardMoveHandler
{
    void Initialize(CardItemData cardItemData);
    void HandleDrag(PointerEventData data);
    void MoveCardToBoard(int boardCardHolderIndex, RectTransform tempParent);
    bool IsDragStarted();
    void OnPointerDown(PointerEventData data);
}
}
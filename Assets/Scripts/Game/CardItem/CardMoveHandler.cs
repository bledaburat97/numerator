using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
public class CardMoveHandler : ICardMoveHandler
{
    private readonly IHapticController _hapticController;
    private readonly IBoardAreaController _boardAreaController;
    private Action<int> _onDragStart;
    private Action<Vector2, int> _onDragContinue;
    private Func<int, int> _onDragComplete;
    private Action<int> _onClick;
    private bool _isDragStart;
    private int _cardIndex;
    public event EventHandler<RectTransform> MoveCardToBoardEvent;
    public event EventHandler MoveCardToInitialEvent;

    public CardMoveHandler(IHapticController hapticController, IBoardAreaController boardAreaController, int cardIndex)
    {
        _hapticController = hapticController;
        _boardAreaController = boardAreaController;
        _cardIndex = cardIndex;
    }

    public void HandleDrag(Vector2 position)
    {
        if (!_isDragStart)
        {
            _hapticController.Vibrate(HapticType.CardGrab);
            _onDragStart?.Invoke(_cardIndex);
        }

        _isDragStart = true;
        _onDragContinue(position, _cardIndex);
    }

    public void MoveCardToBoard(RectTransform boardHolderTransform)
    {
        _onDragStart?.Invoke(_cardIndex);
        MoveCardToBoardEvent?.Invoke(this, boardHolderTransform);
    }

    public void SetOnClick(Action<int> onClick)
    {
        _onClick = onClick;
    }
    
    public void SetOnDragComplete(Func<int, int> onDragComplete)
    {
        _onDragComplete = onDragComplete;
    }

    public void SetOnDragStart(Action<int> onDragStart)
    {
        _onDragStart = onDragStart;
    }

    public void SetOnDragContinue(Action<Vector2, int> onDragContinue)
    {
        _onDragContinue = onDragContinue;
    }
    
    public void OnPointerUp(PointerEventData data)
    {
        if (!IsDragStarted())
        {
            _onClick?.Invoke(_cardIndex);
        }
        else
        {
            _hapticController.Vibrate(HapticType.CardRelease);
            int boardHolderIndex = _onDragComplete(_cardIndex);
            if (boardHolderIndex != -1)
            {
                MoveCardToBoardEvent?.Invoke(this, _boardAreaController.GetRectTransformOfBoardHolder(boardHolderIndex));
            }
            else
            {
                MoveCardToInitialEvent?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsDragStarted()
    {
        return _isDragStart;
    }

    public void OnPointerDown(PointerEventData data)
    {
        _isDragStart = false;
    }

    public void MoveCardToInitial()
    {
        MoveCardToInitialEvent?.Invoke(this, EventArgs.Empty);
    }
}

public interface ICardMoveHandler
{
    void MoveCardToBoard(RectTransform boardHolderTransform);
    bool IsDragStarted();
    void OnPointerDown(PointerEventData data);
    void OnPointerUp(PointerEventData data);
    void SetOnDragStart(Action<int> onDragStart);
    void SetOnClick(Action<int> onClick);
    void SetOnDragComplete(Func<int, int> onDragComplete);
    void SetOnDragContinue(Action<Vector2, int> onDragContinue);
    void HandleDrag(Vector2 position);
    void MoveCardToInitial();
    event EventHandler<RectTransform> MoveCardToBoardEvent;
    event EventHandler MoveCardToInitialEvent;
}
}
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
public class CardMoveHandler : ICardMoveHandler
{
    private readonly IHapticController _hapticController;
    private Action<int> _onDragStart;
    private Action<Vector2, int> _onDragContinue;
    private Func<int, int> _onDragComplete;
    private Action<int, int> _onMoveToBoardRequested;
    private Action<int> _onMoveToInitialRequested;
    private Action<int> _onClick;
    private bool _isDragStart;
    private bool _isMovementLocked;
    private int _cardIndex;

    public CardMoveHandler(IHapticController hapticController, int cardIndex)
    {
        _hapticController = hapticController;
        _cardIndex = cardIndex;
    }

    public void HandleDrag(Vector2 position)
    {
        if (_isMovementLocked) return;

        if (!_isDragStart)
        {
            _hapticController.Vibrate(HapticType.CardGrab);
            _onDragStart?.Invoke(_cardIndex);
        }

        _isDragStart = true;
        _onDragContinue?.Invoke(position, _cardIndex);
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

    public void SetOnMoveToBoardRequested(Action<int, int> onMoveToBoardRequested)
    {
        _onMoveToBoardRequested = onMoveToBoardRequested;
    }

    public void SetOnMoveToInitialRequested(Action<int> onMoveToInitialRequested)
    {
        _onMoveToInitialRequested = onMoveToInitialRequested;
    }
    
    public void OnPointerUp(PointerEventData data)
    {
        if (_isMovementLocked) return;

        if (!IsDragStarted())
        {
            _onClick?.Invoke(_cardIndex);
        }
        else
        {
            _hapticController.Vibrate(HapticType.CardRelease);
            int boardHolderIndex = _onDragComplete != null ? _onDragComplete(_cardIndex) : -1;
            if (boardHolderIndex != -1)
            {
                _onMoveToBoardRequested?.Invoke(_cardIndex, boardHolderIndex);
            }
            else
            {
                _onMoveToInitialRequested?.Invoke(_cardIndex);
            }
        }
    }

    public bool IsDragStarted()
    {
        return _isDragStart;
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (_isMovementLocked) return;

        _isDragStart = false;
    }

    public void SetMovementLocked(bool isLocked)
    {
        _isMovementLocked = isLocked;
        if (isLocked)
        {
            _isDragStart = false;
        }
    }

    public bool IsMovementLocked()
    {
        return _isMovementLocked;
    }
}

public interface ICardMoveHandler
{
    bool IsDragStarted();
    void OnPointerDown(PointerEventData data);
    void OnPointerUp(PointerEventData data);
    void SetOnDragStart(Action<int> onDragStart);
    void SetOnClick(Action<int> onClick);
    void SetOnDragComplete(Func<int, int> onDragComplete);
    void SetOnDragContinue(Action<Vector2, int> onDragContinue);
    void SetOnMoveToBoardRequested(Action<int, int> onMoveToBoardRequested);
    void SetOnMoveToInitialRequested(Action<int> onMoveToInitialRequested);
    void HandleDrag(Vector2 position);
    void SetMovementLocked(bool isLocked);
    bool IsMovementLocked();
}
}

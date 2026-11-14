using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
public class CardMoveHandler : ICardMoveHandler
{
    public const float LongPressDuration = .7f;
    private readonly IHapticController _hapticController;
    private Action<int> _onDragStart;
    private Action<Vector2, int> _onDragContinue;
    private Func<int, int> _onDragComplete;
    private Action<int, int> _onMoveToBoardRequested;
    private Action<int> _onMoveToInitialRequested;
    private Action<int> _onClick;
    private Func<int, bool> _onLongPress;
    private Tween _holdTween;
    private bool _isPressed, _longPressConsumed;
    private bool _isDragStart;
    private bool _isMovementLocked;
    private int _cardIndex;
    private int _pointerId=int.MinValue;
    public bool CanHandleDrag(PointerEventData data)=>_isPressed && !_isMovementLocked && !_longPressConsumed
        && (_pointerId==int.MinValue || data!=null && data.pointerId==_pointerId);

    public CardMoveHandler(IHapticController hapticController, int cardIndex)
    {
        _hapticController = hapticController;
        _cardIndex = cardIndex;
    }

    public void HandleDrag(Vector2 position)
    {
        if (_isMovementLocked || !_isPressed || _longPressConsumed) return;
        CancelHoldTimer();

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

    public void SetOnLongPress(Func<int, bool> onLongPress)
    { CancelPress(); _onLongPress = onLongPress; }
    private void CancelHoldTimer() { _holdTween?.Kill(); _holdTween = null; }
    public void CancelPress()
    {
        bool dragging=_isDragStart;
        CancelHoldTimer(); _isPressed = _isDragStart = _longPressConsumed = false;_pointerId=int.MinValue;
        if(dragging)_onMoveToInitialRequested?.Invoke(_cardIndex);
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
        if (!_isPressed || data!=null && _pointerId!=int.MinValue && data.pointerId!=_pointerId) return;
        CancelHoldTimer(); _isPressed = false;
        if (_isMovementLocked) return;
        if (_longPressConsumed && !_isDragStart) { _longPressConsumed = false; return; }

        bool wasDragging = _isDragStart;
        _isDragStart = false;
        if (!wasDragging)
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
        CancelPress();
        if (_isMovementLocked) return;

        _isPressed = true;
        _pointerId=data?.pointerId??int.MinValue;
        if (_onLongPress != null)
            _holdTween = DOVirtual.DelayedCall(LongPressDuration, () => {
                _holdTween = null;
                if (_isPressed && !_isDragStart && !_isMovementLocked)
                    _longPressConsumed = _onLongPress?.Invoke(_cardIndex) == true;
            }, true).SetUpdate(DOTween.defaultUpdateType, true);
    }

    public void SetMovementLocked(bool isLocked)
    {
        _isMovementLocked = isLocked;
        if (isLocked)
        {
            CancelPress();
        }
    }

    public bool IsMovementLocked()
    {
        return _isMovementLocked;
    }
}

public interface ICardMoveHandler
{
    bool CanHandleDrag(PointerEventData data);
    bool IsDragStarted();
    void OnPointerDown(PointerEventData data);
    void OnPointerUp(PointerEventData data);
    void SetOnDragStart(Action<int> onDragStart);
    void SetOnClick(Action<int> onClick);
    void SetOnLongPress(Func<int, bool> onLongPress);
    void CancelPress();
    void SetOnDragComplete(Func<int, int> onDragComplete);
    void SetOnDragContinue(Action<Vector2, int> onDragContinue);
    void SetOnMoveToBoardRequested(Action<int, int> onMoveToBoardRequested);
    void SetOnMoveToInitialRequested(Action<int> onMoveToInitialRequested);
    void HandleDrag(Vector2 position);
    void SetMovementLocked(bool isLocked);
    bool IsMovementLocked();
}
}

using System;
using UnityEngine;

namespace Scripts
{
    public class DraggableCardItemController 
    {
        protected CardItemData _cardItemData;
        protected Action<Vector2, int> _onDragContinue;

        protected void SetOnDragContinue(Action<Vector2, int> action)
        {
            _onDragContinue += action;
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class InvisibleClickHandler : MonoBehaviour, IPointerClickHandler, IInvisibleClickHandler
    {
        private Action _deselectAction;
        public void Initialize(Action deselectAction)
        {
            _deselectAction = deselectAction;
        }
        
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("InvisibleClick");
            _deselectAction.Invoke();
        }
    }

    public interface IInvisibleClickHandler
    {
        void Initialize(Action deselectAction);
    }
}
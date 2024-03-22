using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Scripts
{
    public class InvisibleClickHandler : MonoBehaviour, IPointerClickHandler, IInvisibleClickHandler
    {
        public event EventHandler OnInvisibleClicked;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("InvisibleClick");
            OnInvisibleClicked?.Invoke(this, EventArgs.Empty);
        }
    }

    public interface IInvisibleClickHandler
    {
        event EventHandler OnInvisibleClicked;
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CloseButtonView : MonoBehaviour, ICloseButtonView
    {
        [SerializeField] private Button button;
        
        public void Init(CloseButtonModel model)
        {
            SetOnClickAction(model.OnClick);
        }

        private void SetOnClickAction(Action onClick)
        {
            button.onClick.AddListener(() => onClick.Invoke());
        }
    }

    public interface ICloseButtonView
    {
        void Init(CloseButtonModel model);
    }
}
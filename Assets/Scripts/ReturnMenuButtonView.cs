using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ReturnMenuButtonView : MonoBehaviour, IReturnMenuButtonView
    {
        [SerializeField] private Button button;
        
        public void Init(ReturnMenuButtonModel model)
        {
            SetOnClickAction(model.OnClick);
        }

        private void SetOnClickAction(Action onClick)
        {
            button.onClick.AddListener(() => onClick.Invoke());
        }
    }

    public interface IReturnMenuButtonView
    {
        void Init(ReturnMenuButtonModel model);
    }

    public class ReturnMenuButtonModel
    {
        public Action OnClick;
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class ResetButtonView : MonoBehaviour, IResetButtonView
    {
        [SerializeField] private Button button;
        
        public void Init(ResetButtonModel model)
        {
            SetOnClickAction(model.OnClick);
        }

        private void SetOnClickAction(Action onClick)
        {
            button.onClick.AddListener(() => onClick.Invoke());
        }
    }

    public interface IResetButtonView
    {
        void Init(ResetButtonModel model);
    }

    public class ResetButtonModel
    {
        public Action OnClick;
    }
}
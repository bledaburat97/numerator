using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CheckButtonView : MonoBehaviour, ICheckButtonView
    {
        [SerializeField] private Button button;
        
        public void Init(CheckButtonModel model)
        {
            SetOnClickAction(model.OnClick);
        }

        private void SetOnClickAction(Action onClick)
        {
            button.onClick.AddListener(() => onClick.Invoke());
        }
    }


    public interface ICheckButtonView
    {
        void Init(CheckButtonModel model);
    }

    public class CheckButtonModel
    {
        public Action OnClick;
    }
}
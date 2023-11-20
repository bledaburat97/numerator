using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class SettingsButtonView : MonoBehaviour, ISettingsButtonView
    {
        [SerializeField] private Button button;
        
        public void Init(SettingsButtonModel model)
        {
            SetOnClickAction(model.OnClick);
        }

        private void SetOnClickAction(Action onClick)
        {
            button.onClick.AddListener(() => onClick.Invoke());
        }
    }

    public interface ISettingsButtonView
    {
        void Init(SettingsButtonModel model);
    }
}
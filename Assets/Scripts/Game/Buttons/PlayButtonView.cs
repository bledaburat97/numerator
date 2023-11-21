using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PlayButtonView : MonoBehaviour, IPlayButtonView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Button button;
        
        public void Init(PlayButtonModel model)
        {
            text.SetText(model.text);
            SetOnClickAction(model.OnClick);
        }

        private void SetOnClickAction(Action onClick)
        {
            button.onClick.AddListener(() => onClick.Invoke());
        }
    }

    public interface IPlayButtonView
    {
        void Init(PlayButtonModel model);
    }

    public class PlayButtonModel
    {
        public string text;
        public Action OnClick;
    }
}
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
            SetText(model.LevelId);
            SetOnClickAction(model.OnClick);
        }

        private void SetText(int levelId)
        {
            text.SetText("Level " + levelId);
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
        public int LevelId;
        public Action OnClick;
    }
}
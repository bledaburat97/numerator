using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GameUIView : MonoBehaviour, IGameUIView
    {
        [SerializeField] private TMP_Text levelIdText;
        [SerializeField] private RectTransform scrollAreaRectTransform;
        [SerializeField] private BaseButtonView checkButton;
        [SerializeField] private BaseButtonView resetButton;
        [SerializeField] private BaseButtonView settingsButton;
        [SerializeField] private CardInfoButtonView cardInfoButton;
        public void SetLevelId(string text)
        {
            levelIdText.SetText(text);
        }

        public void DisableLevelId()
        {
            levelIdText.gameObject.SetActive(false);
        }

        public void IncreaseSizeAndPositionOfScrollArea(float sizeOfResultBlock)
        {
            scrollAreaRectTransform.localPosition = new Vector2(0, scrollAreaRectTransform.localPosition.y + sizeOfResultBlock / 2);
            scrollAreaRectTransform.sizeDelta = new Vector2(scrollAreaRectTransform.sizeDelta.x,scrollAreaRectTransform.sizeDelta.y + sizeOfResultBlock);
        }

        public IBaseButtonView GetCheckButton()
        {
            return checkButton;
        }
        
        public IBaseButtonView GetResetButton()
        {
            return resetButton;
        }
        
        public IBaseButtonView GetSettingsButton()
        {
            return settingsButton;
        }

        public ICardInfoButtonView GetCardInfoButton()
        {
            return cardInfoButton;
        }
    }

    public interface IGameUIView
    {
        void SetLevelId(string text);
        void DisableLevelId();
        void IncreaseSizeAndPositionOfScrollArea(float sizeOfResultBlock);
        IBaseButtonView GetCheckButton();
        IBaseButtonView GetResetButton();
        IBaseButtonView GetSettingsButton();
        ICardInfoButtonView GetCardInfoButton();
    }
}
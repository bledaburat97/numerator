using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class GameUIView : MonoBehaviour, IGameUIView
    {
        [SerializeField] private TMP_Text userText;
        [SerializeField] private TMP_Text opponentText;
        [SerializeField] private RectTransform scrollAreaRectTransform;
        [SerializeField] private BaseButtonView checkButton;
        [SerializeField] private BaseButtonView resetButton;
        [SerializeField] private BaseButtonView settingsButton;
        [SerializeField] private CardInfoButtonView cardInfoButton;
        [SerializeField] private GameObject opponentInfo;
        [SerializeField] private CanvasGroup topButtonsCanvasGroup;
        [SerializeField] private CanvasGroup middleButtonsCanvasGroup;
        [SerializeField] private Image coinImage;
        [SerializeField] private TextHolderAdjustment coinCountHolder;

        public CanvasGroup GetTopButtonsCanvasGroup() => topButtonsCanvasGroup;
        public CanvasGroup GetMiddleButtonsCanvasGroup() => middleButtonsCanvasGroup;
        
        public void SetUserText(string text)
        {
            userText.gameObject.SetActive(true);
            userText.SetText(text);
        }
        
        public void SetOpponentText(string text)
        {
            opponentText.gameObject.SetActive(true);
            opponentText.SetText(text);
        }

        public void SetOpponentInfoStatus(bool status)
        {
            opponentInfo.SetActive(status);
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
        
        public TMP_Text GetUserText()
        {
            return userText;
        }

        public void SetCoinCounterStatus(bool status)
        {
            if (coinImage != null)
            {
                coinImage.gameObject.SetActive(status);
            }

            if (coinCountHolder != null)
            {
                coinCountHolder.gameObject.SetActive(status);
            }
        }

        public void SetCoinCount(int count)
        {
            if (coinCountHolder == null) return;

            coinCountHolder.SetText(count.ToString());
            coinCountHolder.SetPosition();
        }

        public RectTransform GetCoinImageRectTransform()
        {
            return coinImage != null ? coinImage.rectTransform : null;
        }

        public Sequence PunchCoinCounter(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            if (coinImage != null)
            {
                sequence.Join(coinImage.rectTransform.DOPunchScale(Vector3.one * 0.08f, duration, 5, 0.65f));
            }

            if (coinCountHolder != null)
            {
                sequence.Join(coinCountHolder.transform.DOPunchScale(Vector3.one * 0.06f, duration, 5, 0.65f));
            }

            return sequence;
        }
    }

    public interface IGameUIView
    {
        void SetUserText(string text);
        void SetOpponentText(string text);
        IBaseButtonView GetCheckButton();
        IBaseButtonView GetResetButton();
        IBaseButtonView GetSettingsButton();
        ICardInfoButtonView GetCardInfoButton();
        void SetOpponentInfoStatus(bool status);
        CanvasGroup GetTopButtonsCanvasGroup();
        CanvasGroup GetMiddleButtonsCanvasGroup();
        TMP_Text GetUserText();
        void SetCoinCounterStatus(bool status);
        void SetCoinCount(int count);
        RectTransform GetCoinImageRectTransform();
        Sequence PunchCoinCounter(float duration);
    }
}

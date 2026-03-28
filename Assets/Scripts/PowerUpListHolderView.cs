using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PowerUpListHolderView : MonoBehaviour, IPowerUpListHolderView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private PowerUpButtonView revealingPowerUpButton;
        [SerializeField] private PowerUpButtonView lifePowerUpButton;
        [SerializeField] private PowerUpButtonView bombPowerUpButton;
        [SerializeField] private TMP_Text revealingPowerUpCount;
        [SerializeField] private TMP_Text lifePowerUpCount;
        [SerializeField] private TMP_Text bombPowerUpCount;

        private CanvasGroup _canvasGroup;

        
        public IBaseButtonView GetRevealingPowerUpButton()
        {
            return revealingPowerUpButton;
        }
        
        public IBaseButtonView GetLifePowerUpButton()
        {
            return lifePowerUpButton;
        }
        
        public IBaseButtonView GetBombPowerUpButton()
        {
            return bombPowerUpButton;
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
        
        public void SetRevealingPowerUpCount(string count)
        {
            revealingPowerUpCount.SetText(count);
        }
        
        public void SetLifePowerUpCount(string count)
        {
            lifePowerUpCount.SetText(count);
        }
        
        public void SetBombPowerUpCount(string count)
        {
            bombPowerUpCount.SetText(count);
        }

        public IBaseButtonView GetPowerUpButton(RewardType rewardType)
        {
            switch (rewardType)
            {
                case RewardType.Revealing:
                    return revealingPowerUpButton;
                case RewardType.Life:
                    return lifePowerUpButton;
                case RewardType.Bomb:
                    return bombPowerUpButton;
                default:
                    return revealingPowerUpButton;
            }
        }

        public void SetPowerUpCount(RewardType rewardType, int count)
        {
            string text = $"x{count}";
            switch (rewardType)
            {
                case RewardType.Revealing:
                    SetRevealingPowerUpCount(text);
                    break;
                case RewardType.Life:
                    SetLifePowerUpCount(text);
                    break;
                case RewardType.Bomb:
                    SetBombPowerUpCount(text);
                    break;
            }
        }

        public void SetPowerUpAlpha(RewardType rewardType, float alpha)
        {
            switch (rewardType)
            {
                case RewardType.Revealing:
                    SetButtonAlpha(revealingPowerUpButton, alpha);
                    SetTextAlpha(revealingPowerUpCount, alpha);
                    break;
                case RewardType.Life:
                    SetButtonAlpha(lifePowerUpButton, alpha);
                    SetTextAlpha(lifePowerUpCount, alpha);
                    break;
                case RewardType.Bomb:
                    SetButtonAlpha(bombPowerUpButton, alpha);
                    SetTextAlpha(bombPowerUpCount, alpha);
                    break;
            }
        }

        public void SetRaycastStatus(bool status)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _canvasGroup.interactable = status;
            _canvasGroup.blocksRaycasts = status;
        }

        private static void SetButtonAlpha(IBaseButtonView buttonView, float alpha)
        {
            if (buttonView == null) return;

            CanvasGroup canvasGroup = buttonView.GetButtonRectTransform().GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = buttonView.GetButtonRectTransform().gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = alpha;
        }

        private static void SetTextAlpha(TMP_Text text, float alpha)
        {
            if (text == null) return;

            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }

    public interface IPowerUpListHolderView
    {
        IBaseButtonView GetRevealingPowerUpButton();
        IBaseButtonView GetLifePowerUpButton();
        IBaseButtonView GetBombPowerUpButton();
        IBaseButtonView GetPowerUpButton(RewardType rewardType);
        RectTransform GetRectTransform();
        void SetRevealingPowerUpCount(string count);
        void SetLifePowerUpCount(string count);
        void SetBombPowerUpCount(string count);
        void SetPowerUpCount(RewardType rewardType, int count);
        void SetPowerUpAlpha(RewardType rewardType, float alpha);
        void SetRaycastStatus(bool status);
    }
}

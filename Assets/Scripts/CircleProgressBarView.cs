using Game;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class CircleProgressBarView : MonoBehaviour, ICircleProgressBarView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image circleImage;
        [SerializeField] private RewardItemTargetView rewardItemTargetPrefab;
        [SerializeField] private MovingRewardItemView movingRewardItemPrefab;
        [SerializeField] private PowerUpButtonView revealingPowerUpButton;
        [SerializeField] private PowerUpButtonView lifePowerUpButton;
        [SerializeField] private PowerUpButtonView bombPowerUpButton;
        
        public IRewardItemTargetView CreateRewardItemTarget(RewardItemTargetViewFactory rewardItemTargetViewFactory)
        {
            if (rewardItemTargetPrefab == null)
            {
                GameObject targetObject = new GameObject("RewardItemTarget", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(RewardItemTargetView));
                RectTransform targetRectTransform = targetObject.GetComponent<RectTransform>();
                targetRectTransform.SetParent(circleImage.rectTransform, false);
                return targetObject.GetComponent<RewardItemTargetView>();
            }

            return rewardItemTargetViewFactory.Spawn(circleImage.rectTransform, rewardItemTargetPrefab);
        }

        public MovingRewardItemView GetMovingRewardItemPrefab()
        {
            return movingRewardItemPrefab;
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public Image GetImage()
        {
            return circleImage;
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }
        
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

        public void ShowRewardPreview(RewardType rewardType)
        {
            revealingPowerUpButton.SetButtonStatus(rewardType == RewardType.Revealing);
            lifePowerUpButton.SetButtonStatus(rewardType == RewardType.Life);
            bombPowerUpButton.SetButtonStatus(rewardType == RewardType.Bomb);
        }
    }

    public interface ICircleProgressBarView
    {
        RectTransform GetRectTransform();
        Image GetImage();
        IRewardItemTargetView CreateRewardItemTarget(RewardItemTargetViewFactory rewardItemTargetViewFactory);
        void SetStatus(bool status);
        IBaseButtonView GetRevealingPowerUpButton();
        IBaseButtonView GetLifePowerUpButton();
        IBaseButtonView GetBombPowerUpButton();
        IBaseButtonView GetPowerUpButton(RewardType rewardType);
        void ShowRewardPreview(RewardType rewardType);
        MovingRewardItemView GetMovingRewardItemPrefab();
    }
}

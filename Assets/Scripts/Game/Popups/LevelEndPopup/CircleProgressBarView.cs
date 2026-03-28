using DG.Tweening;
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

        public IRewardItemTargetView CreateRewardItemTarget(RewardItemTargetViewFactory rewardItemTargetViewFactory)
        {
            if (rewardItemTargetPrefab != null)
            {
                return rewardItemTargetViewFactory.Spawn(circleImage.rectTransform, rewardItemTargetPrefab);
            }

            GameObject targetObject = new GameObject("RewardItemTarget", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(RewardItemTargetView));
            RectTransform targetRectTransform = targetObject.GetComponent<RectTransform>();
            targetRectTransform.SetParent(circleImage.rectTransform, false);
            return targetObject.GetComponent<RewardItemTargetView>();
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
    }

    public interface ICircleProgressBarView
    {
        RectTransform GetRectTransform();
        Image GetImage();
        IRewardItemTargetView CreateRewardItemTarget(RewardItemTargetViewFactory rewardItemTargetViewFactory);
        void SetStatus(bool status);
    }
}

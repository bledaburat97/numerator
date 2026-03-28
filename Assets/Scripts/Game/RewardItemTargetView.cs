using UnityEngine;

namespace Game
{
    public class RewardItemTargetView : MonoBehaviour, IRewardItemTargetView
    {
        [SerializeField] private RectTransform rectTransform;
        private MovingRewardItemView _rewardItemView;

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
        }

        public void SetLocalPosition(Vector2 localPosition)
        {
            GetRectTransform().localPosition = localPosition;
        }

        public void SetLocalScale(Vector3 localScale)
        {
            GetRectTransform().localScale = localScale;
        }

        public void SetSize(Vector2 size)
        {
            GetRectTransform().sizeDelta = size;
        }

        public RectTransform GetRectTransform()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            return rectTransform;
        }

        public MovingRewardItemView GetRewardItem()
        {
            return _rewardItemView;
        }

        public void SetRewardItem(MovingRewardItemView rewardItemView)
        {
            _rewardItemView = rewardItemView;
        }

        public void ClearRewardItem()
        {
            _rewardItemView = null;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }

    public interface IRewardItemTargetView
    {
        void SetLocalPosition(Vector2 localPosition);
        void SetLocalScale(Vector3 localScale);
        void SetSize(Vector2 size);
        RectTransform GetRectTransform();
        MovingRewardItemView GetRewardItem();
        void SetRewardItem(MovingRewardItemView rewardItemView);
        void ClearRewardItem();
        void Destroy();
    }
}

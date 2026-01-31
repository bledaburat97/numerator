using Game;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class StarImageView : MonoBehaviour, IStarImageView
    {
        [SerializeField] private Image star;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CurvedAnimationPreset curvedAnimationPreset;
        [SerializeField] private MovingRewardItemView movingRewardItemPrefab;
        
        private MovingRewardItemView _movingRewardItemView;
        public void SetLocalPosition(Vector2 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public void CreateMovingRewardItem(Vector2 size, float orbitRadius)
        {
            _movingRewardItemView = Instantiate(movingRewardItemPrefab, rectTransform);
            _movingRewardItemView.Init(rectTransform);
            _movingRewardItemView.SetSize(size);
        }

        public void SetMovingRewardItemStatus(bool status)
        {
            if (_movingRewardItemView != null)
                _movingRewardItemView.SetStatus(status);
        }


        public MovingRewardItemView GetMovingRewardItem()
        {
            return _movingRewardItemView;
        }

        public void SetLocalScale(Vector3 localScale)
        {
            transform.localScale = localScale;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }
        
        public void SetStarStatus(bool status)
        {
            star.gameObject.SetActive(status);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }

        public void SetParent(RectTransform parent)
        {
            rectTransform.SetParent(parent);
        }

        public CurvedAnimationPreset GetCurvedAnimationPreset()
        {
            return curvedAnimationPreset;
        }

        public void SetColor(bool originalColor)
        {
            star.color = originalColor ? ConstantValues.YELLOW_STAR_COLOR : ConstantValues.BLUE_STAR_COLOR;
        }
    }

    public interface IStarImageView
    {
        void SetLocalPosition(Vector2 localPosition);
        void SetLocalScale(Vector3 localScale);
        void SetStarStatus(bool status);
        void SetSize(Vector2 size);
        RectTransform GetRectTransform();
        void SetParent(RectTransform parent);
        void Destroy();
        CurvedAnimationPreset GetCurvedAnimationPreset();
        void SetColor(bool originalColor);
        MovingRewardItemView GetMovingRewardItem();
        void CreateMovingRewardItem(Vector2 size, float orbitRadius);
        void SetMovingRewardItemStatus(bool status);
    }
}
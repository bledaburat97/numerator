using System;
using UnityEngine;

namespace Scripts
{
    public class DirectionButtonView : BaseButtonView, IDirectionButtonView
    {
        [SerializeField] private RectTransform directionImage;

        public void Init(DirectionButtonModel model)
        {
            transform.localPosition = model.localPosition;
            transform.localScale = Vector3.one;
            innerBgOffsetMin = innerBg.rectTransform.offsetMin;
            innerBgOffsetMax = innerBg.rectTransform.offsetMax;
            if (model.direction == Direction.Backward)
            {
                directionImage.localScale = new Vector2(-1,1);
                directionImage.localPosition =
                    new Vector2(-directionImage.localPosition.x, directionImage.localPosition.y);
            }
            button.onClick.AddListener(() => model.onClick.Invoke(model.direction));
            gameObject.SetActive(false);
        }

        public void SetButtonStatus(bool status)
        {
            gameObject.SetActive(status);
        }
    }

    public interface IDirectionButtonView : IBaseButtonView
    {
        void Init(DirectionButtonModel model);
        void SetButtonStatus(bool status);
    }

    public enum Direction
    {
        Forward,
        Backward
    }
    
    public class DirectionButtonModel
    {
        public Vector2 localPosition;
        public Action<Direction> onClick;
        public Direction direction;
    }
}
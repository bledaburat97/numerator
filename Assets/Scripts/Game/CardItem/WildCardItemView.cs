using UnityEngine;

namespace Scripts
{
    public class WildCardItemView : DraggableCardItemView, IWildCardItemView
    {
        private Vector3 _localPositionGap = Vector2.zero;
        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetLocalPositionGap(int cardItemIndex)
        {
            _localPositionGap += new Vector3(-3.5f, -2f, 0f) * cardItemIndex;
        }
        
        public override void InitPosition()
        {
            rectTransform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero + _localPositionGap;
        }
    }

    public interface IWildCardItemView : IDraggableCardItemView
    {
        void Destroy();
        void SetLocalPositionGap(int cardItemIndex);
    }
}
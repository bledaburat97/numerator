using UnityEngine;

namespace Game
{
    public class MovingRewardItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }
        
        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
    }
}
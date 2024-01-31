using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class HandTutorialView : MonoBehaviour, IHandTutorialView
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image handImage;
        
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public Image GetHand()
        {
            return handImage;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public RectTransform GetRectTransform()
        {
            return rectTransform;
        }
    }

    public interface IHandTutorialView
    {
        void Init();
        Image GetHand();
        void Destroy();
        RectTransform GetRectTransform();
    }
}
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class GalaxyLockView : MonoBehaviour, IGalaxyLockView
    {
        [SerializeField] private TMP_Text starCount;
        [SerializeField] private RectTransform starRectTransform;
        private float _gap = 6f;
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = new Vector3(0,80,0);
        }

        public void SetStarCountToUnlock(int starCountToUnlock)
        {
            starCount.text = starCountToUnlock.ToString();
            float textSize = starCount.preferredWidth;
            starCount.rectTransform.localPosition = new Vector3(-(starRectTransform.rect.width + _gap) / 2,
                starCount.rectTransform.localPosition.y, 0);
            starRectTransform.localPosition = new Vector3((textSize + _gap) / 2, starRectTransform.localPosition.y, 0);
        }

        public void SetStatus(bool status)
        {
            gameObject.SetActive(status);
        }
    }

    public interface IGalaxyLockView
    {
        void Init();
        void SetStarCountToUnlock(int starCountToUnlock);
        void SetStatus(bool status);
    }
}
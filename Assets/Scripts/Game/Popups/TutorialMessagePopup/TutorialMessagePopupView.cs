using TMPro;
using UnityEngine;

namespace Scripts
{
    public class TutorialMessagePopupView : MonoBehaviour, ITutorialMessagePopupView
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform rectTransform;
        public void Init()
        {
            transform.localScale = Vector3.one;
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -rectTransform.rect.height / 2);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetText(string text)
        {
            this.text.SetText(text);
        }
    }

    public interface ITutorialMessagePopupView
    {
        void Init();
        void Destroy();
        void SetText(string text);
    }
}
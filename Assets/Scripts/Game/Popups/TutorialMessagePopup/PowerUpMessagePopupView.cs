using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class PowerUpMessagePopupView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image powerUpImage;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text text;
        [SerializeField] private BaseButtonView baseButtonView;
        [SerializeField] private RectTransform leftSide;
        private float borderHeight = 14f;

        public void Init()
        {
            transform.localScale = Vector3.one;
        }

        public void SetSize(bool hasButton)
        {
            float buttonHeight = hasButton ? baseButtonView.GetButtonRectTransform().rect.height : 0;
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, title.rectTransform.sizeDelta.y + text.rectTransform.sizeDelta.y + buttonHeight + borderHeight);
            float maxPowerUpImageSize = leftSide.rect.width > leftSide.rect.height ? leftSide.rect.height : leftSide.rect.width;
            powerUpImage.rectTransform.sizeDelta = new Vector2(maxPowerUpImageSize, maxPowerUpImageSize);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector3(0f, 0f);
            text.rectTransform.pivot = new Vector2(0.5f, 1f);
            text.rectTransform.anchoredPosition = new Vector3(0f, - title.rectTransform.sizeDelta.y);
            if (hasButton)
            {
                baseButtonView.GetButtonRectTransform().pivot = new Vector2(0.5f, 1f);
                baseButtonView.GetButtonRectTransform().anchoredPosition = new Vector3(0f, - title.rectTransform.sizeDelta.y - text.rectTransform.sizeDelta.y);
                baseButtonView.gameObject.SetActive(true);
            }
            else
            {
                baseButtonView.gameObject.SetActive(false);
            }
        }

        public BaseButtonView GetBaseButtonView()
        {
            return baseButtonView;
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }

        public void SetTitle(string text)
        {
            title.SetText(text);
        }

        public void SetText(string text)
        {
            this.text.SetText(text);
        }

        public void SetSprite(Sprite sprite)
        {
            powerUpImage.sprite = sprite;
        }
    }
}
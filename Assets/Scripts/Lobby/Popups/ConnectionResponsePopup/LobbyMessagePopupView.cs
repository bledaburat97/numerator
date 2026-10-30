using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LobbyMessagePopupView : MonoBehaviour
    {
        [SerializeField] private BaseButtonView closeButton;
        [SerializeField] private TMP_Text title;
        
        public void Init()
        {
            Hide();
        }

        public IBaseButtonView GetCloseButton()
        {
            return closeButton;
        }

        public void Show(string text)
        {
            gameObject.SetActive(true);
            SetTitle(text);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
        public void ApplyOnlineLayout()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one; rect.sizeDelta = new Vector2(292, 190);
            foreach (var image in GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.gameObject != gameObject && !image.transform.IsChildOf(closeButton.transform)) image.enabled = false;
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Panel).raycastTarget = true; FruitLabStyle.Rim(rect);
            FruitLabStyle.Place(title.rectTransform, transform, 16, 72, 260, 100);
            title.fontSize = 17; title.color = FruitLabStyle.Text; title.textWrappingMode = TextWrappingModes.Normal;
            FruitLabOnlineLayout.Button(closeButton, transform, 12, 292, false);
        }
        
        private void SetTitle(string text)
        {
            title.SetText(text);
        }
    }
}

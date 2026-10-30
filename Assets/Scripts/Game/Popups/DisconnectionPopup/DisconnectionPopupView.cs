using TMPro;
using UnityEngine;

namespace Scripts
{
    public class DisconnectionPopupView : MonoBehaviour, IDisconnectionPopupView
    {
        [SerializeField] private BaseButtonView menuButtonView;
        [SerializeField] private TMP_Text text;
        public string DisplayedMessage => text.text;
        public void Init()
        {
            var root = (RectTransform)transform; var parent = (RectTransform)GetComponentInParent<Canvas>().transform;
            root.SetParent(parent, false); root.SetAsLastSibling();
            float width = Mathf.Min(320f, parent.rect.width - 24f), height = 172f;
            FruitLabStyle.Place(root, parent, (parent.rect.width - width) * .5f, (parent.rect.height - height) * .5f, width, height);
            FruitLabStyle.AddSurface(gameObject, FruitLabStyle.Panel); FruitLabStyle.Rim(root);
            FruitLabStyle.Place(text.rectTransform, transform, 16, 80, width - 32, 64);
            text.color = FruitLabStyle.Text; text.fontSize = 21; text.enableAutoSizing = true; text.fontSizeMin = 15; text.fontSizeMax = 21;
            FruitLabStyle.Place(menuButtonView.GetButtonRectTransform(), transform, 16, 16, width - 32, 44);
            menuButtonView.ApplyLabActionStyle(true);
        }

        public void SetText(string text)
        {
            this.text.SetText(text);
        }

        public IBaseButtonView GetMenuButtonView()
        {
            return menuButtonView;
        }
    }

    public interface IDisconnectionPopupView
    {
        void Init();
        void SetText(string text);
        IBaseButtonView GetMenuButtonView();
    }
}

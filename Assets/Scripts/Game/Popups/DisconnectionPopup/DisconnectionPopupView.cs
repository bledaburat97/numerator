using TMPro;
using UnityEngine;

namespace Scripts
{
    public class DisconnectionPopupView : MonoBehaviour, IDisconnectionPopupView
    {
        [SerializeField] private BaseButtonView menuButtonView;
        [SerializeField] private TMP_Text text;
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
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
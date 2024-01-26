using UnityEngine;

namespace Scripts
{
    public class DisconnectionPopupView : MonoBehaviour, IDisconnectionPopupView
    {
        [SerializeField] private BaseButtonView menuButtonView;

        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public IBaseButtonView GetMenuButtonView()
        {
            return menuButtonView;
        }
    }

    public interface IDisconnectionPopupView
    {
        void Init();
        IBaseButtonView GetMenuButtonView();
    }
}
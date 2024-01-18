using UnityEngine;

namespace Scripts
{
    public class DisconnectionPopupView : MonoBehaviour, IDisconnectionPopupView
    {
        [SerializeField] private PlayButtonView menuButtonView;

        public void Init(BaseButtonModel menuButtonModel)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            menuButtonView.Init(menuButtonModel);
        }
        
    }

    public interface IDisconnectionPopupView
    {
        void Init(BaseButtonModel menuButtonModel);
    }
}
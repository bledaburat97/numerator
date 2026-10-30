using Scripts;
using UnityEngine;

namespace Menu
{
    public class MenuUIView :  MonoBehaviour, IMenuUIView
    {
        [SerializeField] private BaseButtonView singlePlayerButton;
        [SerializeField] private BaseButtonView multiplayerButton;

        public IBaseButtonView GetSinglePlayerButton()
        {
            return singlePlayerButton;
        }
        
        public IBaseButtonView GetMultiplayerButton()
        {
            return multiplayerButton;
        }
        
        public void SetSinglePlayerButtonText(string text)
        {
            singlePlayerButton.SetText(text);
        }

        public void SetMultiplayerAvailability(bool unlocked)
        {
            CanvasGroup group = multiplayerButton.GetComponent<CanvasGroup>();
            if (group == null) group = multiplayerButton.gameObject.AddComponent<CanvasGroup>();
            group.alpha = unlocked ? 1f : .45f;
            group.interactable = unlocked;
            group.blocksRaycasts = unlocked;
        }
    }

    public interface IMenuUIView
    {
        IBaseButtonView GetSinglePlayerButton();
        IBaseButtonView GetMultiplayerButton();
        void SetSinglePlayerButtonText(string text);
        void SetMultiplayerAvailability(bool unlocked);
    }
}

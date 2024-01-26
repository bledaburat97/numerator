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
    }

    public interface IMenuUIView
    {
        IBaseButtonView GetSinglePlayerButton();
        IBaseButtonView GetMultiplayerButton();
        void SetSinglePlayerButtonText(string text);
    }
}
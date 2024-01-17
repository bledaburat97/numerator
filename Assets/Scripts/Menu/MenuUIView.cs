using Scripts;
using UnityEngine;

namespace Menu
{
    public class MenuUIView :  MonoBehaviour, IMenuUIView
    {
        [SerializeField] private PlayButtonView singlePlayerButton;
        [SerializeField] private PlayButtonView multiplayerButton;

        public void SetSinglePlayerButton(BaseButtonModel model)
        {
            singlePlayerButton.Init(model);
        }
        
        public void SetMultiplayerButton(BaseButtonModel model)
        {
            multiplayerButton.Init(model);
        }
        
        public void SetSinglePlayerButtonText(string text)
        {
            singlePlayerButton.SetText(text);
        }
    }

    public interface IMenuUIView
    {
        void SetSinglePlayerButton(BaseButtonModel model);
        void SetMultiplayerButton(BaseButtonModel model);
        void SetSinglePlayerButtonText(string text);
    }
}
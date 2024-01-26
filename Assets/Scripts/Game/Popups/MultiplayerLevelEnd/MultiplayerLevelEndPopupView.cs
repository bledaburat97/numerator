using TMPro;
using UnityEngine;

namespace Scripts
{
    public class MultiplayerLevelEndPopupView : MonoBehaviour, IMultiplayerLevelEndPopupView
    {
        [SerializeField] private TMP_Text header;
        [SerializeField] private BaseButtonView playAgainButton;
        [SerializeField] private BaseButtonView menuButton;
        
        public void Init(bool isSuccess)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            if (isSuccess) header.text = "Congratulations!";
            else header.text = "Opponent made the right guess.";
        }

        public BaseButtonView GetPlayAgainButton()
        {
            return playAgainButton;
        }

        public BaseButtonView GetMenuButton()
        {
            return menuButton;
        }
    }
    
    public interface IMultiplayerLevelEndPopupView
    {
        void Init(bool isSuccess);
        BaseButtonView GetPlayAgainButton();
        BaseButtonView GetMenuButton();
    }
}
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class MultiplayerLevelEndPopupView : MonoBehaviour, IMultiplayerLevelEndPopupView
    {
        [SerializeField] private TMP_Text header;
        [SerializeField] private PlayButtonView playAgainButton;
        [SerializeField] private PlayButtonView menuButton;
        
        public void Init(bool isSuccess, BaseButtonModel playAgainButtonModel, BaseButtonModel menuButtonModel)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            playAgainButton.Init(playAgainButtonModel);
            menuButton.Init(menuButtonModel);
            if (isSuccess) header.text = "Congratulations!";
            else header.text = "Opponent made the right guess.";
        }
    }
    
    public interface IMultiplayerLevelEndPopupView
    {
        void Init(bool isSuccess, BaseButtonModel playAgainButtonModel, BaseButtonModel menuButtonModel);
    }
}
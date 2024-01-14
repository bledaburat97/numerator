using TMPro;
using UnityEngine;

namespace Scripts
{
    public class MultiplayerLevelEndPopupView : MonoBehaviour, IMultiplayerLevelEndPopupView
    {
        [SerializeField] private TMP_Text header;
        [SerializeField] private BaseButtonView playAgainButton;
        [SerializeField] private BaseButtonView menuButton;
        
        public void Init(bool isSuccess, BaseButtonModel playAgainButtonModel, BaseButtonModel menuButtonModel)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            playAgainButton.Init(playAgainButtonModel);
            menuButton.Init(menuButtonModel);
            if (isSuccess) header.text = "WIN";
            else header.text = "LOSE";
        }
    }
    
    public interface IMultiplayerLevelEndPopupView
    {
        void Init(bool isSuccess, BaseButtonModel playAgainButtonModel, BaseButtonModel menuButtonModel);
    }
}
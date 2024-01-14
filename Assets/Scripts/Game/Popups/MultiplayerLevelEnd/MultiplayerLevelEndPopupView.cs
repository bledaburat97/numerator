using TMPro;
using UnityEngine;

namespace Scripts
{
    public class MultiplayerLevelEndPopupView : MonoBehaviour, IMultiplayerLevelEndPopupView
    {
        [SerializeField] private TMP_Text header;

        public void Init(bool isSuccess)
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            if (isSuccess) header.text = "WIN";
            else header.text = "LOSE";
        }
    }
    
    public interface IMultiplayerLevelEndPopupView
    {
        void Init(bool isSuccess);
    }
}
using TMPro;
using UnityEngine;

namespace Scripts
{
    public class LevelEndPopupView : MonoBehaviour, ILevelEndPopupView
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private PlayButtonView playButtonView;
        [SerializeField] private ReturnMenuButtonView returnMenuButtonView;
        public void Init()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
        }

        public void SetTitle(string text)
        {
            title.SetText(text);
        }

        public IPlayButtonView GetPlayButtonView()
        {
            return playButtonView;
        }

        public IReturnMenuButtonView GetReturnMenuButtonView()
        {
            return returnMenuButtonView;
        }
    }
    
    public interface ILevelEndPopupView
    {
        void Init();
        void SetTitle(string text);
        IPlayButtonView GetPlayButtonView();
        IReturnMenuButtonView GetReturnMenuButtonView();
    }
}
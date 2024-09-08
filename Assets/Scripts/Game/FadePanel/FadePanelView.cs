using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class FadePanelView : MonoBehaviour, IFadePanelView
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private Image tutorialFadeImage;
        
        public Image GetFadeImage()
        {
            return fadeImage;
        }
        
        public Image GetTutorialFadeImage()
        {
            return tutorialFadeImage;
        }
    }

    public interface IFadePanelView
    {
        Image GetFadeImage();
        Image GetTutorialFadeImage();
    }
}
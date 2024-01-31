using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class FadePanelView : MonoBehaviour, IFadePanelView
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private Image tutorialFadeImage;

        public void SetFadeImageStatus(bool status)
        {
            fadeImage.gameObject.SetActive(status);
        }

        public Image GetFadeImage()
        {
            return fadeImage;
        }

        public void SetAlpha(float alpha)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
        
        public void SetTutorialFadeImageStatus(bool status)
        {
            tutorialFadeImage.gameObject.SetActive(status);
        }

        public Image GetTutorialFadeImage()
        {
            return tutorialFadeImage;
        }
        
        public void SetTutorialFadeAlpha(float alpha)
        {
            Color color = tutorialFadeImage.color;
            color.a = alpha;
            tutorialFadeImage.color = color;
        }
    }

    public interface IFadePanelView
    {
        void SetFadeImageStatus(bool status);
        Image GetFadeImage();
        void SetAlpha(float alpha);
        void SetTutorialFadeImageStatus(bool status);
        Image GetTutorialFadeImage();
        void SetTutorialFadeAlpha(float alpha);
    }
}
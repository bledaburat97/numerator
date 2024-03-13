using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class FadePanelController : IFadePanelController
    {
        private IFadePanelView _view;
        private IFadePanelView _nonGlowView;

        public FadePanelController(IFadePanelView view, IFadePanelView nonGlowView)
        {
            _view = view;
            _nonGlowView = nonGlowView;
        }
        
        public void Initialize()
        {
            _view.SetFadeImageStatus(false);
            _view.SetTutorialFadeImageStatus(false);
            _nonGlowView.SetFadeImageStatus(false);
        }

        public void SetFadeImageStatus(bool status)
        {
            _view.SetFadeImageStatus(status);
        }

        public void SetNonGlowFadeImageStatus(bool status)
        {
            _nonGlowView.SetFadeImageStatus(status);
        }

        public Image GetFadeImage()
        {
            return _view.GetFadeImage();
        }

        public void SetFadeImageAlpha(float alpha)
        {
            _view.SetAlpha(alpha);
        }
        
        public void InitMaskSystem(IUnmaskServiceView unmaskService, float fade)
        {
            unmaskService.Init(_view.GetTutorialFadeImage(), Color.black, fade);
        }
        
        public void OpenTutorialFade()
        {
            _view.SetTutorialFadeImageStatus(true);
            _view.SetTutorialFadeAlpha(0f);
        }
        
        public void CloseTutorialFade(float duration)
        {
            _view.AnimateTutorialFade(0f, duration);
            _view.SetTutorialFadeImageStatus(false);
        }
    }

    public interface IFadePanelController
    {
        void Initialize();
        void SetFadeImageStatus(bool status);
        Image GetFadeImage();
        void SetFadeImageAlpha(float alpha);
        void InitMaskSystem(IUnmaskServiceView unmaskService, float fade);
        void OpenTutorialFade();
        void CloseTutorialFade(float duration);
        void SetNonGlowFadeImageStatus(bool status);
    }
}
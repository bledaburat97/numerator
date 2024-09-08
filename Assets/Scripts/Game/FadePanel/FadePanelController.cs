using DG.Tweening;
using UnityEngine;

namespace Scripts
{
    public class FadePanelController : IFadePanelController
    {
        private IFadePanelView _view;

        public FadePanelController(IFadePanelView view)
        {
            _view = view;
        }

        public void SetFadeImageStatus(bool status)
        {
            _view.GetFadeImage().gameObject.SetActive(status);
        }
        
        public void SetTutorialFadeImageStatus(bool status)
        {
            _view.GetTutorialFadeImage().gameObject.SetActive(status);
        }
        
        public Sequence AnimateFade(float finalAlpha, float duration)
        {
            return DOTween.Sequence().Append(_view.GetFadeImage().DOFade(finalAlpha, duration));
        }
        
        public void AnimateTutorialFade(float finalAlpha, float duration)
        {
            DOTween.Sequence().Append(_view.GetTutorialFadeImage().DOFade(finalAlpha, duration));
        }

        public void SetFadeImageAlpha(float alpha)
        {
            Color color = _view.GetFadeImage().color;
            color.a = alpha;
            _view.GetFadeImage().color = color;
        }

        public void SetTutorialFadeImageAlpha(float alpha)
        {
            Color color = _view.GetTutorialFadeImage().color;
            color.a = alpha;
            _view.GetTutorialFadeImage().color = color;
        }
        
        public void InitMaskSystem(IUnmaskServiceView unmaskService, float fade)
        {
            unmaskService.Init(_view.GetTutorialFadeImage(), Color.black, fade);
        }
    }

    public interface IFadePanelController
    {
        void SetFadeImageStatus(bool status);
        void SetTutorialFadeImageStatus(bool status);
        void SetFadeImageAlpha(float alpha);
        void SetTutorialFadeImageAlpha(float alpha);
        void InitMaskSystem(IUnmaskServiceView unmaskService, float fade);
        Sequence AnimateFade(float finalAlpha, float duration);
        void AnimateTutorialFade(float finalAlpha, float duration);
    }
}
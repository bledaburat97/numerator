using UnityEngine.UI;

namespace Scripts
{
    public class FadePanelController : IFadePanelController
    {
        private IFadePanelView _view;
        private IFadePanelView _nonGlowView;
        private IFadePanelView _maskedFadeView;
        
        public void Initialize(IFadePanelView view, IFadePanelView nonGlowView)
        {
            _view = view;
            _nonGlowView = nonGlowView;
            _view.SetFadeImageStatus(false);
            _nonGlowView.SetFadeImageStatus(false);
        }

        public void SetFadeImageStatus(bool status)
        {
            _view.SetFadeImageStatus(status);
            _nonGlowView.SetFadeImageStatus(status);
        }

        public Image GetFadeImage()
        {
            return _view.GetFadeImage();
        }

        public void SetMaskedFadeImageStatus(bool status)
        {
            _maskedFadeView.SetFadeImageStatus(status);
        }

        public Image GetMaskedFadePanelImage()
        {
            return _nonGlowView.GetFadeImage();
        }

        public void SetFadeImageAlpha(float alpha)
        {
            _view.SetAlpha(alpha);
        }
    }

    public interface IFadePanelController
    {
        void Initialize(IFadePanelView view, IFadePanelView maskedView);
        void SetFadeImageStatus(bool status);
        void SetMaskedFadeImageStatus(bool status);
        Image GetMaskedFadePanelImage();
        Image GetFadeImage();
        void SetFadeImageAlpha(float alpha);
    }
}
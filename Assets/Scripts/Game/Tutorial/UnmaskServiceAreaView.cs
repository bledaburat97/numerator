using UnityEngine;

namespace Scripts
{
    public class UnmaskServiceAreaView : MonoBehaviour, IUnmaskServiceAreaView
    {
        private IFadePanelController _fadePanelController;
        [SerializeField] private UnmaskServiceView unmaskServiceView;
        private float _fade = 0.6f;
        
        public void Initialize(IFadePanelController fadePanelController)
        {
            _fadePanelController = fadePanelController;
        }
        
        public void InstantiateTutorialFade()
        {
            _fadePanelController.OpenTutorialFade();
            CreateMaskSystem(0.1f);
        }
        
        private void CreateMaskSystem(float duration)
        {
            _fadePanelController.InitMaskSystem(unmaskServiceView, _fade);
            unmaskServiceView.SetAlpha(_fade, duration);
        }
    }

    public interface IUnmaskServiceAreaView
    {
        void Initialize(IFadePanelController fadePanelController);
        void InstantiateTutorialFade();
    }

}
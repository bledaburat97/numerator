using UnityEngine;

namespace Scripts
{
    public class UnmaskServiceAreaView : MonoBehaviour, IUnmaskServiceAreaView
    {
        private IFadePanelController _fadePanelController;
        [SerializeField] private UnmaskServiceView unmaskServicePrefab;
        private float _fade = 0.6f;
        
        public void Initialize(IFadePanelController fadePanelController)
        {
            _fadePanelController = fadePanelController;
        }
        
        public void InstantiateTutorialFade()
        {
            _fadePanelController.OpenTutorialFade();
            CreateMaskSystem(0f);
        }
        
        private void CreateMaskSystem(float duration)
        {
            UnmaskServiceView unmaskServiceView = Instantiate(unmaskServicePrefab, transform);
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
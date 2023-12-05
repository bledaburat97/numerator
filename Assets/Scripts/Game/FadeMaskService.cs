using UnityEngine;

namespace Scripts
{
    public class FadeMaskService : MonoBehaviour
    {
        [SerializeField] private UnmaskImage unmaskImage;
        [SerializeField] private MaskSystem maskSystemPrefab;
        private IMaskSystem _maskSystem;
        private IFadePanelController _fadePanelController;

        public void Initialize(IFadePanelController fadePanelController)
        {
            _fadePanelController = fadePanelController;
        }

        public void CreateMask()
        {
            _fadePanelController.SetMaskedFadeImageStatus(true);
            if (_maskSystem == null)
            {
                CreateMaskSystem();
            }
        }
        
        private void CreateMaskSystem()
        {
            _maskSystem = Instantiate(maskSystemPrefab, transform);
            _maskSystem.Init(_fadePanelController.GetMaskedFadePanelImage());
            //_maskSystem.SetAlpha(_fade, duration);
        }
    }
}
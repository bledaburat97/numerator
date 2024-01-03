using UnityEngine;
using UnityEngine.Serialization;

namespace Scripts
{
    public class FadeMaskService : MonoBehaviour
    {
        [FormerlySerializedAs("unmaskImage")] [SerializeField] private UnmaskItemView unmaskItemView;
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
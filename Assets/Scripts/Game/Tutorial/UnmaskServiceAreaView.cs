using UnityEngine;

namespace Scripts
{
    public class UnmaskServiceAreaView : MonoBehaviour, IUnmaskServiceAreaView
    {
        private IFadePanelController _fadePanelController;
        [SerializeField] private UnmaskServiceView unmaskServicePrefab;
        private float _fade = 0.9f;
        private IUnmaskServiceView _unmaskServiceView;
        
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
            _unmaskServiceView = Instantiate(unmaskServicePrefab, transform);
            _fadePanelController.InitMaskSystem(_unmaskServiceView, _fade);
            _unmaskServiceView.SetAlpha(_fade, duration);
        }

        public void CreateUnmaskCardItem(Vector2 position, Vector2 size)
        {
            _unmaskServiceView.CreateUnmaskCardItem(position, size);
        }

        public void ClearUnmaskCardItems()
        {
            _unmaskServiceView.ClearUnmaskCardItems();
        }

        public void ChangeLocalPositionOfUnmaskCardItem(Vector2 changeInLocalPos)
        {
            _unmaskServiceView.ChangeLocalPositionOfUnmaskCardItem(changeInLocalPos);
        }
    }

    public interface IUnmaskServiceAreaView
    {
        void Initialize(IFadePanelController fadePanelController);
        void InstantiateTutorialFade();
        void CreateUnmaskCardItem(Vector2 position, Vector2 size);
        void ClearUnmaskCardItems();
        void ChangeLocalPositionOfUnmaskCardItem(Vector2 changeInLocalPos);
    }

}
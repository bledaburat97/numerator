using UnityEngine;

namespace Scripts
{
    public class UnmaskServiceAreaView : MonoBehaviour, IUnmaskServiceAreaView
    {
        private IFadePanelController _fadePanelController;
        [SerializeField] private UnmaskServiceView unmaskServicePrefab;
        private float _fade = 0.5f;
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

        public void CloseTutorialFade()
        {
            _fadePanelController.CloseTutorialFade();
        }
        
        private void CreateMaskSystem(float duration)
        {
            _unmaskServiceView = Instantiate(unmaskServicePrefab, transform);
            _fadePanelController.InitMaskSystem(_unmaskServiceView, _fade);
            _unmaskServiceView.SetAlpha(_fade, duration);
        }

        public void CreateUnmaskCardItem(Vector2 position, Vector2 size, float pixelPerUnit = 230f)
        {
            _unmaskServiceView.CreateUnmaskCardItem(position, size, pixelPerUnit);
        }
        
        public void ClearAllUnmaskCardItems()
        {
            _unmaskServiceView.ClearAllUnmaskCardItems();
        }

        public void ClearUnmaskCardItem(int index)
        {
            _unmaskServiceView.ClearUnmaskCardItem(index);
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
        void CloseTutorialFade();
        void CreateUnmaskCardItem(Vector2 position, Vector2 size, float pixelPerUnit = 230f);
        void ClearAllUnmaskCardItems();
        void ClearUnmaskCardItem(int index);
        void ChangeLocalPositionOfUnmaskCardItem(Vector2 changeInLocalPos);
    }

}
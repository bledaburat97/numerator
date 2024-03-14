using UnityEngine;

namespace Scripts
{
    public class UnmaskServiceAreaView : MonoBehaviour, IUnmaskServiceAreaView
    {
        private IFadePanelController _fadePanelController;
        [SerializeField] private UnmaskServiceView unmaskServicePrefab;
        private float _fade = 0.5f;
        private float _fadeDuration = 0.4f;
        private IUnmaskServiceView _unmaskServiceView;
        private float _anchorMaxYOfSafeArea;
        private float _heightOfCanvas;
        public void Initialize(IFadePanelController fadePanelController)
        {
            _fadePanelController = fadePanelController;
        }

        public void Init(float anchorMaxYOfSafeArea, float heightOfCanvas)
        {
            _anchorMaxYOfSafeArea = anchorMaxYOfSafeArea;
            _heightOfCanvas = heightOfCanvas;
        }
        
        public void InstantiateTutorialFade()
        {
            _fadePanelController.OpenTutorialFade();
            CreateMaskSystem(_fadeDuration);
        }

        public void CloseTutorialFade()
        {
            _fadePanelController.CloseTutorialFade(_fadeDuration);
        }
        
        private void CreateMaskSystem(float duration)
        {
            _unmaskServiceView = Instantiate(unmaskServicePrefab, transform);
            _fadePanelController.InitMaskSystem(_unmaskServiceView, _fade);
            _unmaskServiceView.SetAlpha(_fade, duration);
        }

        public void CreateUnmaskCardItem(Vector2 position, Vector2 size, float changeInLocalPosY = 0f, float pixelPerUnit = 230f)
        {
            _unmaskServiceView.CreateUnmaskCardItem(position, size, _anchorMaxYOfSafeArea, _heightOfCanvas, pixelPerUnit, changeInLocalPosY);
        }
        
        public void ClearAllUnmaskCardItems()
        {
            _unmaskServiceView.ClearAllUnmaskCardItems();
        }

        public void ClearUnmaskCardItem(int index)
        {
            _unmaskServiceView.ClearUnmaskCardItem(index);
        }
    }

    public interface IUnmaskServiceAreaView
    {
        void Initialize(IFadePanelController fadePanelController);
        void Init(float anchorMaxYOfSafeArea, float heightOfCanvas);
        void InstantiateTutorialFade();
        void CloseTutorialFade();
        void CreateUnmaskCardItem(Vector2 position, Vector2 size, float changeInLocalPosY = 0f, float pixelPerUnit = 230f);
        void ClearAllUnmaskCardItems();
        void ClearUnmaskCardItem(int index);
    }

}
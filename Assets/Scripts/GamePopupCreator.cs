using UnityEngine;

namespace Scripts
{
    public class GamePopupCreator : MonoBehaviour
    {
        [SerializeField] private LevelEndPopupView levelEndPopupPrefab;
        private LevelEndPopupControllerFactory _levelEndPopupControllerFactory;
        private LevelEndPopupViewFactory _levelEndPopupViewFactory;
        private IFadePanelController _fadePanelController;
        
        public void Initialize(IResultManager resultManager, IFadePanelController fadePanelController)
        {
            _levelEndPopupControllerFactory = new LevelEndPopupControllerFactory();
            _levelEndPopupViewFactory = new LevelEndPopupViewFactory();
            _fadePanelController = fadePanelController;
            resultManager.LevelEnd += CreateLevelEndPopup;
        }
        
        private void CreateLevelEndPopup(object sender, LevelEndEventArgs args)
        {
            _fadePanelController.SetFadeImageStatus(true);
            ILevelEndPopupController levelEndPopupController = _levelEndPopupControllerFactory.Spawn();
            ILevelEndPopupView levelEndPopupView =
                _levelEndPopupViewFactory.Spawn(transform, levelEndPopupPrefab);
            levelEndPopupController.Initialize(levelEndPopupView, args);
        }
    }
}
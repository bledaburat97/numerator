using UnityEngine;
using Zenject;

namespace Scripts
{
    public class WaitingSceneContext : MonoBehaviour
    {
        [Inject] private IReadyButtonController _readyButtonController;
        [Inject] private IWaitingSceneUIController _waitingSceneUIController;
        [Inject] private IPlayerNameAreaController _playerNameAreaController;
        [Inject] private IWaitingScenePopupCreator _waitingScenePopupCreator;
        void Start()
        {
            CreateButtons();
            InitializeWaitingSceneUI();
            InitializePlayerNameArea();
            InitializeWaitingScenePopupCreator();
        }
        
        private void CreateButtons()
        {
            _readyButtonController.Initialize();
        }

        private void InitializeWaitingSceneUI()
        {
            _waitingSceneUIController.Initialize();
        }

        private void InitializePlayerNameArea()
        {
            _playerNameAreaController.Initialize();
        }
        
        private void InitializeWaitingScenePopupCreator()
        {
            _waitingScenePopupCreator.Initialize();
        }
    }
}
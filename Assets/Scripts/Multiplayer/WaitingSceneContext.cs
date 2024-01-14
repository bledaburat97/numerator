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
        [Inject] private IUserReady _userReady;
        void Start()
        {
            InitializeUserReady();
            CreateButtons();
            InitializeWaitingSceneUI();
            InitializePlayerNameArea();
            InitializeWaitingScenePopupCreator();
        }

        private void InitializeUserReady()
        {
            _userReady.Initialize();
        }
        
        private void CreateButtons()
        {
            _readyButtonController.Initialize(_userReady);
        }

        private void InitializeWaitingSceneUI()
        {
            _waitingSceneUIController.Initialize();
        }

        private void InitializePlayerNameArea()
        {
            _playerNameAreaController.Initialize(_userReady);
        }
        
        private void InitializeWaitingScenePopupCreator()
        {
            _waitingScenePopupCreator.Initialize();
        }
    }
}
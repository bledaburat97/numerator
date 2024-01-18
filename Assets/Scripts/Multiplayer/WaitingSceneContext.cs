using UnityEngine;
using Zenject;

namespace Scripts
{
    public class WaitingSceneContext : MonoBehaviour
    {
        [Inject] private IWaitingSceneUIController _waitingSceneUIController;
        [Inject] private IPlayerNameAreaController _playerNameAreaController;
        [Inject] private IWaitingScenePopupCreator _waitingScenePopupCreator;
        [Inject] private IUserReady _userReady;
        void Start()
        {
            InitializeUserReady();
            InitializeWaitingSceneUI();
            InitializePlayerNameArea();
            InitializeWaitingScenePopupCreator();
        }

        private void InitializeUserReady()
        {
            _userReady.Initialize();
        }

        private void InitializeWaitingSceneUI()
        {
            _waitingSceneUIController.Initialize(_userReady);
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
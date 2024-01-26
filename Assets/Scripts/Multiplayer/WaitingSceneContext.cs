using Unity.Netcode;
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
        [Inject] private IHapticController _hapticController;

        void Start()
        {
            InitializeHapticController();
            InitializeUserReady();
            InitializeWaitingSceneUI();
            InitializePlayerNameArea();
            InitializeWaitingScenePopupCreator();
        }
        
        private void InitializeHapticController() //TODO: set in global installer
        {
            _hapticController.Initialize();
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
        
#if UNITY_EDITOR
        private void OnApplicationFocus(bool pauseStatus)
        {
            pauseStatus = !pauseStatus;
#else
        private void OnApplicationPause(bool pauseStatus)
        {
#endif
            if (pauseStatus)
            {
                if(NetworkManager.Singleton != null)
                {
                    Destroy(NetworkManager.Singleton);
                }
            }
        }
        
        private void OnApplicationQuit()
        {
            if(NetworkManager.Singleton != null)
            {
                Destroy(NetworkManager.Singleton);
            }
        }
    }
}
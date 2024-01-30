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
        [Inject] private IFadePanelController _fadePanelController;

        void Start()
        {
            InitializeHapticController();
            InitializeUserReady();
            InitializeWaitingSceneUI();
            InitializePlayerNameArea();
            InitializeFadePanelController();
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
        
        private void InitializeFadePanelController()
        {
            _fadePanelController.Initialize();
        }
        
        private void InitializeWaitingScenePopupCreator()
        {
            _waitingScenePopupCreator.Initialize(_fadePanelController);
        }
        
        private void OnApplicationQuit()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
#else
#endif
        }
    }
}
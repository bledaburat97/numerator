using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class LobbySceneContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ILobbyPopupCreator _lobbyPopupCreator;
        [Inject] private ILobbyUIController _lobbyUIController;
        void Start()
        {
            _levelTracker.Initialize(null);
            InitializeLobbyPopupCreator();
            CreateLobbyUIController();
        }
        
        private void InitializeLobbyPopupCreator()
        {
            _lobbyPopupCreator.Initialize(_levelTracker);
        }
        
        private void CreateLobbyUIController()
        {
            _lobbyUIController.Initialize(_lobbyPopupCreator);
        }
    }
}
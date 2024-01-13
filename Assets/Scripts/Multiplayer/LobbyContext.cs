using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Scripts
{
    public class LobbyContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ILobbyPopupCreator _lobbyPopupCreator;
        [Inject] private ILobbyUIController _lobbyUIController;
        void Start()
        {
            _levelTracker.Initialize(null);
            InitializeLobbyPopupCreator();
            CreateLobbyUIController();
            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
        }
        
        private void InitializeLobbyPopupCreator()
        {
            _lobbyPopupCreator.Initialize();
        }
        
        private void CreateLobbyUIController()
        {
            _lobbyUIController.Initialize(_lobbyPopupCreator);
        }
    }
}
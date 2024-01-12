using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LobbyContext : MonoBehaviour
    {
        [Inject] private ILobbyPopupCreator _lobbyPopupCreator;
        [Inject] private ILobbyUIController _lobbyUIController;
        void Start()
        {
            InitializeLobbyPopupCreator();
            CreateLobbyUIController();
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
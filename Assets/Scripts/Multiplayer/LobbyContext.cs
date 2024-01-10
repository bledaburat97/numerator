using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LobbyContext : MonoBehaviour
    {
        [Inject] private ICreateGameButtonController _createGameButtonController;
        [Inject] private IJoinGameButtonController _joinGameButtonController;
        [Inject] private ILobbyPopupCreator _lobbyPopupCreator;
        void Start()
        {
            CreatePlayButtons();
            InitializeLobbyPopupCreator();
        }
        
        private void CreatePlayButtons()
        {
            _createGameButtonController.Initialize();
            _joinGameButtonController.Initialize();
        }

        private void InitializeLobbyPopupCreator()
        {
            _lobbyPopupCreator.Initialize();
        }
    }
}
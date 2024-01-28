﻿using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class LobbySceneContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private ILobbyPopupCreator _lobbyPopupCreator;
        [Inject] private ILobbyUIController _lobbyUIController;
        [Inject] private IHapticController _hapticController;

        void Start()
        {
            _levelTracker.Initialize(null);
            InitializeHapticController();
            InitializeLobbyPopupCreator();
            CreateLobbyUIController();
        }
        
        private void InitializeHapticController() //TODO: set in global installer
        {
            _hapticController.Initialize();
        }
        
        private void InitializeLobbyPopupCreator()
        {
            _lobbyPopupCreator.Initialize(_levelTracker);
        }
        
        private void CreateLobbyUIController()
        {
            _lobbyUIController.Initialize(_lobbyPopupCreator);
        }
        
        private void OnApplicationQuit()
        {
            if (NetworkManager.Singleton != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
                
            if (MultiplayerManager.Instance != null)
            {
                Destroy(MultiplayerManager.Instance.gameObject);
            }

            if (PlayerLobby.Instance != null)
            {
                Destroy(PlayerLobby.Instance.gameObject);
            }
        }
    }
}
﻿using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public class LoadingSceneContext : MonoBehaviour
    {
        void Start()
        {
            SceneManager.LoadScene("Game");
            //NetworkManager.Singleton.StartHost();
            //NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
    }
}
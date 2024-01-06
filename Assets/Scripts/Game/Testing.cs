using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    public class Testing : MonoBehaviour
    {
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;

        private void Awake()
        {
            hostButton.onClick.AddListener(() =>
            {
                Debug.Log("HOST");
                NetworkManager.Singleton.StartHost();
                Hide();
            });
            clientButton.onClick.AddListener(() =>
            {
                Debug.Log("CLIENT");
                NetworkManager.Singleton.StartClient();
                Hide();
            });
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
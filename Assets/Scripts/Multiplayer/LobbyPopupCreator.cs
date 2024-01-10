using UnityEngine;

namespace Scripts
{
    public class LobbyPopupCreator : MonoBehaviour, ILobbyPopupCreator
    {
        [SerializeField] private ConnectionResponsePopupView connectionResponsePopupPrefab;
        [SerializeField] private ConnectingPopupView connectingPopupPrefab;
        
        public void Initialize()
        {
            connectionResponsePopupPrefab.Init();
            connectingPopupPrefab.Init();
        }
    }

    public interface ILobbyPopupCreator
    {
        void Initialize();
    }
}
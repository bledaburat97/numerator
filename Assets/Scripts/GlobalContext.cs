using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GlobalContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        private void OnApplicationQuit()
        {
            _levelTracker.SavePlayerPrefs();
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
#else
#endif
        }
    }
}
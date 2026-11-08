using UnityEngine;
using Zenject;
using Unity.Netcode;

namespace Scripts
{
    public class GlobalContext : MonoBehaviour
    {
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private Economy.GardenVisitCoordinator _gardenVisits;
        private void Start()
        {
            _gardenVisits.Failed+=OnGardenFailure;
            _gardenVisits.Initialize();
        }
        private void OnApplicationPause(bool paused)=>_gardenVisits?.SetPaused(paused);
        private void OnApplicationFocus(bool focused)=>_gardenVisits?.SetFocused(focused);
        private void OnGardenFailure(System.Exception error)=>Debug.LogWarning("Garden visit deferred: "+error.GetType().Name);
        private void OnDestroy() { if(_gardenVisits!=null)_gardenVisits.Failed-=OnGardenFailure; }
        private void OnApplicationQuit()
        {
            _levelTracker?.SavePlayerPrefs();
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (_levelTracker != null && _levelTracker.GetGameOption() == GameOption.MultiPlayer)
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

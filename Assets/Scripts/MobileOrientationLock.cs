using UnityEngine;

namespace Scripts
{
    public static class MobileOrientationLock
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Apply()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToPortrait = true;
            Screen.orientation = ScreenOrientation.Portrait;
#endif
        }
    }
}

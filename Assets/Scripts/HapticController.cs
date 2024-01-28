using System.Collections.Generic;
using UnityEngine;

namespace Scripts
{
    public class HapticController : IHapticController
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

        private Dictionary<HapticType, ImpactFeedbackStyle> _vibrationTypeDict = new();
        private ImpactFeedbackStyle _defaultVibrationType = ImpactFeedbackStyle.Light;
        private Dictionary<ImpactFeedbackStyle, int> _vibrationToIntDict = new();
#else
#endif
        public void Initialize()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

            Debug.Log("initalize haptic");
            _vibrationTypeDict = new Dictionary<HapticType, ImpactFeedbackStyle>()
            {
                { HapticType.CardGrab, ImpactFeedbackStyle.Medium },
                { HapticType.CardRelease, ImpactFeedbackStyle.Heavy },
                { HapticType.ButtonClick, ImpactFeedbackStyle.Light },
                { HapticType.Success, ImpactFeedbackStyle.Medium },
                { HapticType.Failure, ImpactFeedbackStyle.Medium },
                { HapticType.Warning , ImpactFeedbackStyle.Heavy}
            };

            _vibrationToIntDict = new Dictionary<ImpactFeedbackStyle, int>()
            {
                { ImpactFeedbackStyle.Light, 50 },
                { ImpactFeedbackStyle.Medium, 75 },
                { ImpactFeedbackStyle.Heavy, 100 }
            };
            
            Vibration.Init();
#else
#endif
        }

        public void Vibrate(HapticType hapticType)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!_vibrationTypeDict.TryGetValue(hapticType, out ImpactFeedbackStyle vibrationType))
            {
                Vibration.VibrateAndroid(_vibrationToIntDict[_defaultVibrationType]);
            }
            else
            {
                Vibration.VibrateAndroid(_vibrationToIntDict[vibrationType]);
            }
            
#elif UNITY_IOS && !UNITY_EDITOR
            if (!_vibrationTypeDict.TryGetValue(hapticType, out ImpactFeedbackStyle vibrationType))
            {
                Vibration.VibrateIOS(_defaultVibrationType);
            }
            else
            {
                Vibration.VibrateIOS(vibrationType);
            }
#else 
#endif
        }
    }

    public interface IHapticController
    {
        void Initialize();
        void Vibrate(HapticType hapticType);
    }

    public enum HapticType
    {
        CardGrab = 1,
        CardRelease = 2,
        ButtonClick = 3,
        Success = 4,
        Warning = 5,
        Failure = 6,
    }
}
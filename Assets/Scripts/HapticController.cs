using System.Collections.Generic;

namespace Scripts
{
    public class HapticController : IHapticController
    {
        private Dictionary<HapticType, VibrationType> _vibrationTypeDict = new();
        private VibrationType _defaultVibrationType = VibrationType.LightImpact;
        
        public void Initialize()
        {
            _vibrationTypeDict = new Dictionary<HapticType, VibrationType>()
            {
                { HapticType.CardGrab, VibrationType.LightImpact },
                { HapticType.CardRelease, VibrationType.HeavyImpact },
                { HapticType.ButtonClick, VibrationType.MediumImpact },
                { HapticType.Success, VibrationType.Success },
                { HapticType.Failure, VibrationType.Failure },
                { HapticType.Warning , VibrationType.Warning}
            };
            VibrationManager.Init();
        }

        public void Vibrate(HapticType hapticType)
        {
            if (!_vibrationTypeDict.TryGetValue(hapticType, out VibrationType vibrationType))
            {
                VibrationManager.Vibrate(_defaultVibrationType);
            }
            else
            {
                VibrationManager.Vibrate(vibrationType);
            }
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
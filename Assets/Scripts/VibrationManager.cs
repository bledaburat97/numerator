using System;
using System.Collections;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.iOS;
#endif

namespace Scripts{
    public class VibrationManager : MonoBehaviour
    {
#if !UNITY_EDITOR
		private static bool _HapticAvailable;
#else
#endif
        private static bool _IsInitialized;

        public static void Init()
        {
            if (_IsInitialized) return;
            _IsInitialized = true;
#if UNITY_IOS && !UNITY_EDITOR
			_HapticAvailable = (int) Device.generation > 30;
			InstantiateFeedbackGenerators();
#elif UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
			using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
				int ver = version.GetStatic<int>("SDK_INT");
				if (ver > 25) {
					_HapticAvailable = true;
					VibrationManagerClass.CallStatic("InitWithAmplitude", context);
				}else{
					VibrationManagerClass.CallStatic("Init", context);
				}
			}

			if (CoroutineExecutor == null)
			{
				GameObject go = new GameObject("VibrationManager");
				VibrationManager mono = go.AddComponent<VibrationManager>();
				DontDestroyOnLoad(go);
				CoroutineExecutor = mono;
			}
#else
#endif
        }

        public static void Deactivate()
        {
            if (!_IsInitialized) return;
            _IsInitialized = false;
#if UNITY_ANDROID && !UNITY_EDITOR
			VibrationManagerClass.CallStatic("Stop");
#elif UNITY_IOS && !UNITY_EDITOR
			ReleaseFeedbackGenerators();
#else
#endif
        }

        public static void Vibrate(VibrationType type)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			if(!_IsInitialized) return;
			AndroidVibrate(type.Pattern, type.Amplitude, type.TotalDuration, type.AdditionRate);
            Debug.Log("Vibrate: " + type);
#elif UNITY_IOS && !UNITY_EDITOR
			if(!_IsInitialized) return;
			IOSVibrate(type.VibrationExtern);
            Debug.Log("Vibrate: " + type);
#else
#endif
        }

        public static void ForceVibrate(VibrationType type)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			if(!_IsInitialized) return;
			AndroidForceVibrate(type.Pattern, type.Amplitude, type.TotalDuration);
#elif UNITY_IOS && !UNITY_EDITOR
			if(!_IsInitialized) return;
			IOSForceVibrate(type.VibrationExtern);
#else
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
		private static readonly AndroidJavaClass VibrationManagerClass =
 new AndroidJavaClass("com.fugo.hypervibes.VibrationManager");
		private static float LastVibrationStartTime = Time.realtimeSinceStartup;
		private static float LastVibrationDuration = 0;
		private static MonoBehaviour CoroutineExecutor;
		private static Coroutine LateVibrationCoroutine = null;
		private static VibrationType QueuedVibration = new VibrationType
		{
			Pattern = new[] {0, (long)0},
			Amplitude = new[] {0, 0},
			TotalDuration = 0
		};

		private static void AndroidVibrate(long[] pattern, int[] amplitudes, float duration, float additionRate)
		{
			if (!_HapticAvailable) return;

			if (LateVibrationCoroutine == null)
			{
				LateVibrationCoroutine = CoroutineExecutor.StartCoroutine(QueuedVibrationCoroutine());
				AddToQueue(pattern, amplitudes, duration, 1);
			}
			else
			{
				AddToQueue(pattern, amplitudes, duration, additionRate);
			}
		}

		private static void AndroidForceVibrate(long[] pattern, int[] amplitudes, float duration)
		{
			LastVibrationStartTime = Time.realtimeSinceStartup;
			LastVibrationDuration = duration;
			if (!_HapticAvailable)
				VibrationManagerClass.CallStatic("Vibrate", pattern, -1);
			else
				VibrationManagerClass.CallStatic("Vibrate", pattern, amplitudes, -1);
		}

		private static void AddToQueue(long[] pattern, int[] amplitudes, float duration, float additionRate)
		{
			if (QueuedVibration.Pattern.Length >= pattern.Length)
			{
				for (int i = 0; i < pattern.Length; i++)
				{
					QueuedVibration.Pattern[i] =
 Math.Min(QueuedVibration.Pattern[i] + (long)(pattern[i] * additionRate), 80);
					QueuedVibration.Amplitude[i] =
 Math.Min(QueuedVibration.Amplitude[i] + (int)(amplitudes[i] * additionRate), 255);
				}
			}
			else
			{
				for (int i = 0; i < QueuedVibration.Pattern.Length; i++)
				{
					pattern[i] = Math.Min(QueuedVibration.Pattern[i] + (long)(pattern[i] * additionRate), 80);
					amplitudes[i] = Math.Min(QueuedVibration.Amplitude[i] + (int)(amplitudes[i] * additionRate), 255);
				}

				QueuedVibration.Pattern = pattern;
				QueuedVibration.Amplitude = amplitudes;
			}
			
			float totalDuration = 0;
			for (int i = 0; i < QueuedVibration.Pattern.Length; i++)
			{
				totalDuration += QueuedVibration.Pattern[i];
			}
			QueuedVibration.TotalDuration = totalDuration / 1000;
		}

		private static IEnumerator QueuedVibrationCoroutine()
		{
			float regularWaitTime = LastVibrationStartTime + LastVibrationDuration - Time.realtimeSinceStartup;
			yield return new WaitForSecondsRealtime(Mathf.Max(regularWaitTime, 0) + 0.05f);
			VibrationManagerClass.CallStatic("Vibrate", QueuedVibration.Pattern, QueuedVibration.Amplitude, -1);
			LastVibrationStartTime = Time.realtimeSinceStartup;
			LastVibrationDuration = QueuedVibration.TotalDuration;
			QueuedVibration.Pattern = new[] {0, (long)0};
			QueuedVibration.Amplitude = new[] {0, 0};
			QueuedVibration.TotalDuration = 0;
			LateVibrationCoroutine = null;
		}
#elif UNITY_IOS && !UNITY_EDITOR
		private static void IOSVibrate(Action nativeHapticMethod)
		{
			if (!_HapticAvailable) return;
			nativeHapticMethod?.Invoke();
		}

		private static void IOSForceVibrate(Action nativeHapticMethod)
		{
			if (!_HapticAvailable)
				Handheld.Vibrate();
			else
				nativeHapticMethod?.Invoke();
		}

		[DllImport ("__Internal")]
		private static extern void InstantiateFeedbackGenerators();
		[DllImport ("__Internal")]
		private static extern void ReleaseFeedbackGenerators();

#endif
    }

    public class VibrationType
    {
#if UNITY_ANDROID && !UNITY_EDITOR
			public long[] Pattern;
			public int[] Amplitude;
			public float AdditionRate;
			public float TotalDuration;
#elif UNITY_IOS && !UNITY_EDITOR
			public Action VibrationExtern;
#else
        public string Name;
#endif


#if UNITY_ANDROID && !UNITY_EDITOR
		private const long LightDuration = 20;
		private const long MediumDuration = 40;
		private const long HeavyDuration = 80;
		private const int LightAmplitude = 40;
		private const int MediumAmplitude = 120;
		private const int HeavyAmplitude = 255;
#endif

        public static readonly VibrationType LightImpact = new VibrationType
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Pattern = new[] {0, LightDuration},
			Amplitude = new[] {0, LightAmplitude},
			AdditionRate = 0.06f,
			TotalDuration = 0.020f
#elif UNITY_IOS && !UNITY_EDITOR
			VibrationExtern = LightImpactHaptic
#else
            Name = "Light Impact"
#endif
        };

        public static readonly VibrationType MediumImpact = new VibrationType
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Pattern = new[] {0, MediumDuration},
			Amplitude = new[] {0, MediumAmplitude},
			AdditionRate = 0.1f,
			TotalDuration = 0.040f
#elif UNITY_IOS && !UNITY_EDITOR
			VibrationExtern = MediumImpactHaptic
#else
            Name = "Medium Impact"
#endif
        };

        public static readonly VibrationType HeavyImpact = new VibrationType
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Pattern = new[] {0, HeavyDuration},
			Amplitude = new[] {0, HeavyAmplitude},
			AdditionRate = 1,
			TotalDuration = 0.080f
#elif UNITY_IOS && !UNITY_EDITOR
			VibrationExtern = HeavyImpactHaptic
#else
            Name = "Heavy Impact"
#endif
        };

        public static readonly VibrationType Selection = new VibrationType
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Pattern = new[] {0, MediumDuration},
			Amplitude = new[] {0, MediumAmplitude},
			AdditionRate = 1,
			TotalDuration = 0.040f
#elif UNITY_IOS && !UNITY_EDITOR
			VibrationExtern = SelectionHaptic
#else
            Name = "Light Impact"
#endif
        };

        public static readonly VibrationType Success = new VibrationType
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Pattern = new[] {0, LightDuration, LightDuration, HeavyDuration},
			Amplitude = new[] {0, LightAmplitude, 0, HeavyAmplitude},
			AdditionRate = 1,
			TotalDuration = 0.120f
#elif UNITY_IOS && !UNITY_EDITOR
			VibrationExtern = SuccessHaptic
#else
            Name = "Success Sequence"
#endif
        };

        public static readonly VibrationType Warning = new VibrationType
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Pattern = new[] {0, HeavyDuration, LightDuration, MediumDuration},
			Amplitude = new[] {0, HeavyAmplitude, 0, MediumAmplitude},
			AdditionRate = 1,
			TotalDuration = 0.140f
#elif UNITY_IOS && !UNITY_EDITOR
			VibrationExtern = WarningHaptic
#else
            Name = "Warning Sequence"
#endif
        };

        public static readonly VibrationType Failure = new VibrationType
        {
#if UNITY_ANDROID && !UNITY_EDITOR
			Pattern = new[]
			{
				0, MediumDuration, LightDuration, MediumDuration, LightDuration, HeavyDuration, LightDuration, LightDuration
			},
			Amplitude = new[] {0, MediumAmplitude, 0, MediumAmplitude, 0, HeavyAmplitude, 0, LightAmplitude},
			AdditionRate = 1,
			TotalDuration = 0.240f
#elif UNITY_IOS && !UNITY_EDITOR
			VibrationExtern = FailureHaptic
#else
            Name = "Failure Sequence"
#endif
        };

#if UNITY_IOS && !UNITY_EDITOR
		[DllImport ("__Internal")]
		private static extern void SelectionHaptic();
		[DllImport ("__Internal")]
		private static extern void SuccessHaptic();
		[DllImport ("__Internal")]
		private static extern void WarningHaptic();
		[DllImport ("__Internal")]
		private static extern void FailureHaptic();
		[DllImport ("__Internal")]
		private static extern void LightImpactHaptic();
		[DllImport ("__Internal")]
		private static extern void MediumImpactHaptic();
		[DllImport ("__Internal")]
		private static extern void HeavyImpactHaptic();
#endif
    }
}

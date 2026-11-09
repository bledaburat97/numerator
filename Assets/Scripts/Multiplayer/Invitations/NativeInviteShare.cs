using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Scripts
{
    public static class NativeInviteShare
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void FruitFriendsShare(string text);
#endif
        public static bool Share(string hostName, string url, string code)
        {
            string text = hostName + " seni birlikte oynamaya davet ediyor 🌿\n" + url + "\n\nOda kodu: " + code;
#if UNITY_IOS && !UNITY_EDITOR
            FruitFriendsShare(text); return true;
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
                {
                    intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text).Dispose();
                    using (var chooser = intent.CallStatic<AndroidJavaObject>("createChooser", intent, "Davet et"))
                        activity.Call("startActivity", chooser);
                }
                return true;
            }
            catch (Exception error) { Debug.LogWarning("Invite share unavailable: " + error.GetType().Name); GUIUtility.systemCopyBuffer = text; return false; }
#else
            GUIUtility.systemCopyBuffer = text; return false;
#endif
        }
    }
}

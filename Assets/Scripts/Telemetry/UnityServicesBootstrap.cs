using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

namespace Scripts
{
    // Analytics and multiplayer share one UGS initialization and environment.
    public static class UnityServicesBootstrap
    {
        private static Task _initialization;
        public static Task InitializeAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized) return Task.CompletedTask;
            if (_initialization == null || _initialization.IsCompleted)
            {
                var options = new InitializationOptions().SetEnvironmentName("production");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (MultiplayerNetworkProbe.IsRunning && !string.IsNullOrEmpty(MultiplayerNetworkProbe.AuthenticationProfile))
                    options.SetProfile(MultiplayerNetworkProbe.AuthenticationProfile);
#endif
                _initialization = UnityServices.InitializeAsync(options);
            }
            return _initialization;
        }
    }
}

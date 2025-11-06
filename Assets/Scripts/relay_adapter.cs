using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public static class RelayAdapter
{
    public static bool IsAvailable
    {
        get
        {
#if UNITY_RELAY
            return true;
#else
            return false;
#endif
        }
    }

    public static async Task<(bool ok, string joinCode, string error)> StartHostAsync(NetworkManager networkManager, UnityTransport transport, int maxConnections = 16)
    {
#if UNITY_RELAY
        try
        {
            await EnsureUnityServicesAsync();

            var alloc = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxConnections);
            var code = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            var protocol = Application.platform == RuntimePlatform.WebGLPlayer ? "wss" : "dtls";
            var relayData = new Unity.Networking.Transport.Relay.RelayServerData(alloc, protocol);
            transport.SetRelayServerData(relayData);

            bool started = networkManager.StartHost();
            return (started, code, started ? null : "NetworkManager.StartHost failed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RelayAdapter] StartHostAsync failed: {ex.Message}");
            return (false, null, ex.Message);
        }
#else
        Debug.LogWarning("[RelayAdapter] UNITY_RELAY not defined. Relay is not available in this build.");
        return (false, null, "Relay not available");
#endif
    }

    public static async Task<(bool ok, string error)> JoinAsync(NetworkManager networkManager, UnityTransport transport, string joinCode)
    {
#if UNITY_RELAY
        try
        {
            await EnsureUnityServicesAsync();

            var joinAlloc = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
            var protocol = Application.platform == RuntimePlatform.WebGLPlayer ? "wss" : "dtls";
            var relayData = new Unity.Networking.Transport.Relay.RelayServerData(joinAlloc, protocol);
            transport.SetRelayServerData(relayData);

            bool started = networkManager.StartClient();
            return (started, started ? null : "NetworkManager.StartClient failed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RelayAdapter] JoinAsync failed: {ex.Message}");
            return (false, ex.Message);
        }
#else
        Debug.LogWarning("[RelayAdapter] UNITY_RELAY not defined. Relay is not available in this build.");
        return (false, "Relay not available");
#endif
    }

#if UNITY_RELAY
    private static bool _servicesReady;
    private static async Task EnsureUnityServicesAsync()
    {
        if (_servicesReady)
            return;

        if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Uninitialized)
        {
            await Unity.Services.Core.UnityServices.InitializeAsync();
        }

        if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
        {
            await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        _servicesReady = true;
    }
#endif
}


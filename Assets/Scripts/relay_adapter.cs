using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#if UNITY_RELAY || UNITY_MULTIPLAYER
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif

public static class RelayAdapter
{
    public static bool IsAvailable
    {
        get
        {
#if UNITY_RELAY || UNITY_MULTIPLAYER
            return true;
#else
            return false;
#endif
        }
    }

    public static async Task<(bool ok, string joinCode, string error)> StartHostAsync(NetworkManager networkManager, UnityTransport transport, int maxConnections = 16)
    {
#if UNITY_RELAY || UNITY_MULTIPLAYER
        try
        {
            await EnsureUnityServicesAsync();

            var alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            var code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            // Configure UnityTransport for Relay (secure: DTLS/WSS)
            transport.SetRelayServerData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData,
                true);

            bool started = networkManager.StartHost();
            return (started, code, started ? null : "NetworkManager.StartHost failed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RelayAdapter] StartHostAsync failed: {ex.Message}");
            return (false, null, ex.Message);
        }
#else
        Debug.LogWarning("[RelayAdapter] UNITY_MULTIPLAYER/UNITY_RELAY not defined. Relay is not available in this build.");
        await System.Threading.Tasks.Task.Yield();
        return (false, null, "Relay not available");
#endif
    }

    public static async Task<(bool ok, string error)> JoinAsync(NetworkManager networkManager, UnityTransport transport, string joinCode)
    {
#if UNITY_RELAY || UNITY_MULTIPLAYER
        try
        {
            await EnsureUnityServicesAsync();

            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // Configure UnityTransport for Relay client (includes host connection data)
            transport.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData,
                true);

            bool started = networkManager.StartClient();
            return (started, started ? null : "NetworkManager.StartClient failed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RelayAdapter] JoinAsync failed: {ex.Message}");
            return (false, ex.Message);
        }
#else
        Debug.LogWarning("[RelayAdapter] UNITY_MULTIPLAYER/UNITY_RELAY not defined. Relay is not available in this build.");
        await System.Threading.Tasks.Task.Yield();
        return (false, "Relay not available");
#endif
    }

#if UNITY_RELAY || UNITY_MULTIPLAYER
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

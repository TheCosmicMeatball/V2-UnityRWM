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

            Debug.Log("[RelayAdapter] Creating Relay allocation for host...");
            var alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            var code = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log($"[RelayAdapter] Relay allocation created. Region={alloc.Region} Server={alloc.RelayServer?.IpV4}:{alloc.RelayServer?.Port} JoinCode={code}");

            // Configure UnityTransport for Relay (secure: DTLS/WSS)
            transport.SetRelayServerData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData,
                null,
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

            Debug.Log($"[RelayAdapter] Joining Relay with JoinCode={joinCode}...");
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"[RelayAdapter] Relay join allocation resolved. Region={joinAlloc.Region} Server={joinAlloc.RelayServer?.IpV4}:{joinAlloc.RelayServer?.Port}");

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
            Debug.Log($"[RelayAdapter] StartClient returned {started}");
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

        Debug.Log("[RelayAdapter] Initializing Unity Services...");
        if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Uninitialized)
        {
            await Unity.Services.Core.UnityServices.InitializeAsync();
            Debug.Log("[RelayAdapter] Unity Services initialized");
        }

        if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[RelayAdapter] Signing in anonymously...");
            await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("[RelayAdapter] Anonymous sign-in complete");
        }

        _servicesReady = true;
    }
#endif
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

public static class LobbyAdapter
{
    private static bool _servicesReady;

    private static async Task EnsureServicesAsync()
    {
        if (_servicesReady)
            return;

        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();
            options.SetEnvironmentName("production");
            await UnityServices.InitializeAsync(options);
            Debug.Log($"[LobbyAdapter] Services initialized. Env=production ProjectId={Application.cloudProjectId}");
        }

        if (!Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
        {
            await Unity.Services.Authentication.AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("[LobbyAdapter] Anonymous sign-in complete");
        }

        _servicesReady = true;
    }

    public static async Task<(bool ok, string lobbyId, string lobbyCode, string error)> CreateOrUpdateLobbyAsync(string existingLobbyId, string roomCodeTag, int maxPlayers, string relayJoinCode)
    {
        try
        {
            await EnsureServicesAsync();

            if (string.IsNullOrEmpty(existingLobbyId))
            {
                var createOptions = new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "roomCode", new DataObject(DataObject.VisibilityOptions.Public, roomCodeTag) },
                        { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode ?? string.Empty) }
                    }
                };

                var lobby = await LobbyService.Instance.CreateLobbyAsync("RWM", maxPlayers, createOptions);
                Debug.Log($"[LobbyAdapter] Lobby created. LobbyId={lobby.Id} Code={lobby.LobbyCode}");
                return (true, lobby.Id, lobby.LobbyCode, null);
            }
            else
            {
                var updateOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode ?? string.Empty) }
                    }
                };

                var lobby = await LobbyService.Instance.UpdateLobbyAsync(existingLobbyId, updateOptions);
                Debug.Log($"[LobbyAdapter] Lobby updated. LobbyId={lobby.Id} Code={lobby.LobbyCode}");
                return (true, lobby.Id, lobby.LobbyCode, null);
            }
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"[LobbyAdapter] CreateOrUpdateLobbyAsync failed: {ex.Message}");
            return (false, null, null, ex.Message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyAdapter] CreateOrUpdateLobbyAsync failed: {ex.Message}");
            return (false, null, null, ex.Message);
        }
    }

    public static async Task<(bool ok, string relayJoinCode, string lobbyId, string error)> ResolveRelayJoinCodeByLobbyCodeAsync(string lobbyCode)
    {
        try
        {
            await EnsureServicesAsync();
            // Join the lobby by code to read its data
            var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            string relayCode = null;
            if (lobby.Data != null && lobby.Data.TryGetValue("relayJoinCode", out var dobj))
            {
                relayCode = dobj?.Value;
            }
            Debug.Log($"[LobbyAdapter] Resolved lobby '{lobbyCode}' to RelayJoinCode='{relayCode}'");
            return (true, relayCode, lobby.Id, null);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"[LobbyAdapter] ResolveRelayJoinCodeByLobbyCodeAsync failed: {ex.Message}");
            return (false, null, null, ex.Message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LobbyAdapter] ResolveRelayJoinCodeByLobbyCodeAsync failed: {ex.Message}");
            return (false, null, null, ex.Message);
        }
    }
}

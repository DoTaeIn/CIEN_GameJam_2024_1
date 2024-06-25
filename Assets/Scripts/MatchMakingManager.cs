using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ParrelSync;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class MatchMakingManager : MonoBehaviour
{
    [SerializeField] private GameObject buttons;
    [SerializeField] private GameObject playerPrefab; // 캐릭터 프리팹을 여기서 참조합니다.

    private Lobby Connected_Lobby;
    private QueryResponse _lobbies;
    private UnityTransport transport;

    private const string JoinCodeKey = "j";
    private string playerId;

    private void Awake()
    {
        transport = FindObjectOfType<UnityTransport>();
    }

    public async void CreateOrJoinLobby()
    {
        await Authenticate();

        var lobby = await QuickJoinLobby() ?? await CreateLobby();
        Connected_Lobby = lobby;

        if (Connected_Lobby != null)
        {
            SpawnPlayer();
        }
    }

    private async Task Authenticate()
    {
        if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
        {
            var options = new InitializationOptions();

            #if UNITY_EDITOR
                options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
            #endif
        
            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            playerId = AuthenticationService.Instance.PlayerId;
        }
    }

    private async Task<Lobby> QuickJoinLobby()
    {
        try
        {
            var lobby = await Lobbies.Instance.QuickJoinLobbyAsync();
            var a = await RelayService.Instance.JoinAllocationAsync(lobby.Data[JoinCodeKey].Value);
            SetTransformAsClient(a);
            NetworkManager.Singleton.StartClient();
            return lobby;
        }
        catch (Exception e)
        {
            Debug.Log("No lobbies available. Creating a new one...");
            return null;
        }
    }

    private async Task<Lobby> CreateLobby()
    {
        const int MaxPlayers = 2;

        var a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

        var options = new CreateLobbyOptions()
        { 
            Data = new Dictionary<string, DataObject>
                { { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
        };
        var lobby = await Lobbies.Instance.CreateLobbyAsync("MyLobby", MaxPlayers, options);

        StartCoroutine(HeartbetLobbyCoroutine(lobby.Id, 15));

        transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

        NetworkManager.Singleton.StartHost();
        return lobby;
    }

    private void SetTransformAsClient(JoinAllocation a)
    {
        transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
    }

    private static IEnumerator HeartbetLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    private void SpawnPlayer()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            var playerInstance = Instantiate(playerPrefab);
            playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
            {
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    var playerInstance = Instantiate(playerPrefab);
                    playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                }
            };
        }
    }

    private void OnDestroy()
    {
        try
        {
            StopAllCoroutines();

            if (Connected_Lobby != null)
            {
                if (Connected_Lobby.HostId == playerId) Lobbies.Instance.DeleteLobbyAsync(Connected_Lobby.Id);
                else Lobbies.Instance.RemovePlayerAsync(Connected_Lobby.Id, playerId);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error while shutting down lobby: {e}");
        }
    }
}

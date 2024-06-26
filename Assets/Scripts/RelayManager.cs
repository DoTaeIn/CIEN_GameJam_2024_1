using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RelayManager : NetworkBehaviour
{
    [SerializeField] private Text joinCodeText;
    [SerializeField] private InputField joinInputField;
    [SerializeField] private GameObject buttons;
    [SerializeField] private GameObject playerPrefab; // 플레이어 프리팹 추가
    [SerializeField] private NetworkTimer _timer;
    public GameObject ProgressBarGroup;
    public GameObject HealthBarGroup;

    private UnityTransport transport;
    private const int MaxPlayers = 2;
    public static RelayManager Instance;

    public GameObject Target;

    private async void Awake()
    {
        Instance = this;
        transport = FindObjectOfType<UnityTransport>();
        buttons.SetActive(false);

        await Authenticate();

        buttons.SetActive(true);

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        
    }

    private static async Task Authenticate()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateGame()
    {
        buttons.SetActive(false);

        Allocation a = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
        joinCodeText.text = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);
        transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);
        
        /*
        if (NetworkManager.Singleton.IsHost)
        {
            _timer.StartTimer(); // 타이머 시작
        }
        */
        
        NetworkManager.Singleton.StartHost();
    }

    public async void JoinGame()
    {
        buttons.SetActive(false);

        JoinAllocation a = await RelayService.Instance.JoinAllocationAsync(joinInputField.text);
        transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
        NetworkManager.Singleton.StartClient();
    }

    private void OnServerStarted()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            //SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
        {
            //SpawnPlayer(clientId);
            
            if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
            {
                SetTargetActiveClientRpc();
            }
        }

        
    }

    [ClientRpc]
    private void SetTargetActiveClientRpc()
    {
        Target.SetActive(true);
    }
    /*
    private void SpawnPlayer(ulong clientId)
    {
        GameObject playerInstance = Instantiate(playerPrefab);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);
    }
    */
}

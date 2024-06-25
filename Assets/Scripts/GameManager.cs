using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject Player_Prefab;
    public NetworkVariable<bool> isDone = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isBlueWon = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private GameObject[] winScreens;
    
    [ServerRpc(RequireOwnership = false)]
    public void RespawnCharacterServerRpc()
    {
        if (NetworkManager.Singleton != null && Player_Prefab != null)
        {
            NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Player_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, new Vector3(0, 0, 0));
        }
    }

    [ClientRpc]
    public void RespawnCharacterClientRpc()
    {
        NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Player_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, new Vector3(0, 0, 0));
    }

    [ServerRpc(RequireOwnership = false)]
    private void ImDoneServerRpc()
    {
        isDone.Value = true;
    }

    private void Update()
    {
        if (isDone.Value)
        {
            if (isBlueWon.Value)
            {
                DisplayWinScreen(0);
            }
            else
            {
                DisplayWinScreen(1);
            }
        }
    }

    void DisplayWinScreen(int i)
    {
        for (int j = 0; j < winScreens.Length; j++)
        {
            if (j == i)
            {
                winScreens[j].SetActive(true);
            }
            else
            {
                winScreens[j].SetActive(false);
            }
        }
    }
}

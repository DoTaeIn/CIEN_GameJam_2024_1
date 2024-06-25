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
   
    
    [ServerRpc(RequireOwnership = false)]
    public void RespawnCharacterServerRpc()
    {
        if (NetworkManager.Singleton != null && Player_Prefab != null)
        {
            NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Player_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, new Vector3(0, 0, 0));
        }
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
                Debug.Log("Blue Won!");
            }
            else
            {
                Debug.Log("Red Won!");
            }
        }
    }
}

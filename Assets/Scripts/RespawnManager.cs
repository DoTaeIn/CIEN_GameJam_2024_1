using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RespawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject Player_Prefab;
    
    [ServerRpc(RequireOwnership = false)]
    public void RespawnCharacterServerRpc()
    {
        if (NetworkManager.Singleton != null && Player_Prefab != null)
        {
            NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Player_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, new Vector3(0, 0, 0));
        }
    }
    
}

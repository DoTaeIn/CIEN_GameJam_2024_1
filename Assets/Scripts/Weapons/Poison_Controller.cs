using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Poison_Controller : NetworkBehaviour
{
    private CircleCollider2D _circleCollider;
    public NetworkObject Effect;

    public int Explode_range = 10;
    public int damage = 5;

    private void Awake()
    {

        _circleCollider = GetComponent<CircleCollider2D>();
    }

    private void Start()
    {
        //_circleCollider.enabled = false;
        _circleCollider.radius = Explode_range;
        Invoke(nameof(Explode), 5);
    }

    public void Explode()
    {
        //_circleCollider.enabled = true;
        
        //EffectServerRpc();
        Invoke(nameof(DestroyServerRpc),1);
    }
    
    [ServerRpc]
    private void EffectServerRpc()
    {
        NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(Effect).transform.position=transform.position;
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(this.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Player>().Hp -= damage;
        }
    }
}

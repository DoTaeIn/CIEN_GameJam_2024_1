using System;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    public float _hp=100;
    public float Hp
    {
        get
        {
            return _hp;
        }
        set
        {
            _hp = value;
            SetHpServerRpc(value);
        }
    }

    [ServerRpc]
    private void SetHpServerRpc(float hp)
    {
        _hp = hp;
        SetHpClientRpc(hp);
    }

    [ClientRpc]
    private void SetHpClientRpc(float hp)
    {
        if (!IsOwner)
        {
            _hp = hp;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        
    }

    private void Update()
    {
        if (IsOwner)
        {
            //Move();
            if (Input.GetKeyDown("o"))
            {
                foreach (var a in GameObject.FindGameObjectsWithTag("Player"))
                {
                    if (a.GetHashCode() != gameObject.GetHashCode())
                    {
                        Debug.Log("test`1");
                        a.GetComponent<Player>().DamagedServerRpc(Vector3.right, 1f);
                    }
                }
            }
        }

        
    }

    private void Move()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveX, moveY, 0) * moveSpeed;
        if (move!=Vector3.zero)
        {
            rb.velocity = new Vector3(move.x, move.y, 0);
        }
    }

    
    [ServerRpc(RequireOwnership = false)]
    public void DamagedServerRpc(Vector3 dir, float damage)
    {
        DamagedClientRpc(dir, 1f);
    }

    
    [ClientRpc]
    public void DamagedClientRpc(Vector3 dir, float damage)
    {
        if (IsOwner)
        {
            rb.AddForce(dir*10,ForceMode2D.Impulse);
            Hp -= damage;
            UpdatePositionServerRpc(rb.position,rb.velocity);
        }
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            Move();
            UpdatePositionServerRpc(rb.position, rb.velocity);
        }
    }

    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 position,Vector3 velocity)
    {
        
            rb.position = position;
            rb.velocity = velocity;
            Debug.Log(position);Debug.Log(velocity);
            UpdatePositionClientRpc(position,velocity);
        
        
        
        
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 position,Vector3 velocity)
    {
        if (!IsOwner)
        {
            rb.position = position;
            rb.velocity = velocity;
        }
    }
}
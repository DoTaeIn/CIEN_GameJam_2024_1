using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private SpriteRenderer _spriteRenderer;

    private NetworkVariable<float> HP = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

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
                        Vector3 dir = a.transform.position - gameObject.transform.position;
                        a.GetComponent<Player>().DamagedServerRpc(dir, 1f);
                       
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

        // 네트워크에서 다른 클라이언트들에게 위치 업데이트
        //UpdatePositionServerRpc(rb.position,rb.velocity);
    }


    
    [ServerRpc(RequireOwnership = false)]
    public void DamagedServerRpc(Vector3 dir, float damage)
    {
        DamagedClientRpc(dir, 1f);
        gameObject.layer = 1;
        _spriteRenderer.color = new Color(233, 233, 233, 250);
        StartCoroutine("BeVulnerable", 1);
    }

    
    [ClientRpc]
    public void DamagedClientRpc(Vector3 dir, float damage)
    {
        if (IsOwner)
        {
            rb.AddForce(dir*10,ForceMode2D.Impulse);
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


    
    
    IEnumerator BeVulnerable()
    {
        foreach (var a in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (a.GetHashCode() != gameObject.GetHashCode())
            {
                a.GetComponent<SpriteRenderer>().color = Color.white;
                a.gameObject.layer = 2;
            }
        }
        yield return null;
    }
}
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;


    private NetworkVariable<float> HP = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        
    }

    private void Update()
    {
        if (IsOwner)
        {
            Move();
            if (Input.GetKeyDown("o"))
            {
                //Debug.Log("test");
                /*
                foreach (var VARIABLE in NetworkManager.Singleton.ConnectedClients)
                {
                    if(VARIABLE==NetworkManager.Singleton.LocalClient)
                }*/
                
                //if(NetworkManager.Singleton.Cli)

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
        rb.velocity = new Vector3(move.x, move.y, 0);

        // 네트워크에서 다른 클라이언트들에게 위치 업데이트
        UpdatePositionServerRpc(transform.position);
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
        }
    }
    
    [ServerRpc]
    private void UpdatePositionServerRpc(Vector3 newPosition)
    {
        transform.position = newPosition;
        UpdatePositionClientRpc(newPosition);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        if (!IsOwner)
        {
            transform.position = newPosition;
        }
    }
}
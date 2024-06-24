using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        
    }

    private void Update()
    {
        if (IsOwner)
        {
            Move();
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
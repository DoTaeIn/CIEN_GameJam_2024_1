using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [Header("Player Default Setting")]
    public float _hp = 100;
    public float moveSpeed = 5f;
    public float chargingMoveSpeed = 2f; // 차징 중 이동 속도
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

    private Rigidbody2D rb;
    
    [Header("Weapon Prefabs")]
    [SerializeField] private GameObject Bomb_Prefab;
    
    [Header("Charging Settings")]
    [SerializeField] private float ChargeShotPower = 50f;
    [SerializeField] private float maxChargeTime = 1.5f; 
    private float chargeTime = 0f;
    private bool isCharging = false; 
    private bool isFullCharge = false;
    
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
    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            Attack();
            HandleCharging();
            if (Input.GetKeyDown("o"))
            {
                foreach (var a in GameObject.FindGameObjectsWithTag("Player"))
                {
                    if (a.GetHashCode() != gameObject.GetHashCode())
                    {
                        Vector3 dir = a.transform.position - gameObject.transform.position;
                        a.GetComponent<Player>().DamagedServerRpc(dir, 1f);
                    }
                }
            }
        }
    }

    private void HandleCharging()
    {
        if (Input.GetKeyDown("k"))
        {
            StartCharging();
        }

        if (Input.GetKeyUp("k"))
        {
            ReleaseCharge();
        }

        if (isCharging)
        {
            chargeTime += Time.deltaTime;
            if (chargeTime >= maxChargeTime)
            {
                isFullCharge = true;
            }
        }
    }

    private void Move()
    {
        float speed = isCharging ? chargingMoveSpeed : moveSpeed;
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveX, moveY, 0) * speed;
        if (move != Vector3.zero)
        {
            rb.velocity = new Vector3(move.x, move.y, 0);
        }
    }

    private void Attack()
    {
        if (Input.GetKeyDown("i"))
        {
            GameObject bomb = Instantiate(Bomb_Prefab);
            bomb.GetComponent<Bomb_Controller>().Invoke("Explode", 5);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamagedServerRpc(Vector3 dir, float damage)
    {
        DamagedClientRpc(dir, damage);
        gameObject.layer = 1;
        _spriteRenderer.color = new Color(233, 233, 233, 250);
        StartCoroutine("BeVulnerable", 1);
    }

    [ClientRpc]
    public void DamagedClientRpc(Vector3 dir, float damage)
    {
        if (IsOwner)
        {
            rb.AddForce(dir * 10, ForceMode2D.Impulse);
            Hp -= damage;
            UpdatePositionServerRpc(rb.position, rb.velocity);
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
    private void UpdatePositionServerRpc(Vector3 position, Vector3 velocity)
    {
        rb.position = position;
        rb.velocity = velocity;
        UpdatePositionClientRpc(position, velocity);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 position, Vector3 velocity)
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
    
    public void StartCharging()
    {
        isCharging = true;
        chargeTime = 0f;
    }

    public void ReleaseCharge()
    {
        isCharging = false;
        bool isFullyCharged = isFullCharge;
        isFullCharge = false;
        chargeTime = 0f;

        if (isFullyCharged)
        {
            ChargeShot();
        }
        else
        {
            ShortDash();
        }
    }

    public void ChargeShot()
    {
        Vector2 dir = rb.velocity.normalized;
        rb.AddForce(dir * ChargeShotPower, ForceMode2D.Impulse);
    }

    public void ShortDash()
    {
        Vector2 dir = rb.velocity.normalized;
        float dashSpeed = 20f;
        rb.velocity = dir * dashSpeed;
    }
}

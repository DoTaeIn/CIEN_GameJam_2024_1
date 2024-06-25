using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Slider = UnityEngine.UI.Slider;

public class Player : NetworkBehaviour
{
    [Header("Player Default Setting")]
    public float _hp = 100;
    public float moveSpeed = 5f;
    public float chargingMoveSpeed = 2f; // 차징 중 이동 속도
    
    private Rigidbody2D rb;
    public GameObject ProgressBarPref;
    public GameObject ProgressBar;

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
    
    public float _score=0;
    public float Score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
            ProgressBar.GetComponent<Slider>().value = value;
            if (IsOwner)
            {
                SetScoreServerRpc(value);
            }
        }
    }

    [ServerRpc]
    private void SetScoreServerRpc(float score)
    {
        _score = score;
        ProgressBar.GetComponent<Slider>().value = score;
        SetScoreClientRpc(score);
    }

    [ClientRpc]
    private void SetScoreClientRpc(float score)
    {
        if (!IsOwner)
        {
            _score = score;
            ProgressBar.GetComponent<Slider>().value = score;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if(IsOwner)
            Score+=Time.deltaTime;
    }

    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
        if (IsOwner)
        {
            SpawnProgressBarServerRpc(OwnerClientId);
        }
        
        
    }

    [ServerRpc]
    private void SpawnProgressBarServerRpc(ulong clientId)
    {
        ProgressBar = NetworkManager.SpawnManager.InstantiateAndSpawn(ProgressBarPref.GetComponent<NetworkObject>(),
            clientId).gameObject;
        ProgressBar.GetComponent<NetworkObject>().TrySetParent(RelayManager.Instance.ProgressBarGroup.transform, false);
        SpawnPregressBarClientRpc();
    }

    [ClientRpc]
    private void SpawnPregressBarClientRpc()
    {
        //if (IsOwner)
        {
            //ProgressBar = NetworkManager.LocalClient.OwnedObjects[NetworkManager.LocalClient.OwnedObjects.Count - 1].gameObject;
            foreach (var networkObject in RelayManager.Instance.ProgressBarGroup.GetComponentsInChildren<NetworkObject>())
            {
                if (networkObject.OwnerClientId == OwnerClientId)
                    ProgressBar = networkObject.gameObject;

            }
            

        }
    }
    private void Update()
    {
        if (ProgressBar == null)
        {
            foreach (var networkObject in RelayManager.Instance.ProgressBarGroup.GetComponentsInChildren<NetworkObject>())
            {
                if (networkObject.OwnerClientId == OwnerClientId)
                    ProgressBar = networkObject.gameObject;

            }
        }
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
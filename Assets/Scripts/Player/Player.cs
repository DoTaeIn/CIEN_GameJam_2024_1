using System;
using System.Collections;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Slider = UnityEngine.UI.Slider;

public class Player : NetworkBehaviour
{
    private SpriteRenderer _spriteRenderer;
    
    [Header("Player Default Setting")]
    public float _hp = 100;
    public float moveSpeed = 5f;
    
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
    
    [Header("Charging & Dash Settings")]
    [SerializeField] private float DashSpeed = 10f;
    [SerializeField] private float DashDuration = 0.2f;
    [SerializeField] private float DashCoolDown = 1f;
    private bool isDashing = false;
    private float prevDashPassed = 0f;
    
    [Header("Scoring System")]
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
            Score += Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Weapon"))
        {
            _hp -= other.gameObject.GetComponent<WeaponManager>().damage;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    
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
        SpawnProgressBarClientRpc();
    }

    [ClientRpc]
    private void SpawnProgressBarClientRpc()
    {
        foreach (var networkObject in RelayManager.Instance.ProgressBarGroup.GetComponentsInChildren<NetworkObject>())
        {
            if (networkObject.OwnerClientId == OwnerClientId)
                ProgressBar = networkObject.gameObject;
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
            isDashing = true;
            prevDashPassed += Time.deltaTime;
        }

    }

    private void Move()
    {

        Vector2 movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (isDashing)
        {
            Vector2 prevVec = rb.velocity;
            rb.velocity = Vector2.zero;
            rb.AddForce(prevVec * DashSpeed, ForceMode2D.Impulse);
            isDashing = false;
        }
        else
        {
            rb.velocity = movement * moveSpeed;
        }

    }
    
    private void Attack()
    {
        if (Input.GetKeyDown("i"))
        {
            GameObject bomb = Instantiate(Bomb_Prefab, transform.position, quaternion.identity);
            bomb.GetComponent<Bomb_Controller>().Invoke("Explode", 5);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void InstantiateObjsServerRpc(Vector3 transform, Quaternion quaternion)
    {
        if (NetworkManager.Singleton != null && Bomb_Prefab != null)
        {
            
            NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Bomb_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, transform, quaternion);
            
        }
    }
    


    [ServerRpc(RequireOwnership = false)]
    public void DamagedServerRpc(Vector3 dir, float damage)
    {
        DamagedClientRpc(dir, damage);
        gameObject.layer = 1;
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
    
}

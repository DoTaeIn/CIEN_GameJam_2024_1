using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

public class Player : NetworkBehaviour
{
    public RuntimeAnimatorController Rabbit;
    public RuntimeAnimatorController Elise;
    private SpriteRenderer _spriteRenderer;
    private GameManager _respawnManager;
    
    private Animator _animator;
    [SerializeField]private Animator Attack_Animtor;
    
    
    public bool isStoped;
    
    [Header("Player Default Setting")]
    public float _hp = 100;
    public float moveSpeed = 5f;
    
    [Header(("Radius Collision Detection"))]
    float angle = 30f;
    private float radius = 3f;
    bool isCollision = false;
    Color _blue = new Color(0f, 0f, 1f, 0.2f);
    Color _red = new Color(1f, 0f, 0f, 0.2f);
    
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
            if(IsOwner)
                SetHpServerRpc(value);
        }
    }

    [Header("Weapon Prefabs")]
    [SerializeField] private GameObject Bomb_Prefab;
    [SerializeField] private GameObject Poison_Prefab;
    private bool _isDaggerDelay, _isPoisonDelay, _isBombDelay,_isDashDelay;
    [Header("Charging & Dash Settings")]
    [SerializeField] private float DashSpeed = 10f;
    [SerializeField] private float DashDuration = 0.2f;
    [SerializeField] private float DashCoolDown = 1f;
    private bool isDashing = false;
    private float prevDashPassed = 0f;
    private bool isPushing = false;
    
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

    [Header("Knokback System")] public bool isKnocked = false;

    [Header("Weapons")] private GameObject[] Weapons;
    
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
        if(IsOwner&& other.tag=="Target")
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
        _respawnManager = FindObjectOfType<GameManager>();
        //_respawnManager = FindObjectOfType<RespawnManager>();
        _animator = GetComponent<Animator>();

    
        if (IsOwner)
        {
            SpawnProgressBarServerRpc(OwnerClientId);
        }

        if (OwnerClientId == 0)
        {
            GetComponent<Animator>().runtimeAnimatorController = Rabbit;
            
        }else if(OwnerClientId==1)
            GetComponent<Animator>().runtimeAnimatorController = Elise;
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
        foreach (var networkObject in RelayManager.Instance.ProgressBarGroup.GetComponentsInChildren<Slider>())
        {
            if (networkObject.GetComponent<NetworkObject>().OwnerClientId == OwnerClientId)
            {
                
                ProgressBar = networkObject.gameObject;
                ProgressBar.transform.GetChild(1).
                    transform.GetChild(0).
                    GetComponent<Image>().color = OwnerClientId == 0 ? Color.red : Color.blue;
                
            }
        }
    }

    [ServerRpc]
    private void SpawnPoisonServerRpc()
    {
        Unity.Netcode.NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(Poison_Prefab.GetComponent<NetworkObject>(),OwnerClientId, false, false, false, transform.position);
    }
    private void Update()
    {
        if (Input.GetKeyDown("n")&& !_isPoisonDelay)
        {
            SpawnPoisonServerRpc();
            _isPoisonDelay = true;
            Invoke(nameof(PoisonDelay),5);
        }
        if (Input.GetKeyDown("t"))
        {
            foreach (var VARIABLE in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (!VARIABLE.GetComponent<NetworkObject>().IsOwner)
                {
                    VARIABLE.GetComponent<Player>().theWorld();
                    makeItStopServerRpc(true);
                }
            }
        }
        
        foreach (var VARIABLE in GameObject.FindGameObjectsWithTag("Player"))
        {
            
            if (!VARIABLE.GetComponent<NetworkObject>().IsOwner)
            {
                Debug.Log(VARIABLE.name);
                Transform target = VARIABLE.gameObject.transform;
                
                Vector3 interV = target.position - transform.position;
                if (interV.magnitude <= radius)
                {
                    Debug.Log("test");
                    // '타겟-나 벡터'와 '내 정면 벡터'를 내적
                    float dot = Vector3.Dot(rb.velocity.normalized, interV.normalized);
                    //Debug.Log("dot"+dot);
                    // 두 벡터 모두 단위 벡터이므로 내적 결과에 cos의 역을 취해서 theta를 구함
                    float theta = Mathf.Acos(dot);
                    //Debug.Log("theta: "+theta);
                    // angleRange와 비교하기 위해 degree로 변환
                    float degree = Mathf.Rad2Deg * theta;
                    //Debug.Log("degree: "+degree);

                    // 시야각 판별
                    if (degree <= angle / 2f)
                    {
                        Debug.Log("Final");
                        isCollision = true;
                    }
                       
                    else
                        isCollision = false;

                }
                else
                    isCollision = false;
            }
        }
        
        Debug.Log(isCollision);

        if (IsOwner)
        {
            if (Input.GetKeyDown("h")&& !_isDaggerDelay)
            {

                if (isCollision)
                {
                    foreach (var a in GameObject.FindGameObjectsWithTag("Player"))
                    {
                        if (a.GetHashCode() != gameObject.GetHashCode())
                        {
                            a.GetComponent<Player>()
                                .DamagedServerRpc((rb.velocity - a.GetComponent<Player>().rb.velocity).normalized, 10);
                        }
                    }

                    SetDaggerServerRpc();
                }
                else
                {
                    SetDaggerServerRpc();
                }

                _isDaggerDelay = true;
                Invoke(nameof(DaggerDelay),2f);

            }
        }
        
        
        
        if (_score >= 100)
        {
            if (_respawnManager.isDone.Value == false)
            {
                if (IsHost)
                {
                    _respawnManager.isDone.Value = true;
                    _respawnManager.isBlueWon.Value = false;
                }
                else
                {
                    _respawnManager.isDone.Value = true;
                    _respawnManager.isBlueWon.Value = true;
                }
            }
        }
        if (Hp <= 0)
        {
            Hp = 100;
            transform.position = new Vector3(100, 100, 0);
            isStoped = true;
            Invoke("unStop", 3);
            
            
        }
        
        
        
        if (ProgressBar == null)
        {
            foreach (var networkObject in RelayManager.Instance.ProgressBarGroup.GetComponentsInChildren<Slider>())
            {
                if (networkObject.GetComponent<NetworkObject>().OwnerClientId == OwnerClientId)
                {
                    ProgressBar = networkObject.gameObject;
                    ProgressBar.transform.GetChild(1).
                        transform.GetChild(0).
                        GetComponent<Image>().color = OwnerClientId == 0 ? Color.red : Color.blue;
                    
                }
                    
            }
        }
        if (IsOwner)
        {
            Attack();
            HandleCharging();
            if (Input.GetKeyDown("o")&&!isPushing)
            {
                foreach (var a in GameObject.FindGameObjectsWithTag("Player"))
                {
                    if (a.GetHashCode() != gameObject.GetHashCode())
                    {
                        isPushing = true;
                        Vector3 dir = a.transform.position - gameObject.transform.position;
                        if (dir.magnitude>2) continue;
                        
                        a.GetComponent<Player>().DamagedServerRpc(dir.normalized, 1f);
                        a.GetComponent<Player>().isKnocked = true;
                        
                        Invoke("SetFalseIsPushing",1);
                        break;
                    }
                }
            }
        }
    }

    

    private void DaggerDelay()
    {
        _isDaggerDelay=false;
    }
    
    private void PoisonDelay()
    {
        _isPoisonDelay=false;
    }
    
    private void DashDelay()
    {
        _isDashDelay=false;
    }
    
    private void BombDelay()
    {
        _isBombDelay=false;
    }

    private void unStop()
    {
        transform.position = new Vector3(0, 0, 0);
        isStoped = false;
    }

    private void SetFalseIsPushing()
    {
        isPushing = false;
    }

    private void HandleCharging()
    {
        if (Input.GetKeyDown("k")&&!isDashing)
        {
            isDashing = true;
        }else
        
        if (prevDashPassed> DashDuration)
        {
            prevDashPassed = 0;
            isDashing = false;
        }

    }

    private void Move()
    {

        Vector2 movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (isDashing&& prevDashPassed==0&& !_isDashDelay)
        {
            Vector2 prevVec = rb.velocity;
            rb.AddForce(prevVec.normalized * DashSpeed, ForceMode2D.Impulse);
            _isDashDelay = true;
            Invoke(nameof(DashDelay),1);
        }
        else if(!isDashing && !isKnocked&& movement.magnitude!=0)
        {
            _animator.SetBool("Move",true);
            SetMoveClientRpc(true);
            rb.velocity = movement * moveSpeed;
            
        }
        else
        {
            _animator.SetBool("Move",false);
            SetMoveServerRpc(false);
        }

    }
    
    [ServerRpc]
    private void SetMoveServerRpc(bool t)
    {
        _animator.SetBool("Move",t);
        SetMoveClientRpc(t);
    }
    
    [ClientRpc]
    private void SetMoveClientRpc(bool t)
    {
        _animator.SetBool("Move",t);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SetDaggerServerRpc()
    {
        Attack_Animtor.SetTrigger("Attack");
        SetDaggerClientRpc();
    }
    
    [ClientRpc]
    private void SetDaggerClientRpc()
    {
        Attack_Animtor.SetTrigger("Attack");
    }
    
    private void Attack()
    {
        if (Input.GetKeyDown("i")&&!_isBombDelay)
        {
            InstantiateObjsServerRpc(new Vector3(transform.position.x, transform.position.y));
            _isBombDelay = true;
            Invoke(nameof(BombDelay),3f);
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void InstantiateObjsServerRpc(Vector3 transform)
    {
        if (NetworkManager.Singleton != null && Bomb_Prefab != null)
        {
            NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Bomb_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, transform);
        }
    }
    


    [ServerRpc(RequireOwnership = false)]
    public void DamagedServerRpc(Vector3 dir, float damage)
    {
        
        DamagedClientRpc(dir, damage);
        gameObject.layer = 1;
        StartCoroutine("BeVulnerable", 1);
        Invoke("MakeIsKnockedFalse", 0.3f);
    }

    [ClientRpc]
    public void DamagedClientRpc(Vector3 dir, float damage)
    {
        if (IsOwner)
        {
            rb.AddForce(dir * 10, ForceMode2D.Impulse);
            Hp -= damage;
            isKnocked = true;
            UpdatePositionServerRpc(rb.position, rb.velocity);
            Invoke("MakeIsKnockedFalse", 0.3f);
            
        }
    }

    private void MakeIsKnockedFalse()
    {
        isKnocked = false;

    }

    private void FixedUpdate()
    {
        if(!isStoped){
            if (IsOwner)
            {

                Move();

                UpdatePositionServerRpc(rb.position, rb.velocity);
            }

            if (isDashing)
            {
                prevDashPassed += Time.deltaTime;
            }
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

    public void theWorld()
    {
        isStoped = true;
        Invoke("reverseTheWorld", 3);
        
    }

    private void reverseTheWorld()
    {
        isStoped = false;
        makeItStopServerRpc(false);
    }


    [ServerRpc(RequireOwnership = false)]
    private void makeItStopServerRpc(bool IsStop)
    {
        isStoped = IsStop;
        makeItStopClientRpc(isStoped);
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void makeItStopClientRpc(bool IsStop)
    {
        isStoped = IsStop;
        
    }
}

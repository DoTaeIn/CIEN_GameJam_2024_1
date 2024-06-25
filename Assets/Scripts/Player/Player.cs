using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEditor;
using Slider = UnityEngine.UI.Slider;

public class Player : NetworkBehaviour
{
    public RuntimeAnimatorController Rabbit;
    public RuntimeAnimatorController Elise;
    private SpriteRenderer _spriteRenderer;
    private RespawnManager _respawnManager;
    private Animator _animator;
    
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Transform target = other.gameObject.transform;
            Vector3 interV = target.position - transform.position;
            if (interV.magnitude <= radius)
            {
                // '타겟-나 벡터'와 '내 정면 벡터'를 내적
                float dot = Vector3.Dot(interV.normalized, transform.forward);
                // 두 벡터 모두 단위 벡터이므로 내적 결과에 cos의 역을 취해서 theta를 구함
                float theta = Mathf.Acos(dot);
                // angleRange와 비교하기 위해 degree로 변환
                float degree = Mathf.Rad2Deg * theta;

                // 시야각 판별
                if (degree <= angle / 2f)
                    isCollision = true;
                else
                    isCollision = false;

            }
            else
                isCollision = false;
        }
        
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
        _respawnManager = FindObjectOfType<RespawnManager>();
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
        foreach (var networkObject in RelayManager.Instance.ProgressBarGroup.GetComponentsInChildren<NetworkObject>())
        {
            if (networkObject.OwnerClientId == OwnerClientId)
                ProgressBar = networkObject.gameObject;
        }
    }
    
    private void Update()
    {
        Debug.Log(isKnocked);
        if (Hp <= 0)
        {
            _respawnManager.RespawnCharacterServerRpc();
            Destroy(gameObject);
        }
        
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

        if (isDashing&& prevDashPassed==0)
        {
            Vector2 prevVec = rb.velocity;
            //rb.velocity = movement.normalized * DashSpeed;
            //Debug.Log(prevVec.normalized * DashSpeed);
            rb.AddForce(prevVec.normalized * DashSpeed, ForceMode2D.Impulse);
            //isDashing = false;
            
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
    
    private void Attack()
    {
        if (Input.GetKeyDown("i"))
        {
            InstantiateObjsServerRpc(new Vector3(transform.position.x, transform.position.y));
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
        //Debug.Log("sadf");
        isKnocked = false;

    }

    private void FixedUpdate()
    {
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
    
    private void OnDrawGizmos()
    {
        Handles.color = isCollision ? _red : _blue;
        // DrawSolidArc(시작점, 노멀벡터(법선벡터), 그려줄 방향 벡터, 각도, 반지름)
        Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, angle / 2, radius);
        Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, -angle / 2, radius);
    }
    
}

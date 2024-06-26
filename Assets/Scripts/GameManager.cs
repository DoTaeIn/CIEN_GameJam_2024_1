using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private GameObject Player_Prefab;
    public NetworkVariable<bool> isDone = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isBlueWon = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private GameObject[] Curtain;
    
    [ServerRpc(RequireOwnership = false)]
    public void ChangeIsBlueWinServerRpc(bool value)
    {
        isBlueWon.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeIsDoneServerRpc(bool value)
    {
        isDone.Value = value;
    }
    
    [Header("Skill CoolDown System")] 
    [SerializeField]private Image dagger;
    [SerializeField]private Image bomb;
    [SerializeField]private Image poison;
    [SerializeField]private Image time;
    
    private float daggerCurrentTime = 0;
    private float bombCurrentTime = 0;
    private float poisonCurrentTime = 0;
    private float timeCurrentTime = 0;
    
    private bool isDaggerCooling = false;
    private bool isBombCooling = false;
    private bool isPoisonCooling = false;
    private bool isTimeCooling = false;

    [SerializeField] private GameObject[] winScreens;
    
    [ServerRpc(RequireOwnership = false)]
    public void RespawnCharacterServerRpc()
    {
        if (NetworkManager.Singleton != null && Player_Prefab != null)
        {
            NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Player_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, new Vector3(0, 0, 0));
        }
    }

    [ClientRpc]
    public void RespawnCharacterClientRpc()
    {
        NetworkObject gameobject = NetworkManager.SpawnManager.InstantiateAndSpawn(Player_Prefab.GetComponent<NetworkObject>(), OwnerClientId, false, false, false, new Vector3(0, 0, 0));
    }
    

    private void Start()
    {
        LeanTween.init( 1300 );
        dagger.fillAmount = 0;
        bomb.fillAmount = 0;
        time.fillAmount = 0;
        poison.fillAmount = 0;
        
    }

    private bool gameStart;

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        if (NetworkManager.ConnectedClientsList.Count == 2)
        {
            gameStart = true;
            StartGameClientRpc();
        }
    }

    private void Update()
    {
        //NetworkManager
        if(!gameStart)
            StartGameServerRpc();
        
        
        
        if (isDone.Value)
        {
            if (isBlueWon.Value)
            {
                DisplayWinScreen(0);
            }
            else
            {
                DisplayWinScreen(1);
            }
        }
        
        if (Input.GetKeyDown("h") && !isDaggerCooling)
        {
            isDaggerCooling = true;
            daggerCurrentTime = 0;
        }
        if (Input.GetKeyDown("i") && !isBombCooling)
        {
            isBombCooling = true;
            bombCurrentTime = 0;
        }
        if (Input.GetKeyDown("n") && !isPoisonCooling)
        {
            isPoisonCooling = true;
            poisonCurrentTime = 0;
        }
        if (Input.GetKeyDown("t") && !isTimeCooling)
        {
            isTimeCooling = true;
            timeCurrentTime = 0;
        }

        if (isDaggerCooling)
        {
            CoolTimeFilling(ref daggerCurrentTime, dagger, 2, ref isDaggerCooling);
        }
        if (isBombCooling)
        {
            CoolTimeFilling(ref bombCurrentTime, bomb, 4, ref isBombCooling);
        }
        if (isPoisonCooling)
        {
            CoolTimeFilling(ref poisonCurrentTime, poison, 5, ref isPoisonCooling);
        }
        if (isTimeCooling)
        {
            CoolTimeFilling(ref timeCurrentTime, time, 30, ref isTimeCooling);
        }
    }

    void DisplayWinScreen(int i)
    {
        for (int j = 0; j < winScreens.Length; j++)
        {
            if (j == i)
            {
                winScreens[j].SetActive(true);
            }
            else
            {
                winScreens[j].SetActive(false);
            }
        }
    }
    
    private void CoolTimeFilling(ref float currentTime, Image filling, int coolTime, ref bool isCooling)
    {
        currentTime += Time.deltaTime;
        filling.fillAmount = 1 - (currentTime / coolTime);

        if (currentTime >= coolTime)
        {
            currentTime = 0; // Cooldown completed, reset timer
            isCooling = false; // Cooldown 끝남을 표시
        }
    }

    public void ReStart()
    {
        Destroy(NetworkManager.Singleton.gameObject());
        
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    [ClientRpc]
    public void StartGameClientRpc()
    {
        Curtain[0].transform.LeanMoveLocal(new Vector2(-650*3, 0), 2).setEaseOutQuart();
        Curtain[1].transform.LeanMoveLocal(new Vector2(650*3, 0), 2).setEaseOutQuart();
        
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AreaDetector : MonoBehaviour
{
    private NetworkTimer _timer;

    private void Awake()
    {
        _timer = FindObjectOfType<NetworkTimer>();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.tag);
        if (other.CompareTag("Player"))
        {
            if (NetworkManager.Singleton.IsServer)
            {
                _timer.isTimerRunning = true;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (NetworkManager.Singleton.IsServer)
            {
                _timer.isTimerRunning = false;
            }
        }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AreaDetector : NetworkBehaviour
{
    private NetworkTimer _timer;

    private void Awake()
    {
        _timer = FindObjectOfType<NetworkTimer>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (IsServer)
            {
                _timer.isTimerRunning = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (IsServer)
            {
                _timer.isTimerRunning = false;
            }
        }
    }
}

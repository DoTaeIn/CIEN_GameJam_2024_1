using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkTimer : NetworkBehaviour
{
    [SerializeField] private Text timerText;
    private float startTime;
    private float timerDuration = 300f; // 5분 타이머

    private NetworkVariable<float> networkTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        if (IsServer)
        {
            startTime = Time.time;
            networkTime.Value = startTime;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            networkTime.Value = Time.time - startTime;
        }

        UpdateTimerUI(networkTime.Value);
    }

    private void UpdateTimerUI(float currentTime)
    {
        float timeLeft = timerDuration - currentTime;
        int minutes = Mathf.FloorToInt(timeLeft / 60);
        int seconds = Mathf.FloorToInt(timeLeft % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StartTimer()
    {
        if (IsServer)
        {
            startTime = Time.time;
            networkTime.Value = startTime;
        }
    }

    public void StopTimer()
    {
        if (IsServer)
        {
            networkTime.Value = timerDuration; // 타이머를 종료 값으로 설정
        }
    }
}

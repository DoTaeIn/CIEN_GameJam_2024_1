using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb_Controller : MonoBehaviour
{
    private CircleCollider2D _circleCollider;

    public int Explode_range = 10;
    public int damage = 10;

    private void Awake()
    {

        _circleCollider = GetComponent<CircleCollider2D>();
    }
    
    public void Explode()
    {
        _circleCollider.radius = Explode_range;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Player>().Hp -= damage;
        }
    }
}
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : NetworkBehaviour
{
    [SerializeField] private float maxHp;
    [SerializeField] private int exp;
    private float curHp;


    private void Awake()
    {
        curHp = maxHp;
    }

    [Server]
    public void TakeDamage(Player _player, float damage)
    {
        curHp -= damage;
        if (curHp <= 0)
        {
            _player.GainExp(exp);
            NetworkServer.Destroy(gameObject);
        }
    }
}
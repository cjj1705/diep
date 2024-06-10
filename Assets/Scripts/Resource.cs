using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Resource : NetworkBehaviour
{
    [SerializeField] private float maxHp;
    [SerializeField] private int exp;
    [SyncVar] private float curHp;

    [SerializeField] private Slider hpBar;

    public override void OnStartServer()
    {
        SyncHpBar();
    }

    private void Awake()
    {
        curHp = maxHp;
    }

    private void Start()
    {
        SyncHpBar();
    }

    [Server]
    public void TakeDamage(Player player, float damage)
    {
        RcpUpdateHealth(player, curHp - damage);
    }

    [ClientRpc]
    private void RcpUpdateHealth(Player player, float value)
    {
        curHp = value;
        SyncHpBar();
        if (curHp <= 0)
        {
            player.GainExp(exp);
            NetworkServer.Destroy(gameObject);
        }
    }

    private void SyncHpBar()
    {
        hpBar.value = curHp / maxHp;
    }
}
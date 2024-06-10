using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

public enum CharacterStat
{
    // 1초당 체력 재생
    HealthRegen,
    // 최대 체력
    MaxHealth,
    // 몸통박치기 데미지
    BodyDamage,
    // 총알 속도
    BulletSpeed,
    // 총알 관통력 : 1당 1개의 오브젝트를 통과
    BulletPenetration,
    // 총알 데미지
    BulletDamage,
    // 공격 딜레이
    Reload,
    // 이동 속도
    MovementSpeed
}

public class Player : NetworkBehaviour
{
    private Dictionary<CharacterStat, float> Stats;

    // 레벨
    private int level;
    // 다음 레벨에 도달하기 위한 경험치량
    private int exp = 10;
    // 현재 경험치량
    private int curExp = 0;
    // 스탯을 찍을 수 있는 횟수
    private int statPoint = 0;

    // 이동 방향
    private Vector2 moveDir;

    // 현재 체력
    private float curHp;
    private float curAttackTime;

    private float bulletLifeTime = 2f;

    [SerializeField] private GameObject bullet;
    [SerializeField] private Sprite otherPlayerSprite;

    private GameObject weapon;
    private Transform shootPoint;

    private void Awake()
    {
        Stats = new Dictionary<CharacterStat, float>();

        Stats[CharacterStat.HealthRegen] =  0f;
        Stats[CharacterStat.MaxHealth] = 10f;
        Stats[CharacterStat.BodyDamage] = 1f;
        Stats[CharacterStat.BulletSpeed] = 4f;
        Stats[CharacterStat.BulletPenetration] = 0f;
        Stats[CharacterStat.BulletDamage] = 1f;
        Stats[CharacterStat.Reload] = 0f;
        Stats[CharacterStat.MovementSpeed] = 3f;

        curHp = Stats[CharacterStat.MaxHealth];

        weapon = transform.GetChild(0).gameObject;
        shootPoint = transform.GetChild(0).GetChild(0).transform;
    }

    private void Start()
    {
        curAttackTime = Time.time;

        if (isLocalPlayer)
        {
            CmdUpdateOtherPlayerSprites();
        }

        StartCoroutine(RegenHealth(Stats[CharacterStat.HealthRegen]));
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Application.isFocused)
        {
            Move();
            Rotate();
            Shoot();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌한 객체가 플레이어인지 확인
        Player otherPlayer = collision.gameObject.GetComponent<Player>();
        if (otherPlayer != null)
        {
            // 서로에게 데미지를 입힘
            if (isServer)
            {
                TakeDamage(Stats[CharacterStat.BodyDamage]); // 서버에서만 데미지를 입힘
            }
        }
    }

    private void Move()
    {
        moveDir.x = Input.GetAxis("Horizontal");
        moveDir.y = Input.GetAxis("Vertical");

        CmdMove(moveDir);
    }

    [Command]
    private void CmdMove(Vector2 direction)
    {
        RpcMove(direction);
    }

    [ClientRpc]
    private void RpcMove(Vector2 direction)
    {
        transform.Translate(direction * Stats[CharacterStat.MovementSpeed] * Time.deltaTime);
    }

    private void Rotate()
    {
        Vector2 lookDir = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        CmdRotate(angle);
    }

    [Command]
    private void CmdRotate(float angle)
    {
        RpcRotate(angle);
    }

    [ClientRpc]
    private void RpcRotate(float angle)
    {
        if (!isLocalPlayer)
            return;

        weapon.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Shoot()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (Time.time > curAttackTime)
            {
                curAttackTime = Time.time + 1f - (Stats[CharacterStat.Reload] / 15f);
                CmdShoot(shootPoint.position, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
        }
    }

    [Command]
    private void CmdShoot(Vector2 bulletSpawnPosition, Vector2 targetPosition)
    {
        GameObject curBullet = Instantiate(bullet, bulletSpawnPosition, Quaternion.identity);
        Vector2 lookDir = targetPosition - bulletSpawnPosition;
        curBullet.GetComponent<Bullet>().Initialize(lookDir.normalized, 
            Stats[CharacterStat.BulletSpeed], Stats[CharacterStat.BulletPenetration], Stats[CharacterStat.BulletDamage], 
            bulletLifeTime, isLocalPlayer, (int)netId);
        NetworkServer.Spawn(curBullet);
        RpcSetUpBullet(curBullet, lookDir.normalized,
            Stats[CharacterStat.BulletSpeed], Stats[CharacterStat.BulletPenetration], Stats[CharacterStat.BulletDamage], 
            bulletLifeTime);
    }

    [ClientRpc]
    private void RpcSetUpBullet(GameObject bulletObject, Vector2 direction, float speed, float pernetration, float damage, float lifeTime)
    {
        bulletObject.GetComponent<Bullet>().Initialize(direction, speed, pernetration, damage, lifeTime, isLocalPlayer, (int)netId);
    }

    [Server]
    public void TakeDamage(float amount)
    {
        curHp -= amount;
        if (curHp <= 0)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    [Command]
    private void CmdUpdateOtherPlayerSprites()
    {
        // 모든 플레이어를 확인하여 스프라이트 변경 명령을 내림
        foreach (var player in FindObjectsOfType<Player>())
        {
            player.RpcChangeSprite();
        }
    }

    [ClientRpc]
    private void RpcChangeSprite()
    {
        foreach (var player in FindObjectsOfType<Player>())
        {
            if (player != this)
            {
                player.ChangeSprite();
            }
        }
    }

    private void ChangeSprite()
    {
        if (!isLocalPlayer)
        {
            GetComponent<SpriteRenderer>().sprite = otherPlayerSprite;
        }
    }

    private IEnumerator RegenHealth(float value)
    {
        yield return new WaitForSeconds(5f);
        curHp += value;
        if (curHp > Stats[CharacterStat.MaxHealth])
        {
            curHp = Stats[CharacterStat.MaxHealth];
        }

        StartCoroutine(RegenHealth(Stats[CharacterStat.HealthRegen]));
    }

    private void LevelUp()
    {
        statPoint++;
        exp = level * 10;

        // TODO : 스탯찍는 UI 띄우기
        // 스탯 버튼을 눌러서 스탯을 찍었을 때, 남은 스탯포인트가 없으면 UI 치우기
    }

    private void GainExp(int _exp)
    {
        curExp += _exp;
        if (curExp > exp)
        {
            LevelUp();
        }
    }
}
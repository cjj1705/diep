using UnityEngine;
using Mirror;
using System.Collections;

public class Player : NetworkBehaviour
{
    // 1초당 체력 재생
    private int healthRegen = 0;
    // 최대 체력
    private int maxHealth = 20;
    // 몸통박치기 데미지
    private int bodyDamage = 1;
    // 총알 속도
    private float bulletSpeed = 4f;
    // 총알 관통력 : 1당 1개의 오브젝트를 통과
    private int bulletPenetration = 0;
    // 총알 데미지
    private int bulletDamage = 1;
    // 공격 딜레이
    private int reload;
    // 이동 속도
    private float movementSpeed = 2f;



    // 이동 방향
    private Vector2 moveDir;

    private float bulletLifeTime = 2f;
    private int curHp;
    private float curAttackTime;

    [SerializeField] private GameObject bullet;
    [SerializeField] private Sprite otherPlayerSprite;

    private GameObject weapon;
    private Transform shootPoint;

    private void Awake()
    {
        curHp = maxHealth;

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

        StartCoroutine(RegenHealth(healthRegen));
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
                TakeDamage(bodyDamage); // 서버에서만 데미지를 입힘
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
        transform.Translate(direction * movementSpeed * Time.deltaTime);
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
                curAttackTime = Time.time + 1f - (reload / 15f);
                CmdShoot(shootPoint.position, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
        }
    }

    [Command]
    private void CmdShoot(Vector2 bulletSpawnPosition, Vector2 targetPosition)
    {
        GameObject curBullet = Instantiate(bullet, bulletSpawnPosition, Quaternion.identity);
        Vector2 lookDir = targetPosition - bulletSpawnPosition;
        curBullet.GetComponent<Bullet>().Initialize(lookDir.normalized, bulletSpeed, bulletPenetration, bulletDamage, bulletLifeTime, isLocalPlayer, (int)netId);
        NetworkServer.Spawn(curBullet);
        RpcSetUpBullet(curBullet, lookDir.normalized, bulletSpeed, bulletPenetration, bulletDamage, bulletLifeTime);
    }

    [ClientRpc]
    private void RpcSetUpBullet(GameObject bulletObject, Vector2 direction, float speed, int pernetration, int damage, float lifeTime)
    {
        bulletObject.GetComponent<Bullet>().Initialize(direction, speed, pernetration, damage, lifeTime, isLocalPlayer, (int)netId);
    }

    [Server]
    public void TakeDamage(int amount)
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

    private IEnumerator RegenHealth(int value)
    {
        yield return new WaitForSeconds(5f);
        curHp += value;
        if (curHp > maxHealth)
        {
            curHp = maxHealth;
        }

        StartCoroutine(RegenHealth(healthRegen));
    }
}
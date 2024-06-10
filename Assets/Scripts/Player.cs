using UnityEngine;
using Mirror;
using System.Collections;

public class Player : NetworkBehaviour
{
    // 1�ʴ� ü�� ���
    private int healthRegen = 0;
    // �ִ� ü��
    private int maxHealth = 20;
    // �����ġ�� ������
    private int bodyDamage = 1;
    // �Ѿ� �ӵ�
    private float bulletSpeed = 4f;
    // �Ѿ� ����� : 1�� 1���� ������Ʈ�� ���
    private int bulletPenetration = 0;
    // �Ѿ� ������
    private int bulletDamage = 1;
    // ���� ������
    private int reload;
    // �̵� �ӵ�
    private float movementSpeed = 2f;



    // �̵� ����
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
        // �浹�� ��ü�� �÷��̾����� Ȯ��
        Player otherPlayer = collision.gameObject.GetComponent<Player>();
        if (otherPlayer != null)
        {
            // ���ο��� �������� ����
            if (isServer)
            {
                TakeDamage(bodyDamage); // ���������� �������� ����
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
        // ��� �÷��̾ Ȯ���Ͽ� ��������Ʈ ���� ����� ����
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
using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour
{
    private Vector2 dir;
    private float moveSpeed = 2f;

    private float attackDelay = 0.5f;
    private float curAttackTime;

    [SerializeField] private GameObject playerBullet;
    [SerializeField] private GameObject enemyBullet;

    private GameObject weapon;
    private Transform shootPoint;

    private void Awake()
    {
        weapon = transform.GetChild(0).gameObject;
        shootPoint = transform.GetChild(0).GetChild(0).transform;
    }

    private void Start()
    {
        curAttackTime = Time.time;
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

    private void Move()
    {
        dir.x = Input.GetAxis("Horizontal");
        dir.y = Input.GetAxis("Vertical");

        CmdMove(dir);
    }

    [Command]
    private void CmdMove(Vector2 direction)
    {
        // 서버에서 모든 클라이언트에게 이동 명령을 보냄
        RpcMove(direction);
    }

    [ClientRpc]
    private void RpcMove(Vector2 direction)
    {
        // 클라이언트들이 서버로부터 받은 이동 명령에 따라 플레이어 이동
        transform.Translate(direction * moveSpeed * Time.deltaTime);
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
        // 서버에서 모든 클라이언트에게 이동 명령을 보냄
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
                curAttackTime = Time.time + attackDelay;
                CmdShoot(shootPoint.position, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
        }
    }

    [Command]
    private void CmdShoot(Vector2 bulletSpawnPosition, Vector2 targetPosition)
    {
        GameObject curBullet = Instantiate(playerBullet, bulletSpawnPosition, Quaternion.identity);
        Vector2 lookDir = targetPosition - bulletSpawnPosition;
        curBullet.GetComponent<Bullet>().Initialize(lookDir.normalized, 3f, 2f, isLocalPlayer);
        NetworkServer.Spawn(curBullet);
        RpcSetUpBullet(curBullet, lookDir.normalized, 3f, 2f);
        Debug.Log(isLocalPlayer + " : Command");
    }

    [ClientRpc]
    private void RpcSetUpBullet(GameObject bulletObject, Vector2 direction, float speed, float lifeTime)
    {
        bulletObject.GetComponent<Bullet>().Initialize(direction, speed, lifeTime, isLocalPlayer);
        Debug.Log(isLocalPlayer + " : ClientRpc");
    }
}
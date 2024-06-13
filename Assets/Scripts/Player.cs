using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum CharacterStat
{
    // 1초당 체력 재생
    HealthRegen = 0,
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
    [HideInInspector] public Dictionary<CharacterStat, int> UpgradeStats;

    // 레벨
    private int level = 1;
    // 다음 레벨에 도달하기 위한 경험치량
    private int exp = 10;
    // 현재 경험치량
    private int curExp = 0;
    private int totalExp = 0;
    // 스탯을 찍을 수 있는 횟수
    private int statPoint = 0;

    // 이동 방향
    private Vector2 moveDir;

    // 현재 체력
    [SyncVar] private float curHp;
    private float curAttackTime;

    private float bulletLifeTime = 2f;

    [SerializeField] private GameObject bullet;
    [SerializeField] private Sprite otherPlayerSprite;

    private GameObject weapon;
    private Transform shootPoint;

    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI playerName;

    private UIManager uiManager;

    private void Awake()
    {
        Stats = new Dictionary<CharacterStat, float>();
        Stats[CharacterStat.HealthRegen] = 0f;
        Stats[CharacterStat.MaxHealth] = 10f;
        Stats[CharacterStat.BodyDamage] = 1f;
        Stats[CharacterStat.BulletSpeed] = 10f;
        Stats[CharacterStat.BulletPenetration] = 0f;
        Stats[CharacterStat.BulletDamage] = 1f;
        Stats[CharacterStat.Reload] = 0f;
        Stats[CharacterStat.MovementSpeed] = 5f;

        UpgradeStats = new Dictionary<CharacterStat, int>();
        UpgradeStats[CharacterStat.HealthRegen] = 0;
        UpgradeStats[CharacterStat.MaxHealth] = 0;
        UpgradeStats[CharacterStat.BodyDamage] = 0;
        UpgradeStats[CharacterStat.BulletSpeed] = 0;
        UpgradeStats[CharacterStat.BulletPenetration] = 0;
        UpgradeStats[CharacterStat.BulletDamage] = 0;
        UpgradeStats[CharacterStat.Reload] = 0;
        UpgradeStats[CharacterStat.MovementSpeed] = 0;

        curHp = Stats[CharacterStat.MaxHealth];

        weapon = transform.GetChild(0).gameObject;
        shootPoint = transform.GetChild(0).GetChild(0).transform;

        uiManager = GameObject.Find("UI Manager").GetComponent<UIManager>();
    }

    private void Start()
    {
        curAttackTime = Time.time;
        SyncHpBar();

        if (isLocalPlayer)
        {
            CmdUpdateOtherPlayerSprites();
            StartCoroutine(RegenHealth(Stats[CharacterStat.HealthRegen]));
            uiManager.player = this;
        }

        playerName.text = "Player " + netId;
        uiManager.StartGame();
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

            if (Input.GetKeyDown(KeyCode.G))
            {
                GainExp(exp - curExp);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player otherPlayer = collision.gameObject.GetComponent<Player>();
            if (otherPlayer != null)
            {
                // 서로에게 데미지를 입힘
                if (isServer)
                {
                    TakeDamage(Stats[CharacterStat.BodyDamage]);
                }
            }
        }
        else if (collision.gameObject.CompareTag("Resource"))
        {
            Resource resource = collision.gameObject.GetComponent<Resource>();
            if (resource != null)
            {
                if (isServer)
                {
                    TakeDamage(1f);
                    resource.TakeDamage(this, Stats[CharacterStat.BodyDamage]);
                }
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
        if (Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Space))
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
        curBullet.GetComponent<Bullet>().Initialize(this, lookDir.normalized, 
            Stats[CharacterStat.BulletSpeed], Stats[CharacterStat.BulletPenetration], Stats[CharacterStat.BulletDamage], 
            bulletLifeTime, isLocalPlayer);
        NetworkServer.Spawn(curBullet);
        RpcSetUpBullet(curBullet, this, lookDir.normalized,
            Stats[CharacterStat.BulletSpeed], Stats[CharacterStat.BulletPenetration], Stats[CharacterStat.BulletDamage], 
            bulletLifeTime);
    }

    [ClientRpc]
    private void RpcSetUpBullet(GameObject bulletObject, Player _owner, Vector2 direction, float speed, float pernetration, float damage, float lifeTime)
    {
        bulletObject.GetComponent<Bullet>().Initialize(_owner, direction, speed, pernetration, damage, lifeTime, isLocalPlayer);
    }

    [Server]
    public void TakeDamage(float amount)
    {
        RpcUpdateHealth(curHp - amount);
    }

    [ClientRpc]
    private void RpcUpdateHealth(float health)
    {
        curHp = health;
        SyncHpBar();

        if (curHp <= 0)
        {
            NetworkServer.Destroy(gameObject);
        }
        else if (curHp > Stats[CharacterStat.MaxHealth])
        {
            curHp = Stats[CharacterStat.MaxHealth];
        }
    }

    private IEnumerator RegenHealth(float value)
    {
        yield return new WaitForSeconds(5f);

        CmdRegenHealth(value);
        StartCoroutine(RegenHealth(Stats[CharacterStat.HealthRegen]));
    }

    [Command]
    private void CmdRegenHealth(float value)
    {
        // 서버에서 체력 회복 처리
        curHp += value;
        if (curHp > 100f) // Assuming 100 is the max health
        {
            curHp = 100f;
        }

        // 클라이언트에 동기화
        RpcUpdateHealth(curHp);
    }

    private void SyncHpBar()
    {
        hpBar.value = curHp / Stats[CharacterStat.MaxHealth];
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
            hpBar.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = Color.red;
        }
    }

    private void LevelUp()
    {
        statPoint++;
        exp = 10 + level++ * 3;

        if (isLocalPlayer)
        {
            uiManager.LevelUp.SetActive(true);
            uiManager.UpdateLevel(level);
        }
    }

    public void GainExp(int _exp)
    {
        curExp += _exp;
        totalExp += _exp;

        while (curExp >= exp)
        {
            curExp -= exp;
            LevelUp();
        }

        if (isLocalPlayer)
        {
            uiManager.UpdateExp(curExp, exp);
        }
    }

    public void UpgradeStat(CharacterStat stat)
    {
        UpgradeStats[stat]++;
        Stats[stat] += stat switch
        {
            CharacterStat.HealthRegen => 1,
            CharacterStat.MaxHealth => 2,
            CharacterStat.BodyDamage => 0.2f,
            CharacterStat.BulletSpeed => 1f,
            CharacterStat.BulletPenetration => 1,
            CharacterStat.BulletDamage => 0.2f,
            CharacterStat.Reload => 0.75f,
            CharacterStat.MovementSpeed => 0.5f
        };

        if (--statPoint <= 0)
        {
            uiManager.LevelUp.SetActive(false);
        }
    }
}
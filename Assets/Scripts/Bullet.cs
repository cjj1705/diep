using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private Player owner;
    private Vector2 direction;
    private float speed;
    private float damage;
    private float pernetration;
    private float lifeTime;

    private bool initialized = false;

    [SerializeField] private Sprite otherBulletSprite;

    // Update 메서드로 인해 초기화가 여러 번 호출되는 것을 방지하기 위해 사용됩니다.
    public void Initialize(Player _owner, Vector2 _direction, float _speed, float _pernetration, float _damage, float _lifeTime, bool _isLocalPlayer)
    {
        if (!initialized)
        {
            owner = _owner;
            direction = _direction.normalized;
            speed = _speed;
            pernetration = _pernetration;
            damage = _damage;
            lifeTime = Time.time + _lifeTime;

            if (_isLocalPlayer)
            {
                tag = "PlayerBullet";
            }
            else
            {
                tag = "EnemyBullet";
                GetComponent<SpriteRenderer>().sprite = otherBulletSprite;
            }

            initialized = true;
        }
    }

    private void Update()
    {
        if (Time.time > lifeTime)
        {
            DestroyBullet();
        }
        else
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player hitPlayer = collision.gameObject.GetComponent<Player>();
            if (hitPlayer != null && hitPlayer != owner)
            {
                hitPlayer.TakeDamage(damage);
            }
        }
        else if (collision.gameObject.CompareTag("Resource"))
        {
            Resource resource = collision.gameObject.GetComponent<Resource>();
            resource.TakeDamage(owner, damage);
        }

        if (--pernetration < 0 || collision.gameObject.CompareTag("Wall"))
        {
            RpcDestroyBullet();
        }
    }

    [ClientRpc]
    private void RpcDestroyBullet()
    {
        DestroyBullet();
    }

    private void DestroyBullet()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
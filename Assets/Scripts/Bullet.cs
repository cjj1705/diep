using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifeTime;
    private int damage;
    private int ownerId;

    private bool initialized = false;

    [SerializeField] private Sprite otherBulletSprite;

    // Update 메서드로 인해 초기화가 여러 번 호출되는 것을 방지하기 위해 사용됩니다.
    public void Initialize(Vector2 _direction, float _speed, float _lifeTime, int _damage, bool _isLocalPlayer, int _netId)
    {
        if (!initialized)
        {
            direction = _direction.normalized;
            speed = _speed;
            lifeTime = Time.time + _lifeTime;
            damage = _damage;

            if (_isLocalPlayer)
            {
                tag = "PlayerBullet";
            }
            else
            {
                tag = "EnemyBullet";
                GetComponent<SpriteRenderer>().sprite = otherBulletSprite;
            }

            ownerId = _netId;

            initialized = true;
        }
    }

    public void SetColor(bool _isLocalPlayer)
    {

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
            if (hitPlayer != null && hitPlayer.netId != ownerId)
            {
                hitPlayer.TakeDamage(damage);
            }
        }

        RpcDestroyBullet();
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
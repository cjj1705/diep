using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifeTime;

    private bool initialized = false;

    // Update 메서드로 인해 초기화가 여러 번 호출되는 것을 방지하기 위해 사용됩니다.
    public void Initialize(Vector2 _direction, float _speed, float _lifeTime, bool _isLocalPlayer)
    {
        if (!initialized)
        {
            direction = _direction.normalized;
            speed = _speed;
            lifeTime = Time.time + _lifeTime;
            initialized = true;

            if (_isLocalPlayer)
            {
                tag = "PlayerBullet";
                GetComponent<SpriteRenderer>().color = Color.white;
            }
            else
            {
                tag = "EnemyBullet";
                GetComponent<SpriteRenderer>().color = Color.red;
            }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 클라이언트에서의 로직 처리
        if (!isClient)
            return;

        DestroyBullet();
    }

    private void DestroyBullet()
    {
        // 클라이언트에서의 로직 처리
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
using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    private Vector2 direction;
    private float speed;
    private float lifeTime;

    private bool initialized = false;

    // Update �޼���� ���� �ʱ�ȭ�� ���� �� ȣ��Ǵ� ���� �����ϱ� ���� ���˴ϴ�.
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
        // Ŭ���̾�Ʈ������ ���� ó��
        if (!isClient)
            return;

        DestroyBullet();
    }

    private void DestroyBullet()
    {
        // Ŭ���̾�Ʈ������ ���� ó��
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
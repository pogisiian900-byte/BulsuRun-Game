using UnityEngine;

public class EnemyBullets : MonoBehaviour
{

    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;

    [SerializeField] private int damage = 10;

    private Vector2 direction;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void setDirection(Vector2 dir)
    {
        direction = dir.normalized;
        Destroy(gameObject, lifeTime);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }
void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("Player"))
    {
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage,transform.position);
        }

        Destroy(gameObject);
    }
        else if (!collision.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
}

    
}

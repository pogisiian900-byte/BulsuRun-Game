using UnityEngine;

public class SkyBotRocket : MonoBehaviour
{
    [Header("Rocket Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private int damage = 10;
     public Vector2 direction = Vector2.left; 


    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        // Launch forward immediately
        ApplyVelocity();
        // Destroy after lifetime
        Destroy(gameObject, lifeTime);
    }

    public void ConfigureDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        ApplyVelocity();
    }

    private void ApplyVelocity()
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

   void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage,transform.position);
        }

        // Destroy on hitting anything tagged Wall/Ground
        if (other.CompareTag("Ground") || other.CompareTag("Player"))
        {
            if (explosionPrefab != null)
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}

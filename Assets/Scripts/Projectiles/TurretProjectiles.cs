using UnityEngine;

public class TurretProjectiles : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 10;
    [Header("Stun")]
    [SerializeField] private bool canStunPlayer;
    [SerializeField] private float stunDuration = 1f;

    private bool hasHit;

    public bool CanStunPlayer => canStunPlayer;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit || !collision.CompareTag("Player"))
            return;

        hasHit = true;

        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);

            if (canStunPlayer)
                playerHealth.Stun(stunDuration);
        }

        Destroy(gameObject);
    }
}

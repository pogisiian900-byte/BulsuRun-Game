using UnityEngine;

public class EnemyKill : MonoBehaviour
{
    [Header("Instant Kill")]
    public int killDamage = 9999; // enough to kill player

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ignore non-blocking hazards and other enemies so the chase bot keeps moving.
        if (collision.gameObject.CompareTag("Obstacle")
            || collision.gameObject.CompareTag("Damage")
            || collision.gameObject.CompareTag("Enemy"))
        {
            Physics2D.IgnoreCollision(collision.collider, GetComponent<Collider2D>());
            return;
        }

        // Player caught → GAME OVER via damage
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(killDamage, transform.position);
            }
        }
    }
}

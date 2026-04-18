using UnityEngine;

public class FireDamage : MonoBehaviour
{
    [SerializeField] private int damage = 10;

    private float damageCooldown = 1.5f;
private float lastDamageTime;

private void OnTriggerStay2D(Collider2D collision)
{
    if (collision.CompareTag("Player"))
    {
        if (Time.time >= lastDamageTime + damageCooldown)
        {
            PlayerHealth health = collision.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage,transform.position);
            }

            lastDamageTime = Time.time;
        }
    }
}
}

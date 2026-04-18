using UnityEngine;

public class ElectricMine : MonoBehaviour
{
    [Header("Mine Settings")]
    [SerializeField] private float stunDuration = 2f;
    [SerializeField] private int damage = 10;
    [SerializeField] private GameObject electricEffect; // optional VFX
    [SerializeField] private float destroyDelay = 0.2f;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            triggered = true;

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            PlayerHealth controller = other.GetComponent<PlayerHealth>();

            // Damage
            if (health != null)
                health.TakeDamage(damage,transform.position);

            // Stun
            if (controller != null)
                controller.Stun(stunDuration);

            // Effect
            if (electricEffect != null)
                {
                    GameObject vfx = Instantiate(electricEffect, transform.position, Quaternion.identity);
                    Destroy(vfx, 1.5f); // destroy VFX after 1.5 seconds
                }
            Destroy(gameObject, destroyDelay);
            
        }
    }
}
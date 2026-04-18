using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LandMineExplosionTrigger : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;

    [Header("Settings")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float delayBeforeExplosion = 0f;

    private bool triggered;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (triggerOnce && triggered) return;

        triggered = true;

        if (delayBeforeExplosion > 0f)
            Invoke(nameof(SpawnExplosion), delayBeforeExplosion);
        else
            SpawnExplosion();

        if (triggerOnce)
            Destroy(gameObject);
    }

    private void SpawnExplosion()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
    }
}

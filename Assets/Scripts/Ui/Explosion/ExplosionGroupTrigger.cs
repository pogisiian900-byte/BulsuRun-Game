using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ExplosionGroupTrigger : MonoBehaviour
{
    [Header("What to activate")]
    [SerializeField] private GameObject explosionsRoot; // drag ExplosionGroup here

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    private bool triggered;

    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        if (explosionsRoot != null)
            explosionsRoot.SetActive(false); // keep off until player crosses
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && triggered) return;

        triggered = true;

        if (explosionsRoot != null)
            explosionsRoot.SetActive(true); // activates ALL explosions

        if (triggerOnce)
            Destroy(gameObject);
    }
}
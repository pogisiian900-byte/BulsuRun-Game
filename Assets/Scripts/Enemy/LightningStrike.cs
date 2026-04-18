using UnityEngine;

public class LightningStrike : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float maxLength = 50f;
    [SerializeField] private LayerMask groundLayer;
[Header("Stun")]
[SerializeField] private float stunDuration = 0.5f;
    [Header("Visual")]
     private SpriteRenderer sr;
     private BoxCollider2D boxCollider;
    [SerializeField] private float visualWidth = 2f;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 0.35f;

   private void Awake()
{
    // cache components BEFORE Init can be called
    sr = GetComponent<SpriteRenderer>();
    boxCollider = GetComponent<BoxCollider2D>();

    transform.localScale = Vector3.one;
}


    public void Init(Vector2 startPos)
    {
        // Find ground
        RaycastHit2D groundHit = Physics2D.Raycast(
            startPos,
            Vector2.down,
            maxLength,
            groundLayer
        );

        float length = groundHit.collider != null
            ? Vector2.Distance(startPos, groundHit.point)
            : maxLength;

        // Position lightning center
        transform.position = startPos + Vector2.down * (length * 0.5f);

        // Resize sprite (SpriteRenderer must be Tiled)
        if (sr != null)
            sr.size = new Vector2(visualWidth, length);

        // Resize collider to match sprite
        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(visualWidth, length);
            boxCollider.offset = Vector2.zero;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    if (other.CompareTag("Player"))
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.Stun(stunDuration);
        }
    }
}
}

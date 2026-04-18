using UnityEngine;

public class FloatingEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private int direction = 1; // 1 = right, -1 = left

    [Header("Floating")]
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatSpeed = 3f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 5f; // seconds

    [Header("Collision")]
    [SerializeField] private LayerMask barrierLayer;

    private SpriteRenderer spriteRenderer;
    private Vector3 startPos;
    private float timer;

    public void SetDirection(int newDirection)
    {
        direction = newDirection >= 0 ? 1 : -1;

        if (spriteRenderer != null)
            spriteRenderer.flipX = direction > 0;
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPos = transform.position;

        // Flip based on direction
        spriteRenderer.flipX = direction < 0;
    }

    void Update()
    {
        // Movement
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
        spriteRenderer.flipX = direction > 0;

        float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, startPos.y + floatY, transform.position.z);

        // Lifetime countdown
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleImpact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleImpact(other);
    }

    private void HandleImpact(Collider2D other)
    {
        if (other == null)
            return;

        // Remove the projectile when it touches configured barriers, even as a trigger.
        if (((1 << other.gameObject.layer) & barrierLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}

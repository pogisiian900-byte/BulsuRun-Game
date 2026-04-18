using UnityEngine;

public class EnemyPatrolling : MonoBehaviour
{
 
    [SerializeField] private float speed = 2f;
    [SerializeField] private int direction = -1; // -1 = left, +1 = right
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask spikesLayer;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
    }

   private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.CompareTag("Player"))
    {
        PlayerHealth player = collision.GetComponent<PlayerHealth>();

        if (player != null)
        {
            player.TakeHeartDamage(1,transform.position); // or TakeDamage depending on your system
        }

        TurnAround();
        return;
    }
}

    private void OnCollisionEnter2D(Collision2D collision)
        {
            int hitLayer = collision.gameObject.layer;

        if (((1 << hitLayer) & wallLayer) != 0 ||
            ((1 << hitLayer) & spikesLayer) != 0)
        {
            
            TurnAround();
        }
    }   
    void TurnAround()
    {
        direction *= -1;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (direction > 0 ? 1 : -1);
        transform.localScale = s;
    }
}

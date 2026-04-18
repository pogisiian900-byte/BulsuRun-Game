using UnityEngine;

public class RunningNPC : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private int direction = 1; // 1 = right, -1 = left
    [SerializeField] private bool spriteFacesLeftByDefault = true;

    [Header("Wall Detection")]
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        UpdateFacing();
    }

    void Update()
    {
        rb.linearVelocity = new Vector2(speed * direction, rb.linearVelocity.y);
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if (direction == 0)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        float scaleX = Mathf.Abs(scale.x);
        bool movingRight = direction > 0;

        scale.x = spriteFacesLeftByDefault
            ? (movingRight ? -scaleX : scaleX)
            : (movingRight ? scaleX : -scaleX);

        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If it hits something in the Wall layer
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}

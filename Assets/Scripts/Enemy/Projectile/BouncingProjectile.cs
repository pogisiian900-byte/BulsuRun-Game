    using UnityEngine;

    [RequireComponent(typeof(Rigidbody2D))]
    public class BouncingProjectile : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float speed = 12f;

        [Header("Destroy")]
        [SerializeField] private LayerMask destroyLayer;
        [SerializeField] private float lifeTime = 40f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        public void Launch(Vector2 direction)
        {
            rb.linearVelocity = direction.normalized * speed;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Check if collided object is in destroyLayer
            if ((destroyLayer.value & (1 << collision.gameObject.layer)) > 0)
            {
                Destroy(gameObject);
                return;
            }

            // Keep speed consistent after bounce
            if (rb.linearVelocity.sqrMagnitude > 0.01f)
                rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
    }
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class DodgeBallProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private LayerMask destroyLayer;
    [SerializeField] private float lifeTime = 6f;

    [Header("Ball Behavior")]
    [SerializeField] private bool knockoutBall = true; // true = OUT + respawn, false = damage only

    [Header("Hit Message")]
    [SerializeField] private bool showOutMessage = true;
    [SerializeField] private Sprite outPortrait;
    [SerializeField] private string outName = "FIT BOT";
    [SerializeField] private string outText = "YOU'RE OUT!!!";
    [SerializeField] private float outSeconds = 1.0f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool hitPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
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
        if (hitPlayer) return;

        Rigidbody2D hitRb = collision.collider.attachedRigidbody;

        if (hitRb != null && hitRb.CompareTag("Player"))
        {
            hitPlayer = true;

            if (knockoutBall)
            {
                // PLAYER IS OUT
                if (showOutMessage)
                {
                    DialogueManager.Instance?.ShowQuickMessage(
                        outName,
                        outPortrait,
                        outText,
                        outSeconds,
                        true
                    );
                }

                float delay = showOutMessage ? outSeconds : 0f;
                StartCoroutine(RespawnAfterDelay(hitRb.transform, delay));
            }
            else
            {
                // DAMAGE ONLY
                PlayerHealth health = hitRb.GetComponent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(10,transform.position);

                Destroy(gameObject);
            }

            DisableProjectile();
            return;
        }

        // Destroy on walls/ground
        if ((destroyLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            Destroy(gameObject);
            return;
        }

        // Keep constant speed after bouncing
        rb.linearVelocity = rb.linearVelocity.normalized * speed;
    }

    private void DisableProjectile()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (col) col.enabled = false;
        if (sr) sr.enabled = false;
    }

    private IEnumerator RespawnAfterDelay(Transform playerRoot, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (playerRoot == null) yield break;

        if (SpawnPlayer.Instance == null)
        {
            yield break;
        }

        SpawnPlayer.Instance.RespawnPlayer(playerRoot);

        Destroy(gameObject);
    }
}

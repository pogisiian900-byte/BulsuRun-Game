using System.Collections;
using UnityEngine;

public class BossKnockback : MonoBehaviour
{
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private bool knockedBack;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 hitSource)
    {
        if (!knockedBack)
            StartCoroutine(KnockbackRoutine(hitSource));
    }

    IEnumerator KnockbackRoutine(Vector2 hitSource)
    {
        knockedBack = true;

        Vector2 dir = ((Vector2)transform.position - hitSource).normalized;

        rb.linearVelocity = dir * knockbackForce;

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector2.zero;

        knockedBack = false;
    }
}
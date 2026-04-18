using UnityEngine;

public class TrashProjectile : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float lifeTime = 6f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Hit ground → disappear
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Destroy(gameObject);
            return;
        }

        // Hit player
        PlayerHealth ph = collision.collider.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeHeartDamage(1,transform.position);
            Destroy(gameObject);
        }
    }
}

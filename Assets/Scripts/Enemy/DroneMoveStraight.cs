using UnityEngine;

public class DroneMoveStraight : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public Vector2 direction = Vector2.right; // default = move right

    private int wallLayer = -1;

    private void Awake()
    {
        wallLayer = LayerMask.NameToLayer("Wall");
    }

    private void Update()
    {
        // Move in a fixed direction
        transform.Translate(direction.normalized * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsWall(other.gameObject))
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsWall(collision.gameObject))
            Destroy(gameObject);
    }

    private bool IsWall(GameObject otherObject)
    {
        if (otherObject == null)
            return false;

        return otherObject.CompareTag("Wall") ||
               (wallLayer >= 0 && otherObject.layer == wallLayer);
    }
}

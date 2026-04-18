using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float distance = 5f;
    public float speed = 2f;
    public Vector2 direction = Vector2.right;

    private Vector3 startPos;
    private Vector3 endPos;
    private bool goingToEnd = true;
    private readonly HashSet<Transform> riders = new HashSet<Transform>();

    void Start()
    {
        startPos = transform.position;
        endPos = startPos + (Vector3)(direction.normalized * distance);
    }

    void Update()
    {
        Vector3 target = goingToEnd ? endPos : startPos;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) < 0.05f)
            goingToEnd = !goingToEnd;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Transform playerTransform = GetPlayerTransform(collision);
        if (playerTransform == null)
            return;

        riders.Add(playerTransform);

        if (playerTransform.parent != transform)
            playerTransform.SetParent(transform, true);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Transform playerTransform = GetPlayerTransform(collision);
        if (playerTransform == null)
            return;

        riders.Remove(playerTransform);
        DetachPlayer(playerTransform);
    }

    private void OnDisable()
    {
        foreach (Transform rider in riders)
        {
            DetachPlayer(rider);
        }

        riders.Clear();
    }

    private Transform GetPlayerTransform(Collision2D collision)
    {
        if (collision == null)
            return null;

        Rigidbody2D playerBody = collision.rigidbody;
        if (playerBody != null && playerBody.CompareTag("Player"))
            return playerBody.transform;

        if (collision.collider != null && collision.collider.CompareTag("Player"))
            return collision.collider.transform;

        return null;
    }

    private void DetachPlayer(Transform playerTransform)
    {
        if (playerTransform == null || playerTransform.parent != transform)
            return;

        PlayerMovement movement = playerTransform.GetComponent<PlayerMovement>();
        if (movement != null && movement.isActiveAndEnabled)
        {
            movement.StartCoroutine(DetachAtEndOfPhysicsStep(playerTransform, transform));
            return;
        }

        SafeDetach(playerTransform, transform);
    }

    private static IEnumerator DetachAtEndOfPhysicsStep(Transform playerTransform, Transform platformTransform)
    {
        yield return new WaitForFixedUpdate();

        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (TryDetach(playerTransform, platformTransform))
                yield break;

            yield return null;
        }
    }

    private static void SafeDetach(Transform playerTransform, Transform platformTransform)
    {
        TryDetach(playerTransform, platformTransform);
    }

    private static bool TryDetach(Transform playerTransform, Transform platformTransform)
    {
        if (playerTransform == null || platformTransform == null || playerTransform.parent != platformTransform)
            return true;

        try
        {
            playerTransform.SetParent(null, true);
            return true;
        }
        catch (UnityException)
        {
            // Unity can reject SetParent while the platform hierarchy is toggling active state.
            // The coroutine retries on the next frame if that happens.
            return false;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(direction.normalized * distance);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(start, 0.2f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(end, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
    }
}

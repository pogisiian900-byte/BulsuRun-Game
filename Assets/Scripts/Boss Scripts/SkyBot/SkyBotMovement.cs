using System.Collections;
using UnityEngine;

public class SkyBotMovement : MonoBehaviour
{
    [Header("Movement Areas")]
    [SerializeField] private BoxCollider2D phase1And2Area;
    [SerializeField] private BoxCollider2D phase3Area;
    [SerializeField] private Transform phase3LastStand;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTime = 1.5f;
    [SerializeField] private float arrivalDistance = 0.1f;

    private Vector3 targetPoint;
    private BoxCollider2D currentArea;
    private Coroutine moveRoutine;
    private Rigidbody2D rb;
    private bool holdPhase3LastStand;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ResetToDefaultArea();
        BeginMovement();
    }

    private void OnDisable()
    {
        StopMovement();
    }

    public void BeginMovement()
    {
        if (!isActiveAndEnabled || moveRoutine != null)
            return;

        moveRoutine = StartCoroutine(MoveLoop());
    }

    public void StopMovement()
    {
        if (moveRoutine == null)
            return;

        StopCoroutine(moveRoutine);
        moveRoutine = null;
    }

    public void ResetToDefaultArea()
    {
        holdPhase3LastStand = false;
        currentArea = phase1And2Area;
    }

    public void SwitchToPhase3Area()
    {
        currentArea = phase3Area;
    }

    public void EnterPhase3LastStand()
    {
        StopMovement();
        SwitchToPhase3Area();
        holdPhase3LastStand = true;
        ZeroVelocity();
    }

    public bool IsAtPhase3LastStand()
    {
        return Vector2.Distance(transform.position, GetPhase3LastStandPosition()) <= arrivalDistance;
    }

    private IEnumerator MoveLoop()
    {
        while (true)
        {
            ChooseNewPoint();

            while (Vector2.Distance(transform.position, targetPoint) > 0.2f)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    targetPoint,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            yield return new WaitForSeconds(waitTime);
        }
    }

    private void ChooseNewPoint()
    {
        if (currentArea == null)
            return;

        Bounds bounds = currentArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        targetPoint = new Vector3(x, y, transform.position.z);
    }

    private Vector3 GetPhase3LastStandPosition()
    {
        if (phase3LastStand != null)
            return new Vector3(phase3LastStand.position.x, phase3LastStand.position.y, transform.position.z);

        if (phase3Area != null)
            return new Vector3(phase3Area.bounds.center.x, phase3Area.bounds.center.y, transform.position.z);

        return transform.position;
    }

    private void LateUpdate()
    {
        if (!holdPhase3LastStand)
            return;

        Vector3 destination = GetPhase3LastStandPosition();

        transform.position = Vector2.MoveTowards(
            transform.position,
            destination,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, destination) <= arrivalDistance)
            transform.position = destination;

        ZeroVelocity();
    }

    private void ZeroVelocity()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (phase1And2Area != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(phase1And2Area.bounds.center, phase1And2Area.bounds.size);
        }

        if (phase3Area != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(phase3Area.bounds.center, phase3Area.bounds.size);
        }

        if (phase3LastStand != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(phase3LastStand.position, 0.35f);
        }
    }
}

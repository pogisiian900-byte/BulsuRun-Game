using UnityEngine;

public class DronePatrol : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float arriveDistance = 0.05f;

    private Transform target;

    void Start()
    {
        target = pointB; // start moving toward B
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        // Move toward target
        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        // When close enough, switch target
        if (Vector2.Distance(transform.position, target.position) <= arriveDistance)
        {
            target = (target == pointA) ? pointB : pointA;
        }
    }

    #if UNITY_EDITOR
void OnDrawGizmos()
{
    if (pointA == null || pointB == null) return;

    Gizmos.color = Color.cyan;

    Gizmos.DrawLine(pointA.position, pointB.position);

    // Draw point markers
    Gizmos.DrawSphere(pointA.position, 0.1f);
    Gizmos.DrawSphere(pointB.position, 0.1f);
}
#endif

}

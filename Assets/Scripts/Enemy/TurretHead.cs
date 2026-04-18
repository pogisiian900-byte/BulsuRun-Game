using UnityEngine;
using UnityEngine.SceneManagement;

public class TurretHead : MonoBehaviour
{
    private const float TargetSearchRetryInterval = 0.5f;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("Rotate")]
    [SerializeField] private float rotationSpeed = 5f;

    private float nextTargetSearchTime;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindPlayerTarget();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayerTarget();
    }

    private void Update()
    {
        // If target got destroyed or not set yet, try finding again
        if (target == null)
        {
            if (Time.time < nextTargetSearchTime)
            {
                return;
            }

            nextTargetSearchTime = Time.time + TargetSearchRetryInterval;
            FindPlayerTarget();
            if (target == null) return;
        }

        Vector2 dir = target.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.Euler(0, 0, angle),
            Time.deltaTime * rotationSpeed
        );
    }

    private void FindPlayerTarget()
    {
        // Prefer tag (fast + clean)
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);

        // Fallback: find by component if tag missing
        if (p == null)
        {
            // CHANGED: Look for PlayerMovement instead of the old PlayerController
            var pm = FindObjectOfType<PlayerMovement>(true);
            if (pm != null) p = pm.gameObject;
        }

        target = (p != null) ? p.transform : null;
    }
}

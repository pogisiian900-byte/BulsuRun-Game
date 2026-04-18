using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public class CameraFollowSetter : MonoBehaviour
{
    [Header("Multiplayer Framing")]
    [SerializeField] private float targetRefreshInterval = 0.5f;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float minOrthographicSize = 7f;
    [SerializeField] private float maxOrthographicSize = 12f;
    [SerializeField] private float horizontalPadding = 3f;
    [SerializeField] private float verticalPadding = 2f;
    [SerializeField] private bool stretchCameraToFitPlayers = false;

    private readonly List<Transform> playerTargets = new List<Transform>();
    private CinemachineCamera cinemachineCamera;
    private Camera cachedMainCamera;
    private Transform runtimeFollowTarget;
    private Vector3 followVelocity;
    private float targetOrthographicSize;
    private float refreshTimer;

    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        cachedMainCamera = Camera.main;

        if (cinemachineCamera != null)
        {
            minOrthographicSize = cinemachineCamera.Lens.OrthographicSize;
            maxOrthographicSize = Mathf.Max(maxOrthographicSize, minOrthographicSize);
            targetOrthographicSize = minOrthographicSize;
        }
    }

    private void Start()
    {
        CreateRuntimeFollowTarget();
        RefreshTargets();
        UpdateCameraTarget(immediate: true);
    }

    private void LateUpdate()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= targetRefreshInterval || playerTargets.Count == 0)
        {
            refreshTimer = 0f;
            RefreshTargets();
        }

        UpdateCameraTarget(immediate: false);
    }

    private void CreateRuntimeFollowTarget()
    {
        if (runtimeFollowTarget != null)
        {
            return;
        }

        GameObject followTarget = new GameObject("RuntimeCameraTarget");
        runtimeFollowTarget = followTarget.transform;
        runtimeFollowTarget.position = transform.position;
    }

    private void RefreshTargets()
    {
        playerTargets.Clear();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player != null && player.activeInHierarchy)
            {
                playerTargets.Add(player.transform);
            }
        }

        if (cinemachineCamera != null && runtimeFollowTarget != null)
        {
            cinemachineCamera.Follow = runtimeFollowTarget;
        }
    }

    private void UpdateCameraTarget(bool immediate)
    {
        if (runtimeFollowTarget == null || cinemachineCamera == null || playerTargets.Count == 0)
        {
            return;
        }

        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }

        Bounds targetBounds = new Bounds(playerTargets[0].position, Vector3.zero);
        foreach (Transform target in playerTargets)
        {
            if (target != null)
            {
                targetBounds.Encapsulate(target.position);
            }
        }

        Vector3 desiredPosition = targetBounds.center;
        if (immediate)
        {
            runtimeFollowTarget.position = desiredPosition;
        }
        else
        {
            runtimeFollowTarget.position = Vector3.SmoothDamp(
                runtimeFollowTarget.position,
                desiredPosition,
                ref followVelocity,
                smoothTime);
        }

        float desiredSize = minOrthographicSize;
        if (stretchCameraToFitPlayers)
        {
            float halfWidth = targetBounds.extents.x + horizontalPadding;
            float halfHeight = targetBounds.extents.y + verticalPadding;
            float aspect = Mathf.Max(cachedMainCamera != null ? cachedMainCamera.aspect : 16f / 9f, 0.01f);
            float sizeFromWidth = halfWidth / aspect;
            desiredSize = Mathf.Clamp(
                Mathf.Max(minOrthographicSize, halfHeight, sizeFromWidth),
                minOrthographicSize,
                maxOrthographicSize);
        }

        if (immediate)
        {
            targetOrthographicSize = desiredSize;
        }
        else
        {
            targetOrthographicSize = Mathf.Lerp(targetOrthographicSize, desiredSize, Time.deltaTime * 5f);
        }

        LensSettings lens = cinemachineCamera.Lens;
        lens.OrthographicSize = targetOrthographicSize;
        cinemachineCamera.Lens = lens;
    }

    public void SnapToPlayersImmediate()
    {
        RefreshTargets();
        followVelocity = Vector3.zero;
        UpdateCameraTarget(immediate: true);
    }
}

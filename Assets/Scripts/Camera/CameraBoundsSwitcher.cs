using UnityEngine;
using Unity.Cinemachine;

public class CameraBoundsSwitcher : MonoBehaviour
{
    private const float CameraClampGraceDuration = 0.2f;

    public CinemachineConfiner2D confiner;
    private Collider2D triggerBounds;
    private CameraFollowSetter cameraFollowSetter;

    private void Awake()
    {
        triggerBounds = GetComponent<Collider2D>();
        cameraFollowSetter = FindFirstObjectByType<CameraFollowSetter>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject playerObject = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (!playerObject.CompareTag("Player") || confiner == null || triggerBounds == null)
        {
            return;
        }

        confiner.BoundingShape2D = triggerBounds;
        confiner.InvalidateCache();

        PlayerMovement movement = playerObject.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.SuspendCameraClamp(CameraClampGraceDuration);
        }

        if (cameraFollowSetter == null)
        {
            cameraFollowSetter = FindFirstObjectByType<CameraFollowSetter>();
        }

        if (cameraFollowSetter != null)
        {
            cameraFollowSetter.SnapToPlayersImmediate();
        }
    }
}

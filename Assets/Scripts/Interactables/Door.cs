using Photon.Pun;
using Photon.Realtime;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class Door : MonoBehaviour
{
    private const float TeleportCameraClampGraceDuration = 0.35f;
    private static Door activeDoor;

    [Header("Door Settings")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Collider2D newCameraBounds;

    [Header("Optional Info Card")]
    [SerializeField] private GameObject infoCard;

    [Header("UI")]
    [SerializeField] private GameObject doorButtonObject;

    private Button doorButton;
    private bool playerInRange;
    private Transform player;

    private CinemachineConfiner2D confiner;

    private void Awake()
    {
        if (infoCard != null)
            infoCard.SetActive(false);

        if (doorButtonObject != null)
        {
            doorButtonObject.SetActive(false);
            doorButton = doorButtonObject.GetComponent<Button>();
        }
    }

    private void Start()
    {
        confiner = FindObjectOfType<CinemachineConfiner2D>();
    }

    private void EnterRoom()
    {
        if (!playerInRange || player == null) return;

        if (PhotonNetwork.InRoom)
        {
            PhotonView playerView = player.GetComponent<PhotonView>();
            if (playerView == null)
            {
                playerView = player.GetComponentInParent<PhotonView>();
            }

            if (playerView != null)
            {
                playerView.RPC(nameof(PlayerHealth.SynchronizeDoorEntryRpc), RpcTarget.All, GetDoorIdentifier());
            }

            return;
        }

        EnterRoomLocally(player);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out Transform localPlayer)) return;

        if (activeDoor != null && activeDoor != this)
            activeDoor.ClearInteractionState();

        playerInRange = true;
        player = localPlayer;
        activeDoor = this;

        if (infoCard != null)
            infoCard.SetActive(true);

        ShowDoorButton();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out Transform localPlayer) || localPlayer != player) return;

        ClearInteractionState();
    }

    private void OnDisable()
    {
        if (activeDoor == this)
            activeDoor = null;

        HideDoorButton();
    }

    private void ClearInteractionState()
    {
        playerInRange = false;
        player = null;

        if (infoCard != null)
            infoCard.SetActive(false);

        if (activeDoor == this)
            activeDoor = null;

        HideDoorButton();
    }

    private void ShowDoorButton()
    {
        if (doorButtonObject == null || doorButton == null) return;

        doorButton.onClick.RemoveListener(EnterRoom);
        doorButton.onClick.AddListener(EnterRoom);
        doorButtonObject.SetActive(true);
    }

    private void HideDoorButton()
    {
        if (doorButton != null)
            doorButton.onClick.RemoveListener(EnterRoom);

        if (doorButtonObject != null)
            doorButtonObject.SetActive(false);
    }

    public void SynchronizeLocalPlayerEntry()
    {
        if (PhotonNetwork.InRoom)
        {
            SynchronizeMultiplayerEntry();
            return;
        }

        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(out Transform localPlayer))
        {
            return;
        }

        EnterRoomLocally(localPlayer);
    }

    public string GetDoorIdentifier()
    {
        return BuildTransformPath(transform);
    }

    public static Door FindDoorByIdentifier(string doorIdentifier)
    {
        if (string.IsNullOrWhiteSpace(doorIdentifier))
        {
            return null;
        }

        Door[] doors = FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Door door in doors)
        {
            if (door != null && string.Equals(door.GetDoorIdentifier(), doorIdentifier, System.StringComparison.Ordinal))
            {
                return door;
            }
        }

        return null;
    }

    private void EnterRoomLocally(Transform targetPlayer)
    {
        if (targetPlayer == null)
        {
            return;
        }

        TeleportPlayerToRoomSpawn(targetPlayer);
        FinalizeDoorEntry();
    }

    private Vector3 ResolveSpawnPosition(Transform targetPlayer)
    {
        if (playerSpawnPoint == null)
        {
            return targetPlayer.position;
        }

        if (!PhotonNetwork.InRoom)
        {
            return playerSpawnPoint.position;
        }

        PhotonView view = targetPlayer.GetComponent<PhotonView>();
        if (view == null)
        {
            view = targetPlayer.GetComponentInParent<PhotonView>();
        }

        Player owner = view != null ? view.Owner : PhotonNetwork.LocalPlayer;
        SpawnPlayer spawnPlayer = SpawnPlayer.Instance != null
            ? SpawnPlayer.Instance
            : FindFirstObjectByType<SpawnPlayer>();

        return spawnPlayer != null
            ? spawnPlayer.GetSpawnPositionForPlayer(owner, playerSpawnPoint.position)
            : playerSpawnPoint.position;
    }

    private static string BuildTransformPath(Transform current)
    {
        if (current == null)
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder(current.name);
        Transform parent = current.parent;

        while (parent != null)
        {
            builder.Insert(0, '/');
            builder.Insert(0, parent.name);
            parent = parent.parent;
        }

        return builder.ToString();
    }

    private void SynchronizeMultiplayerEntry()
    {
        PhotonView[] playerViews = FindObjectsByType<PhotonView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (PhotonView view in playerViews)
        {
            if (!IsTrackedMultiplayerPlayer(view))
            {
                continue;
            }

            TeleportPlayerToRoomSpawn(view.transform);
        }

        FinalizeDoorEntry();
    }

    private void TeleportPlayerToRoomSpawn(Transform targetPlayer)
    {
        if (targetPlayer == null)
        {
            return;
        }

        Rigidbody2D rb = targetPlayer.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = targetPlayer.GetComponentInParent<Rigidbody2D>();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        targetPlayer.position = ResolveSpawnPosition(targetPlayer);

        PlayerMovement movement = targetPlayer.GetComponent<PlayerMovement>();
        if (movement == null)
        {
            movement = targetPlayer.GetComponentInParent<PlayerMovement>();
        }

        if (movement != null)
        {
            movement.SuspendCameraClamp(TeleportCameraClampGraceDuration);
        }
    }

    private void FinalizeDoorEntry()
    {
        if (confiner != null && newCameraBounds != null)
        {
            confiner.BoundingShape2D = newCameraBounds;
            confiner.InvalidateCache();
        }

        CameraFollowSetter cameraFollowSetter = FindFirstObjectByType<CameraFollowSetter>();
        if (cameraFollowSetter != null)
        {
            cameraFollowSetter.SnapToPlayersImmediate();
        }

        if (infoCard != null)
            infoCard.SetActive(false);

        HideDoorButton();

        playerInRange = false;
        player = null;

        if (activeDoor == this)
            activeDoor = null;
    }

    private static bool IsTrackedMultiplayerPlayer(PhotonView view)
    {
        return view != null
            && view.ViewID != 0
            && view.gameObject.activeInHierarchy
            && view.gameObject.CompareTag("Player");
    }
}

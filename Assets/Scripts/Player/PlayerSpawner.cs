using System;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SpawnPlayer : MonoBehaviourPunCallbacks
{
    private const string DefaultPlayerOnePrefabName = "Player Boy";
    private const string DefaultPlayerTwoPrefabName = "Player Girl";
    private const string PlayerLayerName = "Player";
    private const float MultiplayerSpawnTimeout = 5f;
    private const float MultiplayerSpawnSettleDuration = 1f;
    private const float SpawnAlignmentTolerance = 0.05f;
    private const float RespawnCameraClampGraceDuration = 0.35f;

    public static SpawnPlayer Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private string playerOnePrefabName = DefaultPlayerOnePrefabName;
    [SerializeField] private string playerTwoPrefabName = DefaultPlayerTwoPrefabName;
    [SerializeField] private float multiplayerSpawnSpacing = 1.5f;
    [SerializeField] private Vector2 multiplayerSpawnDirection = Vector2.right;

    private bool hasSpawned;
    private Coroutine multiplayerSpawnRoutine;

    private void Awake()
    {
        Instance = this;
        IgnorePlayerSelfCollision();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Multiplayer detected -> checking for local player spawn");
            CleanupStaleSinglePlayerPlayers();
            QueueMultiplayerSpawnCheck();
            return;
        }

        Debug.Log("Singleplayer/off-room detected -> ensuring local player exists");
        SpawnSinglePlayer();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room -> spawning multiplayer player");
        CleanupStaleSinglePlayerPlayers();
        QueueMultiplayerSpawnCheck();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.InRoom)
        {
            QueueMultiplayerSpawnCheck();
        }
    }

    private void QueueMultiplayerSpawnCheck()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        CleanupStaleSinglePlayerPlayers();

        if (multiplayerSpawnRoutine != null)
        {
            StopCoroutine(multiplayerSpawnRoutine);
        }

        multiplayerSpawnRoutine = StartCoroutine(EnsureMultiplayerPlayerSpawned());
    }

    private IEnumerator EnsureMultiplayerPlayerSpawned()
    {
        float timeout = MultiplayerSpawnTimeout;
        float settleTimer = 0f;

        while (PhotonNetwork.InRoom && timeout > 0f)
        {
            PhotonView localPlayerView = FindLocalMultiplayerPlayer();
            if (localPlayerView != null)
            {
                hasSpawned = true;

                if (TryAlignPlayerToSpawn(localPlayerView.transform))
                {
                    settleTimer = 0f;
                }
                else
                {
                    settleTimer += Time.unscaledDeltaTime;
                    if (settleTimer >= MultiplayerSpawnSettleDuration)
                    {
                        multiplayerSpawnRoutine = null;
                        yield break;
                    }
                }
            }
            else
            {
                settleTimer = 0f;
                SpawnMultiplayerPlayer();
            }

            yield return null;
            timeout -= Time.unscaledDeltaTime;
        }

        PhotonView finalLocalPlayerView = FindLocalMultiplayerPlayer();
        if (finalLocalPlayerView != null)
        {
            TryAlignPlayerToSpawn(finalLocalPlayerView.transform, forceRespawn: true);
        }

        multiplayerSpawnRoutine = null;
    }

    private void SpawnMultiplayerPlayer()
    {
        if (hasSpawned)
        {
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("Tried to spawn but not in room!");
            return;
        }

        CleanupStaleSinglePlayerPlayers();

        PhotonView existingLocalPlayer = FindLocalMultiplayerPlayer();
        if (existingLocalPlayer != null)
        {
            hasSpawned = true;
            Debug.Log("Local multiplayer player already exists in this scene");
            return;
        }

        Vector3 spawnPos = GetSpawnPositionForPlayer(PhotonNetwork.LocalPlayer);
        string prefabName = ResolveMultiplayerPrefabName();
        Debug.Log(
            $"Spawning multiplayer player '{prefabName}' for actor {PhotonNetwork.LocalPlayer.ActorNumber} " +
            $"(isMaster={PhotonNetwork.IsMasterClient}, playerCount={PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}) at {spawnPos}");
        GameObject spawnedPlayer = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
        hasSpawned = spawnedPlayer != null;
    }

    private void SpawnSinglePlayer()
    {
        if (hasSpawned)
        {
            return;
        }

        Transform existingPlayer = FindExistingSinglePlayer();
        if (existingPlayer != null)
        {
            Debug.Log("Persistent single-player character found -> reusing existing player");
            RespawnPlayer(existingPlayer);
            EnsurePlayerTag(existingPlayer.gameObject);
            hasSpawned = true;
            return;
        }

        Vector3 spawnPos = transform.position;
        GameObject prefabToSpawn = ResolveSinglePlayerPrefab();
        if (prefabToSpawn == null)
        {
            Debug.LogError("Failed to resolve a single-player prefab to spawn.");
            return;
        }

        GameObject player = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        EnsurePlayerTag(player);
        hasSpawned = true;
    }

    private string ResolveMultiplayerPrefabName()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
        {
            if (PhotonNetwork.IsMasterClient && CanLoadPrefab(playerOnePrefabName))
            {
                return playerOnePrefabName;
            }

            if (!PhotonNetwork.IsMasterClient && CanLoadPrefab(playerTwoPrefabName))
            {
                return playerTwoPrefabName;
            }
        }

        Player[] players = PhotonNetwork.PlayerList;
        Array.Sort(players, ComparePlayersByActorNumber);

        int localIndex = Array.FindIndex(
            players,
            player => player != null && player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber
        );

        if (localIndex == 1 && CanLoadPrefab(playerTwoPrefabName))
        {
            return playerTwoPrefabName;
        }

        if (CanLoadPrefab(playerOnePrefabName))
        {
            return playerOnePrefabName;
        }

        if (playerPrefab != null)
        {
            return playerPrefab.name;
        }

        throw new InvalidOperationException("No multiplayer player prefab could be resolved.");
    }

    private GameObject ResolveSinglePlayerPrefab()
    {
        GameObject prefabFromResources = LoadPrefab(playerOnePrefabName);
        if (prefabFromResources != null)
        {
            return prefabFromResources;
        }

        return playerPrefab;
    }

    private bool CanLoadPrefab(string prefabName)
    {
        return LoadPrefab(prefabName) != null;
    }

    private GameObject LoadPrefab(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            return null;
        }

        return Resources.Load<GameObject>(prefabName);
    }

    private static int ComparePlayersByActorNumber(Player left, Player right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.ActorNumber.CompareTo(right.ActorNumber);
    }

    public void RespawnPlayer(Transform player)
    {
        if (player == null)
        {
            return;
        }

        if (player.parent != null)
        {
            player.SetParent(null, true);
        }

        if (!player.gameObject.activeSelf)
        {
            player.gameObject.SetActive(true);
        }

        player.position = GetRespawnPosition(player);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.ResetForRespawn();
            movement.EnableMovement(true);
            movement.SuspendCameraClamp(RespawnCameraClampGraceDuration);
        }

        PlayerWeaponBinder weaponBinder = player.GetComponent<PlayerWeaponBinder>();
        if (weaponBinder != null)
        {
            weaponBinder.RefreshBinding();
        }

        PlayerWeapon weapon = player.GetComponent<PlayerWeapon>();
        if (weapon != null)
        {
            weapon.ResetAttackState();
        }
    }

    private Transform FindExistingSinglePlayer()
    {
        PlayerHealth[] players = FindObjectsOfType<PlayerHealth>(true);

        foreach (PlayerHealth player in players)
        {
            if (player == null)
            {
                continue;
            }

            if (!player.gameObject.scene.IsValid())
            {
                continue;
            }

            return player.transform;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        return taggedPlayer != null ? taggedPlayer.transform : null;
    }

    private static void EnsurePlayerTag(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        if (!player.CompareTag("Player"))
        {
            player.tag = "Player";
        }
    }

    private static void IgnorePlayerSelfCollision()
    {
        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        if (playerLayer < 0)
        {
            Debug.LogWarning($"Layer '{PlayerLayerName}' was not found, so player self-collision was not disabled.");
            return;
        }

        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);
    }

    private PhotonView FindLocalMultiplayerPlayer()
    {
        PhotonView[] views = FindObjectsOfType<PhotonView>(true);

        foreach (PhotonView view in views)
        {
            if (IsLocalMultiplayerPlayer(view))
            {
                return view;
            }
        }

        return null;
    }

    private void CleanupStaleSinglePlayerPlayers()
    {
        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject taggedPlayer in taggedPlayers)
        {
            if (taggedPlayer == null)
            {
                continue;
            }

            PhotonView view = taggedPlayer.GetComponent<PhotonView>();
            if (view == null)
            {
                view = taggedPlayer.GetComponentInParent<PhotonView>();
            }

            if (IsRuntimeMultiplayerPlayer(view))
            {
                continue;
            }

            Debug.Log($"Removing stale offline player before multiplayer spawn: {taggedPlayer.name}");
            Destroy(taggedPlayer);
        }
    }

    private static bool IsLocalMultiplayerPlayer(PhotonView view)
    {
        return view != null
            && view.IsMine
            && view.gameObject.CompareTag("Player")
            && IsRuntimeMultiplayerPlayer(view);
    }

    private static bool IsRuntimeMultiplayerPlayer(PhotonView view)
    {
        // Scene PhotonViews from Bootstrap also have a non-zero ViewID, but they are not
        // PhotonNetwork.Instantiate runtime players. Only treat instantiated room objects
        // as valid multiplayer player instances.
        return view != null && view.InstantiationId != 0;
    }

    private Vector3 GetRespawnPosition(Transform player)
    {
        if (!PhotonNetwork.InRoom || player == null)
            return transform.position;

        PhotonView view = player.GetComponent<PhotonView>();
        if (view == null)
            view = player.GetComponentInParent<PhotonView>();

        Player owner = view != null ? view.Owner : PhotonNetwork.LocalPlayer;
        return GetSpawnPositionForPlayer(owner);
    }

    private bool TryAlignPlayerToSpawn(Transform player, bool forceRespawn = false)
    {
        if (player == null)
        {
            return false;
        }

        Vector3 targetPosition = GetRespawnPosition(player);
        if (!forceRespawn && Vector3.Distance(player.position, targetPosition) <= SpawnAlignmentTolerance)
        {
            return false;
        }

        RespawnPlayer(player);
        return true;
    }

    public Vector3 GetSpawnPositionForPlayer(Player player)
    {
        return GetSpawnPositionForPlayer(player, transform.position);
    }

    public Vector3 GetSpawnPositionForPlayer(Player player, Vector3 basePosition)
    {
        if (!PhotonNetwork.InRoom || player == null)
            return basePosition;

        Player[] players = PhotonNetwork.PlayerList;
        Array.Sort(players, ComparePlayersByActorNumber);

        int playerIndex = Array.FindIndex(
            players,
            candidate => candidate != null && candidate.ActorNumber == player.ActorNumber);

        if (playerIndex <= 0)
            return basePosition;

        Vector2 direction = multiplayerSpawnDirection.sqrMagnitude > 0.0001f
            ? multiplayerSpawnDirection.normalized
            : Vector2.right;
        float spacing = multiplayerSpawnSpacing > 0f ? multiplayerSpawnSpacing : 1.5f;

        return basePosition + (Vector3)(direction * (playerIndex * spacing));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.4f);
    }
}

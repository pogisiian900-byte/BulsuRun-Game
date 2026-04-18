using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentPlayerSpawner : MonoBehaviour
{
    [SerializeField] private string spawnTag = "SpawnPoint";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the spawn point in the NEW scene
        GameObject spawnObj = GameObject.FindGameObjectWithTag(spawnTag);
        if (spawnObj == null)
        {
            Debug.LogWarning($"No spawn point with tag '{spawnTag}' in scene {scene.name}");
            return;
        }

        // Stop physics movement before teleport (optional but recommended)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Teleport player to that scene's spawn
        transform.position = ResolveSpawnPosition(spawnObj.transform.position);
    }

    private Vector3 ResolveSpawnPosition(Vector3 basePosition)
    {
        if (!PhotonNetwork.InRoom)
        {
            return basePosition;
        }

        PhotonView view = GetComponent<PhotonView>();
        if (view == null)
        {
            view = GetComponentInParent<PhotonView>();
        }

        Player owner = view != null ? view.Owner : PhotonNetwork.LocalPlayer;

        SpawnPlayer spawnPlayer = SpawnPlayer.Instance != null
            ? SpawnPlayer.Instance
            : FindFirstObjectByType<SpawnPlayer>();

        if (spawnPlayer != null)
        {
            return spawnPlayer.GetSpawnPositionForPlayer(owner, basePosition);
        }

        return GetFallbackMultiplayerSpawnPosition(owner, basePosition);
    }

    private static Vector3 GetFallbackMultiplayerSpawnPosition(Player owner, Vector3 basePosition)
    {
        if (!PhotonNetwork.InRoom || owner == null)
        {
            return basePosition;
        }

        Player[] players = PhotonNetwork.PlayerList;
        Array.Sort(players, ComparePlayersByActorNumber);

        int playerIndex = Array.FindIndex(
            players,
            candidate => candidate != null && candidate.ActorNumber == owner.ActorNumber);

        if (playerIndex <= 0)
        {
            return basePosition;
        }

        return basePosition + (Vector3)(Vector2.right * (playerIndex * 1.5f));
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
}

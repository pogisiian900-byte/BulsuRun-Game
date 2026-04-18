using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NextLevelDoor : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private string nextSceneName;

    [Header("Mode")]
    [SerializeField] private bool autoEnter = false; // 👈 NEW

    [Header("Optional Info Card")]
    [SerializeField] private GameObject infoCard;

    [Header("UI")]
    [SerializeField] private GameObject doorButtonObject;

    private Button doorButton;
    private bool playerInRange;
    private Transform player;

    private void Awake()
    {
        if (infoCard != null)
            infoCard.SetActive(false);

        if (doorButtonObject != null)
        {
            doorButtonObject.SetActive(false);
            doorButton = doorButtonObject.GetComponent<Button>();

            if (doorButton != null)
                doorButton.onClick.AddListener(EnterRoom);
        }
    }

    private void EnterRoom()
    {
        if (!playerInRange) return;

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        // Stop player movement
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        if (infoCard != null)
            infoCard.SetActive(false);

        if (doorButtonObject != null)
            doorButtonObject.SetActive(false);

        SinglePlayerSaveSystem.SaveCheckpoint();

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(nextSceneName);
                return;
            }

            Transform localPlayer = player != null ? player : LocalPlayerUtility.FindLocalPlayerTransform();
            PhotonView playerView = localPlayer != null ? localPlayer.GetComponent<PhotonView>() : null;
            if (playerView == null && localPlayer != null)
            {
                playerView = localPlayer.GetComponentInParent<PhotonView>();
            }

            if (playerView != null)
            {
                playerView.RPC(nameof(PlayerHealth.RequestLoadLevelRpc), RpcTarget.MasterClient, nextSceneName);
            }
            else
            {
                Debug.LogWarning($"NextLevelDoor could not find a local PhotonView to request scene load for '{nextSceneName}'.");
            }

            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out Transform localPlayer)) return;

        playerInRange = true;
        player = localPlayer;

        // 🔥 AUTO MODE
        if (autoEnter)
        {
            LoadNextScene();
            return;
        }

        // NORMAL MODE
        if (infoCard != null)
            infoCard.SetActive(true);

        if (doorButtonObject != null)
            doorButtonObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out Transform localPlayer) || localPlayer != player) return;

        playerInRange = false;
        player = null;

        if (infoCard != null)
            infoCard.SetActive(false);

        if (doorButtonObject != null)
            doorButtonObject.SetActive(false);
    }
}

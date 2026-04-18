using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    public static PhotonManager Instance;

    private void Awake()
    {
        ConfigurePhotonRuntimeDispatch();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        EnsureConnection();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"Connected to Master Server ({PhotonConnectionSettings.DescribeConnectionBucket()})");
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log($"Joined Lobby ({PhotonConnectionSettings.DescribeConnectionBucket()})");
    }

    public override void OnJoinedRoom()
    {
        SinglePlayerSaveSystem.BeginTemporaryMultiplayerSession();
    }

    public override void OnLeftRoom()
    {
        SinglePlayerSaveSystem.EndTemporaryMultiplayerSession();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SinglePlayerSaveSystem.EndTemporaryMultiplayerSession();
        Debug.Log("Disconnected from Photon: " + cause);
    }

    public void EnsureConnection()
    {
        ConfigurePhotonRuntimeDispatch();
        PhotonConnectionSettings.ApplyRuntimeSettings();
        PlayerNameStore.ApplySavedName();
        PhotonNetwork.AutomaticallySyncScene = true;

        if (PhotonNetwork.InRoom)
        {
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (!PhotonNetwork.InLobby)
            {
                Debug.Log($"Photon connected and ready -> joining lobby ({PhotonConnectionSettings.DescribeConnectionBucket()})");
                PhotonNetwork.JoinLobby();
            }

            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            Debug.Log($"Photon is still connected but not ready yet: {PhotonNetwork.NetworkClientState} ({PhotonConnectionSettings.DescribeConnectionBucket()})");
            return;
        }

        Debug.Log($"Connecting to Photon ({PhotonConnectionSettings.DescribeConnectionBucket()})");
        PhotonNetwork.ConnectUsingSettings();
    }

    private static void ConfigurePhotonRuntimeDispatch()
    {
        // Allow Photon to dispatch incoming room events in LateUpdate when gameplay is paused
        // with Time.timeScale = 0 (for shared dialogue, pause menu, etc.).
        PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 0f;
    }
}

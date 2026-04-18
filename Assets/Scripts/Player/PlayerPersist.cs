using Photon.Pun;
using UnityEngine;

public class PlayerPersist : MonoBehaviourPunCallbacks
{
    private static PlayerPersist instance;

    void Awake()
    {
        // If multiplayer is active, skip persistence.
        if (PhotonNetwork.InRoom)
        {
            return;
        }

        // Single player logic
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnJoinedRoom()
    {
        // Keep real Photon-instantiated room players, but destroy the bootstrap single-player
        // character even if it has a scene PhotonView assigned by Unity.
        if (photonView != null && photonView.InstantiationId != 0)
        {
            return;
        }

        if (instance == this)
        {
            instance = null;
        }

        Debug.Log("Destroying stale single-player player after joining a multiplayer room.");
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

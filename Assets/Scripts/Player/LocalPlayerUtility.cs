using Photon.Pun;
using UnityEngine;

public static class LocalPlayerUtility
{
    public static bool TryGetLocalPlayerTransform(out Transform localPlayer)
    {
        localPlayer = FindLocalPlayerTransform();
        return localPlayer != null;
    }

    public static bool TryGetLocalPlayerTransform(Collider2D other, out Transform localPlayer)
    {
        localPlayer = ResolvePlayerTransform(other != null ? other.transform : null);
        if (localPlayer == null)
        {
            return false;
        }

        if (!PhotonNetwork.InRoom)
            return true;

        PhotonView playerView = FindOwningPhotonView(other.transform);
        if (playerView != null && playerView.IsMine)
            return true;

        localPlayer = null;
        return false;
    }

    public static Transform FindLocalPlayerTransform()
    {
        if (!PhotonNetwork.InRoom)
        {
            GameObject localOfflinePlayer = GameObject.FindGameObjectWithTag("Player");
            return localOfflinePlayer != null ? localOfflinePlayer.transform : null;
        }

        PhotonView[] views = Object.FindObjectsByType<PhotonView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PhotonView view in views)
        {
            if (view == null || !view.IsMine)
                continue;

            Transform localPlayer = ResolvePlayerTransform(view.transform);
            if (localPlayer != null)
                return localPlayer;
        }

        return null;
    }

    private static Transform ResolvePlayerTransform(Transform source)
    {
        Transform current = source;
        while (current != null)
        {
            if (current.CompareTag("Player") || current.GetComponent<PlayerMovement>() != null)
                return current;

            current = current.parent;
        }

        if (source == null)
            return null;

        Transform root = source.root;
        if (root != null && (root.CompareTag("Player") || root.GetComponent<PlayerMovement>() != null))
            return root;

        return null;
    }

    private static PhotonView FindOwningPhotonView(Transform source)
    {
        Transform current = source;
        while (current != null)
        {
            PhotonView view = current.GetComponent<PhotonView>();
            if (view != null)
                return view;

            current = current.parent;
        }

        return source != null ? source.root.GetComponentInChildren<PhotonView>() : null;
    }
}

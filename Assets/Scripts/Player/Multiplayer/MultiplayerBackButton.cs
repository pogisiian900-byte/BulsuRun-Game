using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiplayerBackButton : MonoBehaviourPunCallbacks
{
    private bool isLeaving = false;

    [SerializeField] private string startSceneName = "Start Screen";
    [SerializeField] private Button backButton;

    void Start()
    {
        backButton.onClick.AddListener(OnBackPressed);
    }

    public void OnBackPressed()
    {
        if (isLeaving) return;

        isLeaving = true;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            DisconnectFromPhoton();
        }
    }

    public override void OnLeftRoom()
    {
        DisconnectFromPhoton();
    }

    void DisconnectFromPhoton()
    {
        PhotonNetwork.AutomaticallySyncScene = false;

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
        else
        {
            LoadStartScene();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        LoadStartScene();
    }

    void LoadStartScene()
    {
        isLeaving = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }
}
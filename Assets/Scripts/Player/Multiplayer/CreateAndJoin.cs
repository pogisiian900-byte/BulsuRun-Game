using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreateAndJoin : MonoBehaviourPunCallbacks
{
    private const int MaxRoomNameLength = 24;

    [SerializeField] private TMP_InputField roomInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text popupMessageText;
    [SerializeField] private Button popupCloseButton;

    private bool isJoining;

    private void Awake()
    {
        CacheUiReferences();
        HidePopup();
        RegisterPopupCloseButton();
    }

    private void Start()
    {
        EnsurePhotonConnection();
    }

    private void OnDestroy()
    {
        UnregisterPopupCloseButton();
    }

    public void CreateRoom()
    {
        if (isJoining || PhotonNetwork.InRoom)
        {
            return;
        }

        if (!TryGetValidatedInputs(out string roomName))
        {
            return;
        }

        roomInput.SetTextWithoutNotify(roomName);
        isJoining = true;
        StartCoroutine(CreateRoomWhenReady(roomName));
    }

    private IEnumerator CreateRoomWhenReady(string roomName)
    {
        EnsurePhotonConnection();
        Debug.Log("Waiting to create room...");

        while (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            yield return null;
        }

        Debug.Log($"Creating room '{roomName}' ({PhotonConnectionSettings.DescribeConnectionBucket()})");
        PhotonNetwork.CreateRoom(roomName, new Photon.Realtime.RoomOptions { MaxPlayers = 2 });
    }

    public void JoinRoom()
    {
        if (isJoining || PhotonNetwork.InRoom)
        {
            return;
        }

        if (!TryGetValidatedInputs(out string roomName))
        {
            return;
        }

        roomInput.SetTextWithoutNotify(roomName);
        isJoining = true;
        StartCoroutine(JoinRoomWhenReady(roomName));
    }

    private IEnumerator JoinRoomWhenReady(string roomName)
    {
        EnsurePhotonConnection();
        Debug.Log("Waiting for connection...");

        while (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby)
        {
            yield return null;
        }

        Debug.Log($"Joining room '{roomName}' ({PhotonConnectionSettings.DescribeConnectionBucket()})");
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        isJoining = false;
        HidePopup();

        Debug.Log("Joined Room!");

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("I am HOST (Master Client)");
            PhotonNetwork.LoadLevel("Lobby");
        }
        else
        {
            Debug.Log("I am CLIENT");
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        Debug.LogError($"Join failed ({returnCode}): {message} ({PhotonConnectionSettings.DescribeConnectionBucket()})");
        ShowPopup(BuildJoinRoomFailedMessage(message));
        FocusInput(playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text) ? playerNameInput : roomInput);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        Debug.LogError($"Create failed ({returnCode}): {message} ({PhotonConnectionSettings.DescribeConnectionBucket()})");
        ShowPopup(BuildCreateRoomFailedMessage(message));
        FocusInput(roomInput);
    }

    public override void OnConnectedToMaster()
    {
        if (PhotonManager.Instance == null && !PhotonNetwork.InLobby)
        {
            Debug.Log("CreateAndJoin connected to Master -> joining lobby");
            PhotonNetwork.JoinLobby();
        }
    }

    private void EnsurePhotonConnection()
    {
        PhotonConnectionSettings.ApplyRuntimeSettings();

        if (PhotonManager.Instance != null)
        {
            PhotonManager.Instance.EnsureConnection();
            return;
        }

        PlayerNameStore.ApplySavedName();

        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log($"No PhotonManager found -> connecting from CreateAndJoin ({PhotonConnectionSettings.DescribeConnectionBucket()})");
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            Debug.Log("Photon connected but not in lobby -> joining lobby");
            PhotonNetwork.JoinLobby();
        }
    }

    private bool TryGetValidatedInputs(out string roomName)
    {
        CacheUiReferences();

        roomName = SanitizeRoomName(roomInput != null ? roomInput.text : string.Empty);
        string playerName = playerNameInput != null ? playerNameInput.text.Trim() : PlayerNameStore.GetSavedName();

        if (string.IsNullOrWhiteSpace(playerName))
        {
            ShowPopup("Please enter your name first.");
            FocusInput(playerNameInput);
            return false;
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            ShowPopup("Please enter a room name first.");
            FocusInput(roomInput);
            return false;
        }

        PlayerNameStore.SaveName(playerName);

        if (playerNameInput != null)
        {
            playerNameInput.SetTextWithoutNotify(playerName);
        }

        return true;
    }

    private void ShowPopup(string message)
    {
        CacheUiReferences();

        if (popupMessageText != null)
        {
            popupMessageText.text = message;
        }
        else
        {
            Debug.LogWarning(message);
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }
    }

    public void HidePopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void RegisterPopupCloseButton()
    {
        if (popupCloseButton != null)
        {
            popupCloseButton.onClick.RemoveListener(HidePopup);
            popupCloseButton.onClick.AddListener(HidePopup);
        }
    }

    private void UnregisterPopupCloseButton()
    {
        if (popupCloseButton != null)
        {
            popupCloseButton.onClick.RemoveListener(HidePopup);
        }
    }

    private void CacheUiReferences()
    {
        if (roomInput == null)
        {
            roomInput = FindComponentInScene<TMP_InputField>("Host Field");
        }

        if (playerNameInput == null)
        {
            playerNameInput = FindComponentInScene<TMP_InputField>("Name Field");
        }

        if (popupRoot == null)
        {
            Transform popupTransform = FindTransformInScene("Pop ups");
            if (popupTransform != null)
            {
                popupRoot = popupTransform.gameObject;
            }
        }

        if (popupCloseButton == null)
        {
            popupCloseButton = FindComponentInScene<Button>("Close Button");
            RegisterPopupCloseButton();
        }

        if (popupMessageText == null && popupRoot != null)
        {
            TMP_Text[] popupTexts = popupRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text candidate in popupTexts)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (popupCloseButton != null && candidate.transform.IsChildOf(popupCloseButton.transform))
                {
                    continue;
                }

                popupMessageText = candidate;
                break;
            }
        }
    }

    private void FocusInput(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.ActivateInputField();
        inputField.Select();
    }

    private string BuildJoinRoomFailedMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            return "Room not found. Make sure the host already created it and both devices are using the same build.";
        }

        return "Unable to join that room right now. Please try again.";
    }

    private string BuildCreateRoomFailedMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            return "That room name is already in use. Please enter a different room name.";
        }

        return "Unable to create the room right now. Please try again.";
    }

    private static string SanitizeRoomName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim().ToUpperInvariant();
        return trimmed.Length <= MaxRoomNameLength ? trimmed : trimmed.Substring(0, MaxRoomNameLength);
    }

    private static T FindComponentInScene<T>(string objectName) where T : Component
    {
        Transform transform = FindTransformInScene(objectName);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static Transform FindTransformInScene(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects)
        {
            Transform match = FindTransformRecursive(rootObject.transform, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Transform FindTransformRecursive(Transform current, string objectName)
    {
        if (current == null)
        {
            return null;
        }

        if (string.Equals(current.name, objectName, System.StringComparison.Ordinal) ||
            string.Equals(current.name.Trim(), objectName, System.StringComparison.Ordinal))
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform match = FindTransformRecursive(current.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}

using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartGameMultiplayer : MonoBehaviourPunCallbacks
{
    private const string AutoStartTimePropertyKey = "AutoStartTime";
    private const double AutoStartCountdownSeconds = 3d;

    [SerializeField] private Button startButton;

    private TMP_Text countdownText;
    private bool isLoadingGame;

    private void Awake()
    {
        EnsureCountdownText();
        ApplyButtonState();
    }

    private void OnEnable()
    {
        ApplyButtonState();
        RefreshCountdownState();
    }

    private void Start()
    {
        ApplyButtonState();
        TryScheduleAutoStart();
        RefreshCountdownState();
    }

    private void Update()
    {
        SyncAutoStartState();
        RefreshCountdownState();

        if (isLoadingGame || !PhotonNetwork.InRoom)
        {
            return;
        }

        if (!TryGetScheduledStartTime(out double scheduledStartTime))
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            return;
        }

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.Time >= scheduledStartTime)
        {
            StartGame();
        }
    }

    private void SyncAutoStartState()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || isLoadingGame)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            if (!TryGetScheduledStartTime(out _))
            {
                Debug.Log("Both lobby players are present but no countdown is scheduled yet. Scheduling auto-start now.");
                TryScheduleAutoStart();
            }

            return;
        }

        if (TryGetScheduledStartTime(out _))
        {
            CancelAutoStartIfNeeded();
        }
    }

    public override void OnJoinedRoom()
    {
        isLoadingGame = false;
        ApplyButtonState();
        TryScheduleAutoStart();
        RefreshCountdownState();
    }

    public override void OnLeftRoom()
    {
        isLoadingGame = false;
        ApplyButtonState();
        RefreshCountdownState();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TryScheduleAutoStart();
        RefreshCountdownState();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CancelAutoStartIfNeeded();
        RefreshCountdownState();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        isLoadingGame = false;
        TryScheduleAutoStart();
        RefreshCountdownState();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        RefreshCountdownState();
    }

    public void StartGame()
    {
        if (isLoadingGame || !PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            CancelAutoStartIfNeeded();
            return;
        }

        isLoadingGame = true;
        PhotonNetwork.LoadLevel("Worlds");
    }

    private void TryScheduleAutoStart()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            return;
        }

        if (TryGetScheduledStartTime(out _))
        {
            return;
        }

        Hashtable customProperties = new Hashtable
        {
            { AutoStartTimePropertyKey, PhotonNetwork.Time + AutoStartCountdownSeconds }
        };

        Debug.Log($"Scheduling multiplayer auto-start. PlayerCount={PhotonNetwork.CurrentRoom.PlayerCount}, ActorNumber={PhotonNetwork.LocalPlayer?.ActorNumber}");
        PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);
    }

    private void CancelAutoStartIfNeeded()
    {
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            TryScheduleAutoStart();
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(AutoStartTimePropertyKey))
        {
            return;
        }

        Hashtable customProperties = new Hashtable
        {
            { AutoStartTimePropertyKey, 0d }
        };

        Debug.Log("Cancelling multiplayer auto-start because the room no longer has two players.");
        PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);
    }

    private void RefreshCountdownState()
    {
        ApplyButtonState();
        EnsureCountdownText();

        if (countdownText == null)
        {
            return;
        }

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            countdownText.text = string.Empty;
            return;
        }

        if (TryGetScheduledStartTime(out double scheduledStartTime) && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            double remainingTime = Mathf.Max(0f, (float)(scheduledStartTime - PhotonNetwork.Time));
            countdownText.text = remainingTime > 0d
                ? "STARTING IN " + Mathf.CeilToInt((float)remainingTime)
                : "STARTING...";
            return;
        }

        countdownText.text = PhotonNetwork.CurrentRoom.PlayerCount >= 2
            ? "PREPARING MATCH..."
            : "WAITING FOR PLAYER 2";
    }

    private void ApplyButtonState()
    {
        if (startButton == null)
        {
            return;
        }

        startButton.gameObject.SetActive(PhotonNetwork.InRoom);
        startButton.interactable = false;

        if (startButton.targetGraphic != null)
        {
            startButton.targetGraphic.raycastTarget = false;
        }
    }

    private void EnsureCountdownText()
    {
        if (startButton == null)
        {
            return;
        }

        if (countdownText == null)
        {
            countdownText = startButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (countdownText != null)
        {
            countdownText.raycastTarget = false;
            return;
        }

        RectTransform buttonTransform = startButton.transform as RectTransform;
        if (buttonTransform == null)
        {
            return;
        }

        GameObject textObject = new GameObject("Auto Start Countdown", typeof(RectTransform));
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.SetParent(buttonTransform, false);
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(24f, 24f);
        textTransform.offsetMax = new Vector2(-24f, -24f);

        TextMeshProUGUI createdText = textObject.AddComponent<TextMeshProUGUI>();
        createdText.alignment = TextAlignmentOptions.Center;
        createdText.enableWordWrapping = true;
        createdText.fontSize = 40f;
        createdText.fontStyle = FontStyles.Bold;
        createdText.color = Color.white;
        createdText.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
        {
            createdText.font = TMP_Settings.defaultFontAsset;
        }

        countdownText = createdText;
    }

    private bool TryGetScheduledStartTime(out double scheduledStartTime)
    {
        scheduledStartTime = 0d;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return false;
        }

        Hashtable customProperties = PhotonNetwork.CurrentRoom.CustomProperties;
        if (customProperties == null || !customProperties.TryGetValue(AutoStartTimePropertyKey, out object value) || value == null)
        {
            return false;
        }

        switch (value)
        {
            case double doubleValue when doubleValue > 0d:
                scheduledStartTime = doubleValue;
                return true;
            case float floatValue when floatValue > 0f:
                scheduledStartTime = floatValue;
                return true;
            case int intValue when intValue > 0:
                scheduledStartTime = intValue;
                return true;
            default:
                return false;
        }
    }
}

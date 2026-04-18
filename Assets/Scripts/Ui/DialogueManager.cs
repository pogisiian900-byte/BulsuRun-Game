using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class DialogueManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private static readonly bool DebugLoggingEnabled = false;
    private const byte RequestSharedDialogueEventCode = 71;
    private const byte StartSharedDialogueEventCode = 72;
    private const byte ShowSharedDialogueNodeEventCode = 73;
    private const byte EndSharedDialogueEventCode = 74;
    private const byte RequestSharedDialogueInputEventCode = 75;
    private const int SharedDialogueInputAdvance = 0;
    private const int SharedDialogueInputChoice = 1;
    private const float SceneBindRetryDuration = 2f;
    private const float SceneBindRetryInterval = 0.1f;
    private const string SharedDialoguePlayerPropertyKey = "SharedDialogueSource";
    private const string TriggerSourcePrefix = "trigger:";
    private const string NpcSourcePrefix = "npc:";
    private const string RescueSourcePrefix = "rescue:";
    private const string AdminSourcePrefix = "admin:";

    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI dialogueName;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button dialoguePanelButton;

    [Header("Choices UI")]
    [SerializeField] private GameObject choicesContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Player")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerInput playerInput;

    private Coroutine quickCo;
    private Coroutine sharedStartCo;
    private Coroutine deferredDialogueStartCo;
    private Coroutine deferredSharedStartCo;
    private DialogueAsset currentDialogue;
    private int currentNodeIndex;
    private bool dialogueActive;
    private bool hostControlledDialogue;
    private bool sharedDialoguePending;
    private bool sharedDialogueSession;
    private bool quickMessagePaused;
    private int activeDialogueControllerActorNumber = -1;
    private string activeSharedSourceId = string.Empty;
    private DialoguePanelClickProbe dialoguePanelClickProbe;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Start()
    {
        BindSceneObjects();
        SetupClickToAdvance();
        SafeHideUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneObjects();

        if (dialoguePanel != null)
        {
            SetupClickToAdvance();

            if (dialogueActive && currentDialogue != null)
                ShowNode(currentNodeIndex);
            else
                dialoguePanel.SetActive(false);
        }

        SetLocalPlayerDialogueLock(dialogueActive);
        ApplyPauseState();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (dialogueActive && hostControlledDialogue)
            ShowNode(currentNodeIndex);

        ApplyPauseState();
    }

    public override void OnJoinedRoom()
    {
        ApplyPauseState();
    }

    public override void OnLeftRoom()
    {
        StopDeferredStartCoroutines();
        sharedDialogueSession = false;
        sharedDialoguePending = false;
        activeDialogueControllerActorNumber = -1;
        activeSharedSourceId = string.Empty;
        quickMessagePaused = false;
        SetLocalPlayerDialogueLock(false);
        ApplyPauseState();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ApplyPauseState();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ApplyPauseState();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        if (changedProps != null && changedProps.ContainsKey(SharedDialoguePlayerPropertyKey))
            ApplyPauseState();
    }

    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case RequestSharedDialogueEventCode:
                HandleSharedDialogueRequest(photonEvent.CustomData);
                break;

            case StartSharedDialogueEventCode:
                HandleSharedDialogueStart(photonEvent.CustomData);
                break;

            case ShowSharedDialogueNodeEventCode:
                HandleSharedDialogueNode(photonEvent.CustomData);
                break;

            case EndSharedDialogueEventCode:
                HandleSharedDialogueEnd();
                break;

            case RequestSharedDialogueInputEventCode:
                HandleSharedDialogueInputRequest(photonEvent);
                break;
        }
    }

    private void BindSceneObjects()
    {
        Transform localPlayerTransform = LocalPlayerUtility.FindLocalPlayerTransform();
        player = localPlayerTransform != null
            ? localPlayerTransform.GetComponent<PlayerMovement>()
            : FindFirstObjectByType<PlayerMovement>();
        playerInput = localPlayerTransform != null
            ? localPlayerTransform.GetComponent<PlayerInput>()
            : FindFirstObjectByType<PlayerInput>();

        DialogueUIRefs refs = FindBestDialogueUiRefs();

        if (refs == null)
        {
            LogDebug("BindSceneObjects failed: no DialogueUIRefs found in active scene.");
            ClearSceneUiBindings();
            return;
        }

        dialoguePanel = refs.dialoguePanel;
        dialogueText = refs.dialogueText;
        dialogueName = refs.dialogueName;
        portraitImage = refs.portraitImage;
        choicesContainer = refs.choicesContainer;
        choiceButtonPrefab = refs.choiceButtonPrefab;
        dialoguePanelButton = refs.dialoguePanelButton;

        LogDebug(
            $"Bound scene objects. localPlayer='{DescribeObject(localPlayerTransform)}', " +
            $"player='{DescribeObject(player)}', playerInput='{DescribeObject(playerInput)}', " +
            $"uiRefs='{DescribeObject(refs)}', panel='{DescribeObject(dialoguePanel)}', " +
            $"button='{DescribeObject(dialoguePanelButton)}'.");
        SetupClickToAdvance();
    }

    private void SetupClickToAdvance()
    {
        if (dialoguePanelButton == null)
        {
            LogDebug("SetupClickToAdvance skipped: dialoguePanelButton is null.");
            return;
        }

        dialoguePanelButton.onClick.RemoveListener(OnDialogClicked);
        dialoguePanelButton.onClick.AddListener(OnDialogClicked);
        EnsurePanelClickProbe();
        LogDebug(
            $"Click-to-advance bound to '{dialoguePanelButton.name}'. " +
            $"interactable={dialoguePanelButton.interactable}, enabled={dialoguePanelButton.enabled}, " +
            $"activeInHierarchy={dialoguePanelButton.gameObject.activeInHierarchy}.");
        LogPanelInteractionState("SetupClickToAdvance");
    }

    private void SetPanelClickable(bool clickable)
    {
        if (dialoguePanelButton == null)
        {
            LogDebug($"SetPanelClickable({clickable}) skipped: dialoguePanelButton is null.");
            return;
        }

        dialoguePanelButton.interactable = clickable;
        dialoguePanelButton.enabled = clickable;
        LogDebug(
            $"SetPanelClickable({clickable}) on '{dialoguePanelButton.name}'. " +
            $"controller={activeDialogueControllerActorNumber}, local={GetLocalActorNumber()}.");
        LogPanelInteractionState($"SetPanelClickable({clickable})");
    }

    private bool CanLocalPlayerControlActiveDialogue()
    {
        if (sharedDialogueSession && PhotonNetwork.InRoom)
            return true;

        if (!hostControlledDialogue || !PhotonNetwork.InRoom)
            return true;

        if (activeDialogueControllerActorNumber > 0 && PhotonNetwork.LocalPlayer != null)
            return PhotonNetwork.LocalPlayer.ActorNumber == activeDialogueControllerActorNumber;

        return PhotonNetwork.IsMasterClient;
    }

    private void OnDialogClicked()
    {
        if (!dialogueActive || currentDialogue == null)
        {
            LogDebug("Panel click ignored: dialogue is not active or currentDialogue is null.");
            return;
        }

        if (!CanLocalPlayerControlActiveDialogue())
        {
            LogDebug(
                $"Panel click rejected: local actor {GetLocalActorNumber()} is not controller {activeDialogueControllerActorNumber}. " +
                $"hostControlled={hostControlledDialogue}, shared={sharedDialogueSession}.");
            LogPanelInteractionState("OnDialogClicked rejected");
            return;
        }

        DialogueNode node = currentDialogue.nodes[currentNodeIndex];
        if (node.choices != null && node.choices.Count > 0)
        {
            LogDebug($"Panel click ignored: node {currentNodeIndex} has choices and requires a choice button.");
            LogPanelInteractionState("OnDialogClicked ignored for choices");
            return;
        }

        LogDebug($"Panel click accepted by actor {GetLocalActorNumber()} at node {currentNodeIndex}.");
        LogPanelInteractionState("OnDialogClicked accepted");
        Advance();
    }

    private void Update()
    {
        if (!dialogueActive)
            return;

        if (currentDialogue == null)
        {
            EndDialogueLocal();
            return;
        }

        if (!CanLocalPlayerControlActiveDialogue())
            return;

        if (Input.GetMouseButtonDown(0))
            LogDialoguePointerAttempt("MouseDown", Input.mousePosition, -1);

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
                LogDialoguePointerAttempt("TouchBegan", touch.position, touch.fingerId);
        }

        DialogueNode node = currentDialogue.nodes[currentNodeIndex];
        if (node.choices != null && node.choices.Count > 0)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
            Advance();
    }

    public void RequestSharedDialogue(DialogueTrigger trigger)
    {
        if (trigger == null || trigger.GetDialogue() == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            BeginTriggeredDialogue(trigger, false, true);
            return;
        }

        if (dialogueActive || sharedDialoguePending)
            return;

        string sourceId = BuildTriggerSourceId(trigger.GetTriggerId());
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        int controllerActorNumber = PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : -1;

        if (PhotonNetwork.IsMasterClient)
        {
            ScheduleSharedDialogueStart(sourceId, controllerActorNumber);
            return;
        }

        object[] eventData = { sourceId, controllerActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(RequestSharedDialogueEventCode, eventData, options, SendOptions.SendReliable);
    }

    public void RequestSharedNpcDialogue(NPC npc)
    {
        if (npc == null || npc.GetDialogue() == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            StartDialogueInternal(npc.GetDialogue(), false);
            return;
        }

        if (dialogueActive || sharedDialoguePending)
            return;

        string sourceId = BuildNpcSourceId(npc.GetNpcId());
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        int controllerActorNumber = PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : -1;

        LogDebug(
            $"RequestSharedNpcDialogue from actor {controllerActorNumber}. " +
            $"npc='{npc.name}', sourceId='{sourceId}', isMaster={PhotonNetwork.IsMasterClient}.");

        if (PhotonNetwork.IsMasterClient)
        {
            ScheduleSharedDialogueStart(sourceId, controllerActorNumber);
            return;
        }

        object[] eventData = { sourceId, controllerActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(RequestSharedDialogueEventCode, eventData, options, SendOptions.SendReliable);
    }

    public void RequestSharedRescueDialogue(RescueNPC rescueNpc)
    {
        if (rescueNpc == null || rescueNpc.GetDialogue() == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            StartDialogueInternal(rescueNpc.GetDialogue(), false);
            return;
        }

        if (dialogueActive || sharedDialoguePending)
            return;

        string sourceId = BuildRescueSourceId(rescueNpc.GetRescueNpcId());
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        int controllerActorNumber = GetLocalActorNumber();

        LogDebug(
            $"RequestSharedRescueDialogue from actor {controllerActorNumber}. " +
            $"npc='{rescueNpc.name}', sourceId='{sourceId}', isMaster={PhotonNetwork.IsMasterClient}.");

        if (PhotonNetwork.IsMasterClient)
        {
            ScheduleSharedDialogueStart(sourceId, controllerActorNumber);
            return;
        }

        object[] eventData = { sourceId, controllerActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(RequestSharedDialogueEventCode, eventData, options, SendOptions.SendReliable);
    }

    public void RequestSharedAdminDialogue(AdminLevelCompletionChecker checker)
    {
        if (checker == null || checker.GetIncompleteRescueDialogue() == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            StartDialogueInternal(checker.GetIncompleteRescueDialogue(), false);
            return;
        }

        if (dialogueActive || sharedDialoguePending)
            return;

        string sourceId = BuildAdminSourceId(checker.GetDialogueSourceId());
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        int controllerActorNumber = GetLocalActorNumber();

        LogDebug(
            $"RequestSharedAdminDialogue from actor {controllerActorNumber}. " +
            $"checker='{checker.name}', sourceId='{sourceId}', isMaster={PhotonNetwork.IsMasterClient}.");

        if (PhotonNetwork.IsMasterClient)
        {
            ScheduleSharedDialogueStart(sourceId, controllerActorNumber);
            return;
        }

        object[] eventData = { sourceId, controllerActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        PhotonNetwork.RaiseEvent(RequestSharedDialogueEventCode, eventData, options, SendOptions.SendReliable);
    }

    public void StartDialogue(DialogueAsset dialogue)
    {
        StartDialogueInternal(dialogue, false);
    }

    private void StartDialogueInternal(DialogueAsset dialogue, bool hostControlled)
    {
        StartDialogueInternal(dialogue, hostControlled, false);
    }

    private void StartDialogueInternal(DialogueAsset dialogue, bool hostControlled, bool isSharedDialogue)
    {
        if (dialogue == null || dialogue.nodes == null || dialogue.nodes.Count == 0)
        {
            sharedDialoguePending = false;
            return;
        }

        if (dialoguePanel == null || dialogueText == null)
            BindSceneObjects();

        if (dialoguePanel == null || dialogueText == null)
        {
            QueueDeferredDialogueStart(dialogue, hostControlled, isSharedDialogue);
            return;
        }

        if (deferredDialogueStartCo != null)
        {
            StopCoroutine(deferredDialogueStartCo);
            deferredDialogueStartCo = null;
        }

        if (quickCo != null)
        {
            StopCoroutine(quickCo);
            quickCo = null;
        }

        currentDialogue = dialogue;
        currentNodeIndex = 0;
        dialogueActive = true;
        hostControlledDialogue = (hostControlled || isSharedDialogue) && PhotonNetwork.InRoom;
        sharedDialogueSession = isSharedDialogue && PhotonNetwork.InRoom;
        sharedDialoguePending = false;

        if (!sharedDialogueSession)
            activeDialogueControllerActorNumber = -1;

        LogDebug(
            $"StartDialogueInternal speaker='{dialogue.speakerName}', shared={sharedDialogueSession}, " +
            $"hostControlled={hostControlledDialogue}, controller={activeDialogueControllerActorNumber}, " +
            $"local={GetLocalActorNumber()}, panel='{DescribeObject(dialoguePanel)}', button='{DescribeObject(dialoguePanelButton)}'.");
        dialoguePanel.SetActive(true);

        if (dialogueName != null)
            dialogueName.text = dialogue.speakerName;

        if (portraitImage != null)
        {
            portraitImage.sprite = dialogue.portrait;
            portraitImage.enabled = dialogue.portrait != null;
        }

        SetLocalPlayerDialogueLock(true);

        if (sharedDialogueSession)
            SetLocalSharedDialogueState(activeSharedSourceId);

        ApplyPauseState();

        ShowNode(currentNodeIndex);
    }

    public bool IsDialogueActive()
    {
        return dialogueActive || sharedDialoguePending;
    }

    private void ShowNode(int nodeIndex)
    {
        ClearChoices();

        if (currentDialogue == null || nodeIndex < 0 || nodeIndex >= currentDialogue.nodes.Count)
        {
            EndDialogueLocal();
            return;
        }

        currentNodeIndex = nodeIndex;
        DialogueNode node = currentDialogue.nodes[currentNodeIndex];

        if (dialogueText != null)
            dialogueText.text = node.text;

        bool hasChoices = node.choices != null && node.choices.Count > 0;
        bool canControl = CanLocalPlayerControlActiveDialogue();

        LogDebug(
            $"ShowNode index={nodeIndex}, hasChoices={hasChoices}, canControl={canControl}, " +
            $"controller={activeDialogueControllerActorNumber}, local={GetLocalActorNumber()}, " +
            $"button='{DescribeObject(dialoguePanelButton)}'.");
        LogPanelInteractionState($"ShowNode({nodeIndex})");

        if (hasChoices)
        {
            SetPanelClickable(false);

            if (choicesContainer != null)
                choicesContainer.SetActive(true);

            if (choicesContainer != null && choiceButtonPrefab != null)
            {
                foreach (DialogueChoice choice in node.choices)
                {
                    Button button = Instantiate(choiceButtonPrefab, choicesContainer.transform);
                    TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                        label.text = choice.choiceText;

                    int next = choice.nextNodeIndex;
                    button.interactable = canControl;
                    button.enabled = canControl;
                    button.onClick.AddListener(() => OnChoicePicked(next));
                }
            }
        }
        else
        {
            SetPanelClickable(canControl);

            if (choicesContainer != null)
                choicesContainer.SetActive(false);
        }
    }

    private void Advance()
    {
        if (currentDialogue == null)
            return;

        DialogueNode node = currentDialogue.nodes[currentNodeIndex];
        int next = node.nextNodeIndex;

        if (sharedDialogueSession && PhotonNetwork.InRoom)
        {
            RequestSharedDialogueAdvance();
            return;
        }

        if (hostControlledDialogue && PhotonNetwork.InRoom)
        {
            if (!CanLocalPlayerControlActiveDialogue())
                return;

            if (next == -1)
                BroadcastSharedDialogueEnd();
            else
                BroadcastSharedDialogueNode(next);

            return;
        }

        if (next == -1)
            EndDialogueLocal();
        else
            ShowNode(next);
    }

    private void OnChoicePicked(int nextNodeIndex)
    {
        if (sharedDialogueSession && PhotonNetwork.InRoom)
        {
            RequestSharedDialogueChoice(nextNodeIndex);
            return;
        }

        if (hostControlledDialogue && PhotonNetwork.InRoom)
        {
            if (!CanLocalPlayerControlActiveDialogue())
                return;

            if (nextNodeIndex == -1)
                BroadcastSharedDialogueEnd();
            else
                BroadcastSharedDialogueNode(nextNodeIndex);

            return;
        }

        if (nextNodeIndex == -1)
            EndDialogueLocal();
        else
            ShowNode(nextNodeIndex);
    }

    private void ClearChoices()
    {
        if (choicesContainer == null)
            return;

        for (int i = choicesContainer.transform.childCount - 1; i >= 0; i--)
            Destroy(choicesContainer.transform.GetChild(i).gameObject);

        choicesContainer.SetActive(false);
    }

    private void EndDialogueLocal()
    {
        StopDeferredStartCoroutines();

        if (sharedStartCo != null)
        {
            StopCoroutine(sharedStartCo);
            sharedStartCo = null;
        }

        bool wasSharedDialogue = sharedDialogueSession;
        dialogueActive = false;
        hostControlledDialogue = false;
        sharedDialoguePending = false;
        sharedDialogueSession = false;
        activeDialogueControllerActorNumber = -1;
        activeSharedSourceId = string.Empty;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        ClearChoices();

        SetLocalPlayerDialogueLock(false);

        if (wasSharedDialogue)
            SetLocalSharedDialogueState(string.Empty);

        ApplyPauseState();
        currentDialogue = null;
    }

    private void SafeHideUI()
    {
        StopDeferredStartCoroutines();

        if (sharedStartCo != null)
        {
            StopCoroutine(sharedStartCo);
            sharedStartCo = null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        ClearChoices();

        if (choicesContainer != null)
            choicesContainer.SetActive(false);

        dialogueActive = false;
        hostControlledDialogue = false;
        sharedDialoguePending = false;
        sharedDialogueSession = false;
        quickMessagePaused = false;
        activeDialogueControllerActorNumber = -1;
        activeSharedSourceId = string.Empty;
        currentDialogue = null;

        SetLocalSharedDialogueState(string.Empty);
        ApplyPauseState();

        SetLocalPlayerDialogueLock(false);
    }

    public void ShowQuickMessage(
        string speaker,
        Sprite portrait,
        string message,
        float seconds = 1.2f,
        bool pauseGame = false)
    {
        if (dialoguePanel == null || dialogueText == null)
            BindSceneObjects();

        if (dialoguePanel == null || dialogueText == null)
        {
            // Debug.LogWarning("Dialogue UI not found.");
            return;
        }

        if (quickCo != null)
            StopCoroutine(quickCo);

        quickCo = StartCoroutine(QuickMessageRoutine(speaker, portrait, message, seconds, pauseGame));
    }

    private IEnumerator QuickMessageRoutine(
        string speaker,
        Sprite portrait,
        string message,
        float seconds,
        bool pauseGame)
    {
        dialogueActive = false;
        hostControlledDialogue = false;
        currentDialogue = null;
        activeDialogueControllerActorNumber = -1;

        dialoguePanel.SetActive(true);
        ClearChoices();

        if (dialogueName != null)
            dialogueName.text = speaker;

        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        dialogueText.text = message;
        SetPanelClickable(false);

        SetLocalPlayerDialogueLock(true);

        quickMessagePaused = pauseGame;
        ApplyPauseState();

        yield return new WaitForSecondsRealtime(seconds);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        ClearChoices();

        SetLocalPlayerDialogueLock(false);

        quickMessagePaused = false;
        ApplyPauseState();
        SetPanelClickable(true);

        quickCo = null;
    }

    private void HandleSharedDialogueRequest(object customData)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (dialogueActive || sharedDialoguePending)
            return;

        string sourceId = ExtractString(customData);
        if (string.IsNullOrWhiteSpace(sourceId))
            return;

        int controllerActorNumber = ExtractInt(customData, 1, PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1);
        LogDebug($"Master received shared dialogue request. sourceId='{sourceId}', controller={controllerActorNumber}.");
        ScheduleSharedDialogueStart(sourceId, controllerActorNumber);
    }

    private void HandleSharedDialogueInputRequest(EventData photonEvent)
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
        {
            LogDebug(
                $"Ignoring shared dialogue input event because this client cannot process it. " +
                $"inRoom={PhotonNetwork.InRoom}, isMaster={PhotonNetwork.IsMasterClient}, sender={photonEvent.Sender}.");
            return;
        }

        int requestType = ExtractInt(photonEvent.CustomData, 0, SharedDialogueInputAdvance);
        int originNodeIndex = ExtractInt(photonEvent.CustomData, 1, -1);
        int requestedNextNodeIndex = ExtractInt(photonEvent.CustomData, 2, -1);
        LogDebug(
            $"Master received shared dialogue input event. sender={photonEvent.Sender}, type={requestType}, " +
            $"origin={originNodeIndex}, requestedNext={requestedNextNodeIndex}, dialogueActive={dialogueActive}, " +
            $"shared={sharedDialogueSession}, currentNode={currentNodeIndex}, source='{activeSharedSourceId}'.");
        ProcessSharedDialogueInputRequest(requestType, originNodeIndex, requestedNextNodeIndex, photonEvent.Sender);
    }

    private void ScheduleSharedDialogueStart(string sourceId, int controllerActorNumber)
    {
        if (!PhotonNetwork.InRoom || string.IsNullOrWhiteSpace(sourceId))
            return;

        if (!TryResolveSharedDialogueSource(sourceId, out DialogueAsset _, out float delay))
        {
            sharedDialoguePending = false;
            return;
        }

        activeSharedSourceId = sourceId;
        sharedDialoguePending = true;

        if (sharedStartCo != null)
            StopCoroutine(sharedStartCo);

        if (delay > 0f)
        {
            sharedStartCo = StartCoroutine(BroadcastSharedDialogueStartAfterDelay(sourceId, delay, controllerActorNumber));
            return;
        }

        BroadcastSharedDialogueStart(sourceId, controllerActorNumber);
    }

    private void BroadcastSharedDialogueStart(string sourceId, int controllerActorNumber)
    {
        if (!PhotonNetwork.InRoom || string.IsNullOrWhiteSpace(sourceId))
            return;

        sharedStartCo = null;
        sharedDialoguePending = true;

        object[] eventData = { sourceId, controllerActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(StartSharedDialogueEventCode, eventData, options, SendOptions.SendReliable);
    }

    private IEnumerator BroadcastSharedDialogueStartAfterDelay(string sourceId, float delay, int controllerActorNumber)
    {
        yield return new WaitForSecondsRealtime(delay);
        BroadcastSharedDialogueStart(sourceId, controllerActorNumber);
    }

    private void HandleSharedDialogueStart(object customData)
    {
        string sourceId = ExtractString(customData);
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            sharedDialoguePending = false;
            return;
        }

        activeSharedSourceId = sourceId;
        sharedDialoguePending = true;
        activeDialogueControllerActorNumber = ExtractInt(customData, 1, -1);

        LogDebug(
            $"HandleSharedDialogueStart sourceId='{sourceId}', controller={activeDialogueControllerActorNumber}, " +
            $"local={GetLocalActorNumber()}, scene='{SceneManager.GetActiveScene().name}'.");

        if (deferredSharedStartCo != null)
            StopCoroutine(deferredSharedStartCo);

        deferredSharedStartCo = StartCoroutine(DeferredSharedDialogueStartRoutine(sourceId, activeDialogueControllerActorNumber));
    }

    private void BeginTriggeredDialogue(DialogueTrigger trigger, bool hostControlled, bool applyDelay)
    {
        if (trigger == null)
        {
            sharedDialoguePending = false;
            return;
        }

        DialogueAsset dialogue = trigger.GetDialogue();
        float delay = Mathf.Max(0f, trigger.GetDelayBeforeDialogue());

        trigger.ConsumeTrigger();

        if (sharedStartCo != null)
            StopCoroutine(sharedStartCo);

        sharedDialoguePending = true;

        if (applyDelay && delay > 0f)
            sharedStartCo = StartCoroutine(BeginTriggeredDialogueAfterDelay(dialogue, delay, hostControlled));
        else
            StartDialogueInternal(dialogue, hostControlled, false);
    }

    private IEnumerator BeginTriggeredDialogueAfterDelay(DialogueAsset dialogue, float delay, bool hostControlled)
    {
        yield return new WaitForSecondsRealtime(delay);
        sharedStartCo = null;
        StartDialogueInternal(dialogue, hostControlled, false);
    }

    private void RequestSharedDialogueAdvance()
    {
        RequestSharedDialogueInput(SharedDialogueInputAdvance, -1);
    }

    private void RequestSharedDialogueChoice(int nextNodeIndex)
    {
        RequestSharedDialogueInput(SharedDialogueInputChoice, nextNodeIndex);
    }

    private void RequestSharedDialogueInput(int requestType, int requestedNextNodeIndex)
    {
        if (!PhotonNetwork.InRoom || !sharedDialogueSession || currentDialogue == null)
            return;

        int originNodeIndex = currentNodeIndex;
        if (PhotonNetwork.IsMasterClient)
        {
            LogDebug(
                $"Processing shared dialogue input locally as master. type={requestType}, " +
                $"origin={originNodeIndex}, requestedNext={requestedNextNodeIndex}, actor={GetLocalActorNumber()}.");
            ProcessSharedDialogueInputRequest(requestType, originNodeIndex, requestedNextNodeIndex, GetLocalActorNumber());
            return;
        }

        LogDebug(
            $"Sending shared dialogue input request. type={requestType}, origin={originNodeIndex}, " +
            $"requestedNext={requestedNextNodeIndex}, actor={GetLocalActorNumber()}.");
        object[] eventData = { requestType, originNodeIndex, requestedNextNodeIndex };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
        bool raiseSucceeded = PhotonNetwork.RaiseEvent(RequestSharedDialogueInputEventCode, eventData, options, SendOptions.SendReliable);
        LogDebug(
            $"Shared dialogue input request send result={raiseSucceeded}. " +
            $"type={requestType}, origin={originNodeIndex}, requestedNext={requestedNextNodeIndex}, actor={GetLocalActorNumber()}.");
    }

    private void ProcessSharedDialogueInputRequest(int requestType, int originNodeIndex, int requestedNextNodeIndex, int actorNumber)
    {
        if (!dialogueActive || !sharedDialogueSession || currentDialogue == null)
        {
            LogDebug(
                $"Rejecting shared dialogue input from actor {actorNumber} because dialogue state is not ready. " +
                $"dialogueActive={dialogueActive}, shared={sharedDialogueSession}, currentDialogueNull={currentDialogue == null}, " +
                $"origin={originNodeIndex}, requestedNext={requestedNextNodeIndex}, currentNode={currentNodeIndex}.");
            return;
        }

        if (originNodeIndex != currentNodeIndex)
        {
            LogDebug(
                $"Ignoring stale shared dialogue input from actor {actorNumber}. " +
                $"origin={originNodeIndex}, current={currentNodeIndex}, requestedNext={requestedNextNodeIndex}.");
            return;
        }

        DialogueNode node = currentDialogue.nodes[currentNodeIndex];
        switch (requestType)
        {
            case SharedDialogueInputAdvance:
                if (node.choices != null && node.choices.Count > 0)
                {
                    LogDebug($"Rejecting advance request from actor {actorNumber} at node {currentNodeIndex} because the node requires a choice.");
                    return;
                }

                ApplySharedDialogueTransition(node.nextNodeIndex, actorNumber);
                return;

            case SharedDialogueInputChoice:
                if (!NodeContainsChoice(node, requestedNextNodeIndex))
                {
                    LogDebug(
                        $"Rejecting choice request from actor {actorNumber} at node {currentNodeIndex}. " +
                        $"requestedNext={requestedNextNodeIndex} is not a valid choice.");
                    return;
                }

                ApplySharedDialogueTransition(requestedNextNodeIndex, actorNumber);
                return;
        }

        LogDebug($"Ignoring unknown shared dialogue input type {requestType} from actor {actorNumber}.");
    }

    private void ApplySharedDialogueTransition(int nextNodeIndex, int actorNumber)
    {
        LogDebug(
            $"Accepting shared dialogue input from actor {actorNumber}. " +
            $"current={currentNodeIndex}, next={nextNodeIndex}, sharedSource='{activeSharedSourceId}'.");

        if (nextNodeIndex == -1)
        {
            ForceCloseSharedDialogue(actorNumber);
            return;
        }

        if (currentDialogue == null || nextNodeIndex < 0 || nextNodeIndex >= currentDialogue.nodes.Count)
        {
            LogDebug($"Rejecting shared dialogue transition because next node {nextNodeIndex} is out of range.");
            return;
        }

        ShowNode(nextNodeIndex);
        BroadcastSharedDialogueNode(nextNodeIndex);
    }

    private void ForceCloseSharedDialogue(int actorNumber)
    {
        if (!PhotonNetwork.InRoom)
        {
            LogDebug(
                $"ForceCloseSharedDialogue fallback to local close because not in room. " +
                $"actor={actorNumber}, currentNode={currentNodeIndex}, source='{activeSharedSourceId}'.");
            EndDialogueLocal();
            return;
        }

        object[] eventData = { activeSharedSourceId, actorNumber, currentNodeIndex };
        RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        bool raiseSucceeded = PhotonNetwork.RaiseEvent(EndSharedDialogueEventCode, eventData, options, SendOptions.SendReliable);
        LogDebug(
            $"ForceCloseSharedDialogue send result={raiseSucceeded}. actor={actorNumber}, " +
            $"currentNode={currentNodeIndex}, source='{activeSharedSourceId}', local={GetLocalActorNumber()}.");

        if (!raiseSucceeded)
        {
            LogDebug("ForceCloseSharedDialogue event send failed, closing locally as fallback.");
            EndDialogueLocal();
        }
    }

    private void BroadcastSharedDialogueNode(int nodeIndex)
    {
        if (!PhotonNetwork.InRoom)
            return;

        object[] eventData = { nodeIndex };
        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = PhotonNetwork.IsMasterClient ? ReceiverGroup.Others : ReceiverGroup.All
        };
        bool raiseSucceeded = PhotonNetwork.RaiseEvent(ShowSharedDialogueNodeEventCode, eventData, options, SendOptions.SendReliable);
        LogDebug(
            $"BroadcastSharedDialogueNode({nodeIndex}) send result={raiseSucceeded}. " +
            $"isMaster={PhotonNetwork.IsMasterClient}, local={GetLocalActorNumber()}.");
    }

    private void HandleSharedDialogueNode(object customData)
    {
        int nextNodeIndex = ExtractInt(customData, -1);
        LogDebug(
            $"HandleSharedDialogueNode next={nextNodeIndex}, local={GetLocalActorNumber()}, " +
            $"dialogueActive={dialogueActive}, shared={sharedDialogueSession}, currentNode={currentNodeIndex}.");
        if (nextNodeIndex < 0)
        {
            EndDialogueLocal();
            return;
        }

        ShowNode(nextNodeIndex);
    }

    private void BroadcastSharedDialogueEnd()
    {
        if (!PhotonNetwork.InRoom)
            return;

        RaiseEventOptions options = new RaiseEventOptions
        {
            Receivers = PhotonNetwork.IsMasterClient ? ReceiverGroup.Others : ReceiverGroup.All
        };
        bool raiseSucceeded = PhotonNetwork.RaiseEvent(EndSharedDialogueEventCode, null, options, SendOptions.SendReliable);
        LogDebug(
            $"BroadcastSharedDialogueEnd send result={raiseSucceeded}. " +
            $"isMaster={PhotonNetwork.IsMasterClient}, local={GetLocalActorNumber()}.");
    }

    private void HandleSharedDialogueEnd()
    {
        LogDebug(
            $"HandleSharedDialogueEnd local={GetLocalActorNumber()}, dialogueActive={dialogueActive}, " +
            $"shared={sharedDialogueSession}, currentNode={currentNodeIndex}.");
        EndDialogueLocal();
    }

    private bool TryResolveSharedDialogueSource(string sourceId, out DialogueAsset dialogue, out float delay)
    {
        dialogue = null;
        delay = 0f;

        if (TryGetTriggerFromSourceId(sourceId, out DialogueTrigger trigger))
        {
            dialogue = trigger.GetDialogue();
            delay = Mathf.Max(0f, trigger.GetDelayBeforeDialogue());
            return dialogue != null;
        }

        if (TryGetNpcFromSourceId(sourceId, out NPC npc))
        {
            dialogue = npc.GetDialogue();
            delay = 0f;
            return dialogue != null;
        }

        if (TryGetRescueNpcFromSourceId(sourceId, out RescueNPC rescueNpc))
        {
            dialogue = rescueNpc.GetDialogue();
            delay = 0f;
            return dialogue != null;
        }

        if (TryGetAdminCheckerFromSourceId(sourceId, out AdminLevelCompletionChecker checker))
        {
            dialogue = checker.GetIncompleteRescueDialogue();
            delay = 0f;
            return dialogue != null;
        }

        return false;
    }

    private static bool NodeContainsChoice(DialogueNode node, int nextNodeIndex)
    {
        if (node == null || node.choices == null)
            return false;

        foreach (DialogueChoice choice in node.choices)
        {
            if (choice != null && choice.nextNodeIndex == nextNodeIndex)
                return true;
        }

        return false;
    }

    private void SetLocalSharedDialogueState(string sourceId)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        string sanitizedSourceId = string.IsNullOrWhiteSpace(sourceId) ? string.Empty : sourceId;
        object existingValue = null;

        if (PhotonNetwork.LocalPlayer.CustomProperties != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(SharedDialoguePlayerPropertyKey))
        {
            existingValue = PhotonNetwork.LocalPlayer.CustomProperties[SharedDialoguePlayerPropertyKey];
        }

        if (string.Equals(existingValue?.ToString() ?? string.Empty, sanitizedSourceId))
            return;

        PhotonHashtable properties = new PhotonHashtable
        {
            { SharedDialoguePlayerPropertyKey, sanitizedSourceId }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }

    private bool IsAnyPlayerInSharedDialogue()
    {
        if (!PhotonNetwork.InRoom)
            return false;

        Player[] players = PhotonNetwork.PlayerList;
        if (players == null)
            return false;

        foreach (Player roomPlayer in players)
        {
            if (roomPlayer?.CustomProperties == null ||
                !roomPlayer.CustomProperties.ContainsKey(SharedDialoguePlayerPropertyKey))
            {
                continue;
            }

            object value = roomPlayer.CustomProperties[SharedDialoguePlayerPropertyKey];
            if (!string.IsNullOrWhiteSpace(value?.ToString()))
                return true;
        }

        return false;
    }

    private void ApplyPauseState()
    {
        bool shouldPause =
            quickMessagePaused ||
            (dialogueActive && !sharedDialogueSession) ||
            (PhotonNetwork.InRoom && (sharedDialogueSession || IsAnyPlayerInSharedDialogue()));

        Time.timeScale = shouldPause ? 0f : 1f;
    }

    private void SetLocalPlayerDialogueLock(bool shouldLock)
    {
        if (player == null || playerInput == null)
            BindSceneObjects();

        if (player != null)
            player.EnableMovement(!shouldLock, shouldLock);

        if (playerInput != null)
            playerInput.SetMovementInputEnabled(!shouldLock);

        LogDebug(
            $"SetLocalPlayerDialogueLock({shouldLock}) local={GetLocalActorNumber()}, " +
            $"player='{DescribeObject(player)}', playerInput='{DescribeObject(playerInput)}'.");
    }

    private void QueueDeferredDialogueStart(DialogueAsset dialogue, bool hostControlled, bool isSharedDialogue)
    {
        if (!gameObject.activeInHierarchy)
        {
            // Debug.LogError("Dialogue UI not found in this scene.");
            sharedDialoguePending = false;
            return;
        }

        sharedDialoguePending = true;

        if (deferredDialogueStartCo != null)
            StopCoroutine(deferredDialogueStartCo);

        deferredDialogueStartCo = StartCoroutine(DeferredDialogueStartRoutine(dialogue, hostControlled, isSharedDialogue));
    }

    private IEnumerator DeferredDialogueStartRoutine(DialogueAsset dialogue, bool hostControlled, bool isSharedDialogue)
    {
        float timeoutAt = Time.unscaledTime + SceneBindRetryDuration;

        while (Time.unscaledTime < timeoutAt)
        {
            BindSceneObjects();
            if (dialoguePanel != null && dialogueText != null)
            {
                deferredDialogueStartCo = null;
                StartDialogueInternal(dialogue, hostControlled, isSharedDialogue);
                yield break;
            }

            yield return new WaitForSecondsRealtime(SceneBindRetryInterval);
        }

        deferredDialogueStartCo = null;
        sharedDialoguePending = false;
        // Debug.LogError($"Dialogue UI not found in scene '{SceneManager.GetActiveScene().name}' after waiting for it to bind.");
    }

    private IEnumerator DeferredSharedDialogueStartRoutine(string sourceId, int controllerActorNumber)
    {
        float timeoutAt = Time.unscaledTime + SceneBindRetryDuration;

        while (Time.unscaledTime < timeoutAt)
        {
            if (TryResolveSharedDialogueSource(sourceId, out DialogueAsset dialogue, out float _))
            {
                deferredSharedStartCo = null;
                activeDialogueControllerActorNumber = controllerActorNumber;

                if (TryGetTriggerFromSourceId(sourceId, out DialogueTrigger trigger))
                    trigger.ConsumeTrigger();

                if (TryGetRescueNpcFromSourceId(sourceId, out RescueNPC rescueNpc))
                    rescueNpc.BeginSharedDialogueSequence();

                StartDialogueInternal(dialogue, false, true);
                yield break;
            }

            yield return new WaitForSecondsRealtime(SceneBindRetryInterval);
        }

        deferredSharedStartCo = null;
        sharedDialoguePending = false;
        // Debug.LogWarning($"DialogueManager could not find shared dialogue source '{sourceId}' in scene '{SceneManager.GetActiveScene().name}'.");
    }

    private void StopDeferredStartCoroutines()
    {
        if (deferredDialogueStartCo != null)
        {
            StopCoroutine(deferredDialogueStartCo);
            deferredDialogueStartCo = null;
        }

        if (deferredSharedStartCo != null)
        {
            StopCoroutine(deferredSharedStartCo);
            deferredSharedStartCo = null;
        }
    }

    private void ClearSceneUiBindings()
    {
        dialoguePanel = null;
        dialogueText = null;
        dialogueName = null;
        portraitImage = null;
        choicesContainer = null;
        choiceButtonPrefab = null;
        dialoguePanelButton = null;
    }

    [System.Diagnostics.Conditional("DIALOGUE_MANAGER_DEBUG_LOGS")]
    private void LogDebug(string message)
    {
        if (!DebugLoggingEnabled)
            return;

        // Debug.Log($"[DialogueDebug] {message}", this);
    }

    private void EnsurePanelClickProbe()
    {
        if (dialoguePanelButton == null)
        {
            dialoguePanelClickProbe = null;
            return;
        }

        if (dialoguePanelClickProbe == null || dialoguePanelClickProbe.gameObject != dialoguePanelButton.gameObject)
            dialoguePanelClickProbe = dialoguePanelButton.GetComponent<DialoguePanelClickProbe>();

        if (dialoguePanelClickProbe == null)
            dialoguePanelClickProbe = dialoguePanelButton.gameObject.AddComponent<DialoguePanelClickProbe>();

        dialoguePanelClickProbe.Initialize(this, dialoguePanelButton);
    }

    [System.Diagnostics.Conditional("DIALOGUE_MANAGER_DEBUG_LOGS")]
    internal void LogDialoguePointerEvent(string eventName, Button sourceButton, PointerEventData eventData)
    {
        if (!DebugLoggingEnabled)
            return;

        string pointerPress = DescribeObject(eventData?.pointerPress);
        string pointerEnter = DescribeObject(eventData?.pointerEnter);
        string raycastObject = DescribeObject(eventData?.pointerCurrentRaycast.gameObject);
        string selectedObject = DescribeObject(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null);

        LogDebug(
            $"{eventName} from '{DescribeObject(sourceButton)}'. " +
            $"pointerId={eventData?.pointerId ?? -999}, position={eventData?.position ?? Vector2.zero}, " +
            $"pointerPress='{pointerPress}', pointerEnter='{pointerEnter}', raycast='{raycastObject}', " +
            $"selected='{selectedObject}', state={DescribeButtonState(sourceButton)}.");
    }

    [System.Diagnostics.Conditional("DIALOGUE_MANAGER_DEBUG_LOGS")]
    private void LogDialoguePointerAttempt(string inputType, Vector2 position, int pointerId)
    {
        if (!DebugLoggingEnabled)
            return;

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        string selectedObject = DescribeObject(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null);

        LogDebug(
            $"Input attempt {inputType}. pointerId={pointerId}, position={position}, pointerOverUi={pointerOverUi}, " +
            $"selected='{selectedObject}', shared={sharedDialogueSession}, controller={activeDialogueControllerActorNumber}, " +
            $"state={DescribeButtonState(dialoguePanelButton)}.");
    }

    [System.Diagnostics.Conditional("DIALOGUE_MANAGER_DEBUG_LOGS")]
    private void LogPanelInteractionState(string context)
    {
        LogDebug($"{context}: {DescribeButtonState(dialoguePanelButton)}.");
    }

    private string DescribeButtonState(Button button)
    {
        if (button == null)
            return "button=null";

        Graphic targetGraphic = button.targetGraphic;
        Canvas parentCanvas = button.GetComponentInParent<Canvas>(true);
        GraphicRaycaster raycaster = parentCanvas != null ? parentCanvas.GetComponent<GraphicRaycaster>() : null;
        CanvasGroup[] canvasGroups = button.GetComponentsInParent<CanvasGroup>(true);

        System.Text.StringBuilder groupBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            CanvasGroup group = canvasGroups[i];
            if (group == null)
                continue;

            if (groupBuilder.Length > 0)
                groupBuilder.Append(" | ");

            groupBuilder.Append(group.name);
            groupBuilder.Append("(alpha=");
            groupBuilder.Append(group.alpha.ToString("0.##"));
            groupBuilder.Append(", interactable=");
            groupBuilder.Append(group.interactable);
            groupBuilder.Append(", blocksRaycasts=");
            groupBuilder.Append(group.blocksRaycasts);
            groupBuilder.Append(")");
        }

        string eventSystemName = DescribeObject(EventSystem.current != null ? EventSystem.current.gameObject : null);
        string selectedObject = DescribeObject(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null);
        string groups = groupBuilder.Length > 0 ? groupBuilder.ToString() : "none";

        return
            $"button='{button.name}', interactable={button.interactable}, enabled={button.enabled}, " +
            $"activeSelf={button.gameObject.activeSelf}, activeInHierarchy={button.gameObject.activeInHierarchy}, " +
            $"targetGraphic='{DescribeObject(targetGraphic)}', targetRaycast={targetGraphic != null && targetGraphic.raycastTarget}, " +
            $"canvas='{DescribeObject(parentCanvas)}', raycaster='{DescribeObject(raycaster)}', " +
            $"eventSystem='{eventSystemName}', selected='{selectedObject}', canvasGroups={groups}";
    }

    private int GetLocalActorNumber()
    {
        return PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
    }

    private static string DescribeObject(Object target)
    {
        return target == null ? "null" : target.name;
    }

    private DialogueUIRefs FindBestDialogueUiRefs()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        DialogueUIRefs[] refsCollection = FindObjectsByType<DialogueUIRefs>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        DialogueUIRefs bestRefs = null;
        int bestScore = int.MinValue;

        foreach (DialogueUIRefs candidate in refsCollection)
        {
            if (candidate == null)
                continue;

            int score = ScoreDialogueUiRefs(candidate, activeScene);
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestRefs = candidate;
        }

        return bestRefs;
    }

    private static int ScoreDialogueUiRefs(DialogueUIRefs candidate, Scene activeScene)
    {
        int score = 0;

        if (candidate.gameObject.scene == activeScene)
            score += 100;

        if (candidate.isActiveAndEnabled)
            score += 25;

        if (candidate.dialoguePanel != null)
            score += 10;

        if (candidate.dialogueText != null)
            score += 10;

        if (candidate.choicesContainer != null)
            score += 5;

        if (candidate.dialoguePanelButton != null)
            score += 5;

        return score;
    }

    private DialogueTrigger FindDialogueTrigger(string triggerId)
    {
        string normalizedTriggerId = NormalizeLookupId(triggerId);
        DialogueTrigger[] triggers = FindObjectsByType<DialogueTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DialogueTrigger trigger in triggers)
        {
            if (trigger == null)
                continue;

            string candidateId = trigger.GetTriggerId();
            if (candidateId == triggerId || NormalizeLookupId(candidateId) == normalizedTriggerId)
                return trigger;
        }

        return null;
    }

    private NPC FindNpc(string npcId)
    {
        string normalizedNpcId = NormalizeLookupId(npcId);
        NPC[] npcs = FindObjectsByType<NPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NPC npc in npcs)
        {
            if (npc == null)
                continue;

            string candidateId = npc.GetNpcId();
            if (candidateId == npcId || NormalizeLookupId(candidateId) == normalizedNpcId)
                return npc;
        }

        return null;
    }

    private RescueNPC FindRescueNpc(string rescueId)
    {
        string normalizedRescueId = NormalizeLookupId(rescueId);
        RescueNPC[] rescueNpcs = FindObjectsByType<RescueNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (RescueNPC rescueNpc in rescueNpcs)
        {
            if (rescueNpc == null)
                continue;

            string candidateId = rescueNpc.GetRescueNpcId();
            if (candidateId == rescueId || NormalizeLookupId(candidateId) == normalizedRescueId)
                return rescueNpc;
        }

        return null;
    }

    private AdminLevelCompletionChecker FindAdminChecker(string checkerId)
    {
        string normalizedCheckerId = NormalizeLookupId(checkerId);
        AdminLevelCompletionChecker[] checkers = FindObjectsByType<AdminLevelCompletionChecker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AdminLevelCompletionChecker checker in checkers)
        {
            if (checker == null)
                continue;

            string candidateId = checker.GetDialogueSourceId();
            if (candidateId == checkerId || NormalizeLookupId(candidateId) == normalizedCheckerId)
                return checker;
        }

        return null;
    }

    private static string ExtractString(object customData)
    {
        if (customData is object[] values && values.Length > 0 && values[0] != null)
            return values[0].ToString();

        return customData != null ? customData.ToString() : string.Empty;
    }

    private static int ExtractInt(object customData, int fallbackValue)
    {
        return ExtractInt(customData, 0, fallbackValue);
    }

    private static int ExtractInt(object customData, int valueIndex, int fallbackValue)
    {
        if (customData is object[] values && values.Length > valueIndex)
            return ConvertToInt(values[valueIndex], fallbackValue);

        return ConvertToInt(customData, fallbackValue);
    }

    private static int ConvertToInt(object value, int fallbackValue)
    {
        if (value is int intValue)
            return intValue;

        return int.TryParse(value?.ToString(), out int parsedValue) ? parsedValue : fallbackValue;
    }

    private static string BuildTriggerSourceId(string triggerId)
    {
        return string.IsNullOrWhiteSpace(triggerId) ? string.Empty : TriggerSourcePrefix + triggerId;
    }

    private static string BuildNpcSourceId(string npcId)
    {
        return string.IsNullOrWhiteSpace(npcId) ? string.Empty : NpcSourcePrefix + npcId;
    }

    private static string BuildRescueSourceId(string rescueId)
    {
        return string.IsNullOrWhiteSpace(rescueId) ? string.Empty : RescueSourcePrefix + rescueId;
    }

    private static string BuildAdminSourceId(string adminId)
    {
        return string.IsNullOrWhiteSpace(adminId) ? string.Empty : AdminSourcePrefix + adminId;
    }

    private bool TryGetTriggerFromSourceId(string sourceId, out DialogueTrigger trigger)
    {
        trigger = null;

        if (string.IsNullOrWhiteSpace(sourceId) || !sourceId.StartsWith(TriggerSourcePrefix))
            return false;

        string triggerId = sourceId.Substring(TriggerSourcePrefix.Length);
        trigger = FindDialogueTrigger(triggerId);
        return trigger != null;
    }

    private bool TryGetNpcFromSourceId(string sourceId, out NPC npc)
    {
        npc = null;

        if (string.IsNullOrWhiteSpace(sourceId) || !sourceId.StartsWith(NpcSourcePrefix))
            return false;

        string npcId = sourceId.Substring(NpcSourcePrefix.Length);
        npc = FindNpc(npcId);
        return npc != null;
    }

    private bool TryGetRescueNpcFromSourceId(string sourceId, out RescueNPC rescueNpc)
    {
        rescueNpc = null;

        if (string.IsNullOrWhiteSpace(sourceId) || !sourceId.StartsWith(RescueSourcePrefix))
            return false;

        string rescueId = sourceId.Substring(RescueSourcePrefix.Length);
        rescueNpc = FindRescueNpc(rescueId);
        return rescueNpc != null;
    }

    private bool TryGetAdminCheckerFromSourceId(string sourceId, out AdminLevelCompletionChecker checker)
    {
        checker = null;

        if (string.IsNullOrWhiteSpace(sourceId) || !sourceId.StartsWith(AdminSourcePrefix))
            return false;

        string adminId = sourceId.Substring(AdminSourcePrefix.Length);
        checker = FindAdminChecker(adminId);
        return checker != null;
    }

    private static string NormalizeLookupId(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(sourceId.Length);
        bool skippingIndex = false;

        for (int i = 0; i < sourceId.Length; i++)
        {
            char current = sourceId[i];

            if (current == '[')
            {
                skippingIndex = true;
                continue;
            }

            if (skippingIndex)
            {
                if (current == ']')
                    skippingIndex = false;

                continue;
            }

            builder.Append(current);
        }

        return builder.ToString();
    }
}

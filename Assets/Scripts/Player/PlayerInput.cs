using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerInput : MonoBehaviourPun
{
    private const float MobileInputRetryInterval = 0.5f;

    private PlayerMovement movement;
    private PlayerInventory inventory;
    private bool movementInputEnabled = true;

    [SerializeField] private MobileInput mobileInput;
    [SerializeField] private bool autoRun;
    [SerializeField] private int autoRunDirection = 1;

    private Coroutine mobileInputSearchRoutine;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        inventory = GetComponent<PlayerInventory>();

        if (ShouldReadLocalInput())
        {
            BeginMobileInputSearch();
        }
    }

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
        if (!ShouldReadLocalInput())
            return;

        mobileInput = null;
        BeginMobileInputSearch();
    }

    private bool ShouldReadLocalInput()
    {
        return !PhotonNetwork.InRoom || photonView.IsMine;
    }

    private void BeginMobileInputSearch()
    {
        if (mobileInputSearchRoutine != null)
        {
            StopCoroutine(mobileInputSearchRoutine);
        }

        mobileInputSearchRoutine = StartCoroutine(FindMobileInput());
    }

    private void Update()
    {
        if (!ShouldReadLocalInput())
            return;

        ReadInput();
    }

    private IEnumerator FindMobileInput()
    {
        while (mobileInput == null)
        {
            mobileInput = FindFirstObjectByType<MobileInput>();
            if (mobileInput == null)
            {
                yield return new WaitForSeconds(MobileInputRetryInterval);
            }
        }

        mobileInput.SetMovementInputEnabled(movementInputEnabled);

        mobileInputSearchRoutine = null;
    }

    public void ReadInput()
    {
        float moveInput = 0f;
        bool jumpDown = false;
        bool jumpHeld = false;
        bool jumpReleased = false;

        if (movementInputEnabled)
        {
            moveInput = autoRun ? autoRunDirection : (mobileInput != null ? mobileInput.MoveX : 0f);

            if (ShouldUseDesktopKeyboard())
            {
                float keyboardMove = Input.GetAxisRaw("Horizontal");
                if (Mathf.Abs(keyboardMove) > 0.1f)
                    moveInput = keyboardMove;
            }

            jumpDown = mobileInput != null && mobileInput.ConsumeJumpPressed();
            jumpHeld = mobileInput != null && mobileInput.JumpHeld();
            jumpReleased = mobileInput != null && mobileInput.JumpReleasedThisFrame;

            if (ShouldUseDesktopKeyboard())
            {
                if (Input.GetKeyDown(KeyCode.Space)) jumpDown = true;
                if (Input.GetKey(KeyCode.Space)) jumpHeld = true;
                if (Input.GetKeyUp(KeyCode.Space)) jumpReleased = true;
            }
        }

        if (movement != null)
            movement.HandleMovement(moveInput, jumpDown, jumpHeld, jumpReleased);

        if (mobileInput != null && inventory != null && mobileInput.ConsumeInventoryPressed())
            inventory.ToggleInventory();
    }

    public void SetAutoRun(bool enable, int dir)
    {
        autoRun = enable;
        autoRunDirection = dir;
    }

    public void SetMovementInputEnabled(bool enabled)
    {
        movementInputEnabled = enabled;

        if (mobileInput != null)
            mobileInput.SetMovementInputEnabled(enabled);

        if (!enabled && movement != null)
            movement.HandleMovement(0f, false, false, false);
    }

    private static bool ShouldUseDesktopKeyboard()
    {
        return !Application.isMobilePlatform;
    }
}

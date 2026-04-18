using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RescueNPC : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueAsset dialogue;

    [Header("Rescue")]
    public Animator animator;
    public int rescueValue = 1;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public Vector2 moveDirection = Vector2.right;
    [SerializeField] private bool spriteFacesLeftByDefault = true;

    [Header("UI")]
    public GameObject rescueButtonUI;

    private bool rescued;
    private bool playerNearby;
    private bool isMoving;
    private bool isStartingRescue;

    private MobileInput mobileInput;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        mobileInput = FindFirstObjectByType<MobileInput>();
    }

    private void Update()
    {
        if (rescued || isStartingRescue || !playerNearby)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
            return;

        if (mobileInput != null && mobileInput.ConsumeRescuePressed())
        {
            StartDialogue();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartDialogue();
        }
    }

    private void FixedUpdate()
    {
        if (!isMoving)
            return;

        Vector2 direction = moveDirection.sqrMagnitude > 0f ? moveDirection.normalized : Vector2.right;

        if (rb != null)
        {
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            transform.position += (Vector3)(direction * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void StartDialogue()
    {
        if (rescued || isStartingRescue)
            return;

        if (dialogue == null || DialogueManager.Instance == null)
        {
            StartRescue();
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            DialogueManager.Instance.RequestSharedRescueDialogue(this);
            return;
        }

        isStartingRescue = true;
        DialogueManager.Instance.StartDialogue(dialogue);
        StartCoroutine(WaitForDialogueEnd());
    }

    public void BeginSharedDialogueSequence()
    {
        if (rescued || isStartingRescue)
            return;

        isStartingRescue = true;
        StartCoroutine(WaitForDialogueEnd());
    }

    private IEnumerator WaitForDialogueEnd()
    {
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
            yield return null;

        StartRescue();
    }

    private void StartRescue()
    {
        if (rescued)
            return;

        isStartingRescue = false;
        rescued = true;
        playerNearby = false;

        if (rescueButtonUI != null)
            rescueButtonUI.SetActive(false);

        Vector2 direction = moveDirection.sqrMagnitude > 0f ? moveDirection.normalized : Vector2.right;

        if (direction.x != 0f)
        {
            Vector3 scale = transform.localScale;
            float scaleX = Mathf.Abs(scale.x);
            bool movingRight = direction.x > 0f;

            scale.x = spriteFacesLeftByDefault
                ? (movingRight ? -scaleX : scaleX)
                : (movingRight ? scaleX : -scaleX);

            transform.localScale = scale;
        }

        if (animator != null)
            animator.SetTrigger("Run");

        if (RescueManager.Instance != null)
            RescueManager.Instance.AddRescue(rescueValue);

        isMoving = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!rescued)
            return;

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0 && collision.gameObject.layer == wallLayer)
            Destroy(gameObject, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (rescued || !LocalPlayerUtility.TryGetLocalPlayerTransform(other, out _))
            return;

        playerNearby = true;

        if (rescueButtonUI != null)
            rescueButtonUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(other, out _))
            return;

        playerNearby = false;

        if (rescueButtonUI != null)
            rescueButtonUI.SetActive(false);
    }

    public DialogueAsset GetDialogue()
    {
        return dialogue;
    }

    public string GetRescueNpcId()
    {
        return gameObject.scene.name + "/" + GetHierarchyPath(transform);
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return string.Empty;

        string path = FormatTransformSegment(target);
        Transform current = target.parent;

        while (current != null)
        {
            path = FormatTransformSegment(current) + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static string FormatTransformSegment(Transform target)
    {
        return target.name + "[" + target.GetSiblingIndex() + "]";
    }
}

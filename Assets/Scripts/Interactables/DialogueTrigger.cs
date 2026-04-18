using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueAsset dialogue;

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Delay Settings")]
    [SerializeField] private float delayBeforeDialogue = 0f; // 👈 set to 3 for 3 sec delay

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);

    private DialogueManager dialogueManager;
    private BoxCollider2D box;
    private bool triggered;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        dialogueManager = FindFirstObjectByType<DialogueManager>();

        if (dialogueManager == null)
            Debug.LogError("DialogueTrigger: No DialogueManager found in scene or DontDestroyOnLoad.");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out _)) return;
        if (triggerOnce && triggered) return;

        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        if (dialogueManager == null) return;

        dialogueManager.RequestSharedDialogue(this);
    }

    public string GetTriggerId()
    {
        return gameObject.scene.name + "/" + GetHierarchyPath(transform);
    }

    public DialogueAsset GetDialogue()
    {
        return dialogue;
    }

    public float GetDelayBeforeDialogue()
    {
        return delayBeforeDialogue;
    }

    public void ConsumeTrigger()
    {
        triggered = true;

        if (triggerOnce)
            Destroy(gameObject);
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

   

    private void OnDrawGizmos()
    {
        if (box == null)
            box = GetComponent<BoxCollider2D>();

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
    }
}

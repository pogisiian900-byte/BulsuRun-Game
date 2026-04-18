using UnityEngine;
using UnityEngine.UI;

public class NPC : MonoBehaviour
{
    [Header("Optional UI")]
    [SerializeField] private GameObject infoCard;

    [Header("Dialogue")]
    [SerializeField] private DialogueAsset dialogue;

    [Header("UI")]
    [SerializeField] private GameObject talkButtonObject; // drag TalkButton here

    private Button talkButton;
    private bool playerInRange;

    private void Start()
    {
        if (infoCard != null)
            infoCard.SetActive(false);

        if (talkButtonObject != null)
        {
            talkButtonObject.SetActive(false);
            talkButton = talkButtonObject.GetComponent<Button>();
            talkButton.onClick.AddListener(StartTalk);
        }
    }

    private void StartTalk()
    {
        if (!playerInRange) return;
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.RequestSharedNpcDialogue(this);

        if (infoCard != null)
            infoCard.SetActive(false);

        talkButtonObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out _)) return;

        playerInRange = true;

        if (infoCard != null)
            infoCard.SetActive(true);

        if (talkButtonObject != null)
            talkButtonObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!LocalPlayerUtility.TryGetLocalPlayerTransform(collision, out _)) return;

        playerInRange = false;

        if (infoCard != null)
            infoCard.SetActive(false);

        if (talkButtonObject != null)
            talkButtonObject.SetActive(false);
    }

    public DialogueAsset GetDialogue()
    {
        return dialogue;
    }

    public string GetNpcId()
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

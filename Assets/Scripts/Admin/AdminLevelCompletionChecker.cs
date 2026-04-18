using UnityEngine;

public class AdminLevelCompletionChecker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RescueManager rescueManager;
    [SerializeField] private DialogueAsset incompleteRescueDialogue;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnPlayerEnter;

    private bool levelCompleted;

    private void Reset()
    {
        rescueManager = FindFirstObjectByType<RescueManager>();
    }

    public void TryCompleteLevel()
    {
        if (levelCompleted)
            return;

        RescueManager activeRescueManager = rescueManager != null ? rescueManager : RescueManager.Instance;
        if (activeRescueManager == null)
        {
            Debug.LogWarning("AdminLevelCompletionChecker could not find a RescueManager.");
            return;
        }

        if (activeRescueManager.RescuedCount == activeRescueManager.RescueGoal)
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogWarning("AdminLevelCompletionChecker could not find a LevelManager.");
                return;
            }

            levelCompleted = true;
            LevelManager.Instance.Win();
            return;
        }

        if (incompleteRescueDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.RequestSharedAdminDialogue(this);
            return;
        }

        if (DialogueManager.Instance != null)
        {
            int remainingRescues = Mathf.Max(0, activeRescueManager.RescueGoal - activeRescueManager.RescuedCount);
            DialogueManager.Instance.ShowQuickMessage(
                "Admin",
                null,
                $"You still need to rescue {remainingRescues} more before finishing this level.",
                2f);
            return;
        }

        Debug.LogWarning("AdminLevelCompletionChecker has no dialogue to play and DialogueManager was not found.");
    }

    public void OnCompleteButtonPressed()
    {
        TryCompleteLevel();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnPlayerEnter || !LocalPlayerUtility.TryGetLocalPlayerTransform(other, out _))
            return;

        TryCompleteLevel();
    }

    public DialogueAsset GetIncompleteRescueDialogue()
    {
        return incompleteRescueDialogue;
    }

    public string GetDialogueSourceId()
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

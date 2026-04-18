using UnityEngine;
using UnityEngine.Video;

public class LevelCompletePortal : MonoBehaviour
{
    [SerializeField] private int nextStageNumber = 2;
    [SerializeField] private string cardSceneName = "Ability Selection";
    [SerializeField] private VideoClip completionCutscene;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var lm = LevelManager.Instance;
        if (lm == null)
        {
            Debug.LogError("LevelManager.Instance is null. Make sure LevelManager exists in a Bootstrap scene or in the first loaded scene.");
            return;
        }

        // ✅ set stage
        RunManager.Instance.SetStage(nextStageNumber);

        // ✅ decide where to go
        string target = RunManager.Instance.ShouldShowCardsForNextStage(nextStageNumber)
            ? cardSceneName
            : GetSceneForStage(nextStageNumber);

        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError($"No scene mapped for stage {nextStageNumber}. Add it to your stage mapping.");
            return;
        }

        lm.SetNextScene(target);

        if (completionCutscene != null && lm.TryPlayVictoryCutscene(true, completionCutscene, target))
            return;

        lm.Win();
    }

    // ✅ Stage -> Scene mapping (same idea as SkillRollUI)
   private string GetSceneForStage(int stage)
{
    switch (stage)
    {
        case 1: return "Level 1 CBA Classroom";
        case 2: return "Level 2 CBA";
        case 3: return "Level 3 CBA";
        case 4: return "CBA Mini Boss";
        case 5: return "AC Level 1";
        case 6: return "AC Level 2";
        case 7: return "AC Mini Boss";
        case 8: return "Level 1 Admin Outside";
        case 9: return "Level 2nd Floor Admin";
        case 10: return "Level 3rd Floor Admin";
        case 11: return "Admin Mini Boss";
        case 12: return "Level 1 Pancho 1st Floor";
        case 13: return "Level 2 Pancho 2nd Floor";
        case 14: return "Pancho Mini Boss";
        case 15: return "Gate Final Boss";
        default: return null;
    }
}
}

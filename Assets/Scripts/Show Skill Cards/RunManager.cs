using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    public int StageIndex { get; private set; } = 1; // Level 1 starts at 1

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        StageIndex = Mathf.Max(1, GameData.RunStageIndex);
    }

    public void SetStage(int stage)
    {
        StageIndex = Mathf.Max(1, stage);
        GameData.RunStageIndex = StageIndex;
    }

    public void ResetToDefault(bool saveToDisk = true)
    {
        StageIndex = 1;
        GameData.RunStageIndex = StageIndex;

        if (saveToDisk)
            SinglePlayerSaveSystem.SaveCheckpoint(false);
    }

    public void LoadFromGameData()
    {
        StageIndex = Mathf.Max(1, GameData.RunStageIndex);
    }

    public bool ShouldShowCardsForNextStage(int nextStage)
    {
        // show on stage 2,4,6...
        return nextStage % 5 == 0;
    }
}

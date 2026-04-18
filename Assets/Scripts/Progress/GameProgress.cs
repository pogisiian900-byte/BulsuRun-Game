using UnityEngine;

public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance;

    public int highestLevelUnlocked = 1;
    public int highestWorldUnlocked = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            highestLevelUnlocked = ProgressStore.GetHighestLevel();
            highestWorldUnlocked = ProgressStore.GetHighestWorld();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UnlockNextLevel()
    {
        highestLevelUnlocked = Mathf.Max(1, ProgressStore.GetHighestLevel() + 1);
        ProgressStore.SetHighestLevel(highestLevelUnlocked);
        ProgressStore.Save();
    }

    public void UnlockNextWorld()
    {
        highestWorldUnlocked = Mathf.Max(1, ProgressStore.GetHighestWorld() + 1);
        ProgressStore.SetHighestWorld(highestWorldUnlocked);
        ProgressStore.Save();
    }
}

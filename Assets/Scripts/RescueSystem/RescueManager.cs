using UnityEngine;

public class RescueManager : MonoBehaviour
{
    public static RescueManager Instance;

    [Header("Rescue Goal")]
    public int rescueGoal = 3;

    [SerializeField] private bool autoCompleteOnGoal = true;

    private int rescuedCount = 0;

    void Awake()
    {
        Instance = this;
    }

    public void AddRescue(int amount)
    {
        rescuedCount += amount;

        RescueUIManager.Instance.UpdateRescueText(rescuedCount, rescueGoal);

        if (autoCompleteOnGoal && rescuedCount >= rescueGoal)
        {
            LevelManager.Instance.Win();
        }
    }

    public int RescueGoal => rescueGoal;

    public int RescuedCount => rescuedCount;

    public bool HasReachedGoal()
    {
        return rescuedCount >= rescueGoal;
    }
}

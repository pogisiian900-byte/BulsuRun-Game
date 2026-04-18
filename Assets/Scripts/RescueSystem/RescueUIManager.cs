using TMPro;
using UnityEngine;

public class RescueUIManager : MonoBehaviour
{
    public static RescueUIManager Instance;

    public TMP_Text rescueText;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateRescueText(int current, int goal)
    {
        rescueText.text = "Rescued: " + current + "/" + goal;
    }
}
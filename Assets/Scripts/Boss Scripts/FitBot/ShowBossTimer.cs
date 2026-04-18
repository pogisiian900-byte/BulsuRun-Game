using UnityEngine;

public class ShowBossTimer : MonoBehaviour
{
    [SerializeField] private GameObject bossTimerUI;

    void Start()
    {
        if (bossTimerUI != null)
            bossTimerUI.SetActive(true);
    }
}
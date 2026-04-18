using UnityEngine;

public class BossBattleUI : MonoBehaviour
{
    [SerializeField] private GameObject bossHealthBar;

    public void StartBossBattle()
    {
        bossHealthBar.SetActive(true);
    }

    public void EndBossBattle()
    {
        bossHealthBar.SetActive(false);
    }
}

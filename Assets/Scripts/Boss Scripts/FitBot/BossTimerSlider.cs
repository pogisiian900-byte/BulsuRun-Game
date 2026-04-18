using UnityEngine;
using UnityEngine.UI;

public class BossTimerPhaseBar : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;

    [Header("Timer")]
    [SerializeField] private float totalTime = 60f;

    [Header("Phase Colors")]
    [SerializeField] private Color phase1Color = Color.green;
    [SerializeField] private Color phase2Color = Color.yellow;
    [SerializeField] private Color phase3Color = Color.red;

    private float timer;

    void Start()
    {
        timer = totalTime;
        slider.maxValue = totalTime;
        slider.value = totalTime;

        fillImage.color = phase1Color;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        slider.value = timer;

        float elapsed = totalTime - timer;

        // Phase 1 (0–15s)
        if (elapsed < 15f)
            fillImage.color = phase1Color;

        // Phase 2 (15–35s)
        else if (elapsed < 35f)
            fillImage.color = phase2Color;

        // Phase 3 (35–60s)
        else
            fillImage.color = phase3Color;
    }
}
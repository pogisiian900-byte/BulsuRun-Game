using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private BossHealth boss;

    [SerializeField] private TrashLordPhases trashLordPhases;
    [SerializeField] private SkyBotPhases skyBotPhases;
    [SerializeField] private BossController bossController;

    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;

    [Header("Phase Colors")]
    [SerializeField] private Color phase1Color = Color.green;
    [SerializeField] private Color phase2Color = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color phase3Color = Color.red;

    private void Awake()
    {
        if (boss == null)
            boss = FindFirstObjectByType<BossHealth>();

        if (trashLordPhases == null)
            trashLordPhases = FindFirstObjectByType<TrashLordPhases>();

        if (skyBotPhases == null)
            skyBotPhases = FindFirstObjectByType<SkyBotPhases>();

        if (bossController == null)
            bossController = FindFirstObjectByType<BossController>();

        if (slider == null)
            slider = GetComponentInChildren<Slider>(true);

        if (fillImage == null && slider != null && slider.fillRect != null)
            fillImage = slider.fillRect.GetComponent<Image>();
    }

    private void Start()
    {
        if (boss == null)
        {
            Debug.LogError("BossHealthBarUI: BossHealth is NOT assigned!");
            return;
        }

        if (slider == null)
        {
            Debug.LogError("BossHealthBarUI: Slider is NOT assigned!");
            return;
        }

        if (fillImage == null)
        {
            Debug.LogError("BossHealthBarUI: Fill Image is NOT assigned!");
            return;
        }

        if (trashLordPhases == null && skyBotPhases == null && bossController == null)
        {
            Debug.LogWarning("BossHealthBarUI: No boss phase controller assigned!");
        }

        slider.maxValue = 1f;
        slider.value = boss.Normalized;

        UpdatePhaseColor();

        boss.OnHealthChanged.AddListener(HandleHealthChanged);
    }

    private void OnDestroy()
    {
        if (boss != null)
            boss.OnHealthChanged.RemoveListener(HandleHealthChanged);
    }

    private void HandleHealthChanged(int cur, int max)
    {
        if (slider == null || boss == null)
            return;

        slider.value = boss.Normalized;
        UpdatePhaseColor();
    }

    private void UpdatePhaseColor()
    {
        if (fillImage == null)
            return;

        if (trashLordPhases != null)
        {
            switch (trashLordPhases.CurrentPhase)
            {
                case TrashLordPhases.Phase.Phase1:
                    fillImage.color = phase1Color;
                    break;

                case TrashLordPhases.Phase.Phase2:
                    fillImage.color = phase2Color;
                    break;

                case TrashLordPhases.Phase.Phase3:
                    fillImage.color = phase3Color;
                    break;
            }
        }
        else if (bossController != null)
        {
            switch (bossController.CurrentPhase)
            {
                case BossController.Phase.Phase1:
                    fillImage.color = phase1Color;
                    break;

                case BossController.Phase.Phase2:
                    fillImage.color = phase2Color;
                    break;

                case BossController.Phase.Phase3:
                    fillImage.color = phase3Color;
                    break;
            }
        }
        else if (skyBotPhases != null)
        {
            switch (skyBotPhases.CurrentPhase)
            {
                case SkyBotPhases.Phase.Phase1:
                    fillImage.color = phase1Color;
                    break;

                case SkyBotPhases.Phase.Phase2:
                    fillImage.color = phase2Color;
                    break;

                case SkyBotPhases.Phase.Phase3:
                    fillImage.color = phase3Color;
                    break;
            }
        }
    }
}

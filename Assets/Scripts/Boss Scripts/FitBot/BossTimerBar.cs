using UnityEngine;
using UnityEngine.UI;

public class BossTimerBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float totalTime = 60f;

    private float timer;
    private bool running;

    public void StartTimer(float duration)
    {
        totalTime = duration;
        timer = duration;
        running = true;
    }

    private void Update()
    {
        if (!running) return;

        timer -= Time.deltaTime;

        float percent = Mathf.Clamp01(timer / totalTime);
        fillImage.fillAmount = percent;

        if (timer <= 0f)
        {
            running = false;
        }
    }
}
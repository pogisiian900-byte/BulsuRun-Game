using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UISlideshow : MonoBehaviour
{
    [Header("UI")]
    public Image targetImage;

    [Header("Slides")]
    public Sprite[] slides;

    [Header("Timing")]
    public float secondsPerSlide = 2f;

    [Header("Loop")]
    public bool loop = true;

    private int index = 0;
    private Coroutine routine;

    void OnEnable()
    {
        if (routine == null)
            routine = StartCoroutine(Play());
    }

    void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    IEnumerator Play()
    {
        if (targetImage == null || slides == null || slides.Length == 0)
            yield break;

        // show first slide immediately
        index = Mathf.Clamp(index, 0, slides.Length - 1);
        targetImage.sprite = slides[index];

        while (true)
        {
            yield return new WaitForSeconds(secondsPerSlide);

            index++;

            if (index >= slides.Length)
            {
                if (loop) index = 0;
                else yield break;
            }

            targetImage.sprite = slides[index];
        }
    }

    // Optional buttons:
    public void Next()
    {
        if (slides == null || slides.Length == 0) return;
        index = (index + 1) % slides.Length;
        targetImage.sprite = slides[index];
    }

    public void Prev()
    {
        if (slides == null || slides.Length == 0) return;
        index = (index - 1 + slides.Length) % slides.Length;
        targetImage.sprite = slides[index];
    }
}

using UnityEngine;
using System.Collections;

public class TrapFloor : MonoBehaviour
{
    [SerializeField] private GameObject fireObject;
    [SerializeField] private Animator animator; // 👈 ADD THIS
    [SerializeField] private float delayBeforeFire = 0.5f;
    [SerializeField] private float fireDuration = 3f;

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTriggered)
        {
            StartCoroutine(ActivateTrap());
        }
    }

    private IEnumerator ActivateTrap()
    {
        isTriggered = true;

        yield return new WaitForSeconds(delayBeforeFire);

        // 🔥 Activate crack animation
        animator.SetTrigger("Activate");

        // 🔥 Activate fire
        fireObject.SetActive(true);

        yield return new WaitForSeconds(fireDuration);

        fireObject.SetActive(false);

        isTriggered = false;
    }
}
using UnityEngine;

public class TurretVision : MonoBehaviour
{
    [SerializeField] private TurretShooting shooting;
     [SerializeField] private Animator animator;


   

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
           animator.SetBool("playerInVision", true);
        shooting.StartFiring();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
          animator.SetBool("playerInVision", false);
        shooting.StopFiring();
    }
}

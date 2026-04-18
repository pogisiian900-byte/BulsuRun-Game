using UnityEngine;

public class FlyTrigger : MonoBehaviour
{
    [SerializeField] private TriggeredFly targetEnemy;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            targetEnemy.StartFlying();
            Debug.Log("TRIGGER HIT BY: " + other.name);
            gameObject.SetActive(false); // disable trigger after use
        }
    }
}
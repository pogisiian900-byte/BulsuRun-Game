using UnityEngine;

public class LevelAutoRun : MonoBehaviour
{
    [SerializeField] private bool enableAutoRun = true;
    [SerializeField] private int direction = 1; // 1 = right, -1 = left

    void Start()
    {
        // CHANGED: Look for PlayerInput instead of PlayerMovement
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();

        if (playerInput != null)
        {
            playerInput.SetAutoRun(enableAutoRun, direction);
        }
        else
        {
            Debug.LogWarning("LevelAutoRun could not find PlayerInput in the scene!");
        }
    }
}
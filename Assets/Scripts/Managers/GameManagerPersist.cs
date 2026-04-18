using UnityEngine;

public class GameManagersPersist : MonoBehaviour
{
    private static GameManagersPersist instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

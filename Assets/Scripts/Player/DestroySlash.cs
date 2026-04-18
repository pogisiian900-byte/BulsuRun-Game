using UnityEngine;

public class DestroySlash : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 0.25f);
    }
}
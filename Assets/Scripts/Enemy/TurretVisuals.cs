using UnityEngine;

public class TurretVisuals : MonoBehaviour
{
    [SerializeField] private SpriteRenderer headRenderer;


    private void Awake()
    {
            headRenderer.enabled = false;// hidden by default
    }

    // Called by Animation Event
    public void ShowHead()
    {

     headRenderer.enabled = true;

    }

    public void HideHead()
    {
        headRenderer.enabled = false;

    }
}

using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    private const float InnerPadding = 0.02f;

    private static Sprite whiteSprite;

    private Transform target;
    private Vector3 worldOffset;
    private float width;
    private float height;
    private float innerWidth;

    private SpriteRenderer frameRenderer;
    private SpriteRenderer emptyRenderer;
    private SpriteRenderer fillRenderer;

    public void Initialize(Transform followTarget, SpriteRenderer ownerRenderer, Vector3 offset, float barWidth, float barHeight)
    {
        target = followTarget;
        worldOffset = offset;
        width = Mathf.Max(0.1f, barWidth);
        height = Mathf.Max(0.02f, barHeight);

        BuildRenderers(ownerRenderer);
        SetHealth(1f);
        UpdatePosition();
    }

    public void SetHealth(float normalizedHealth)
    {
        if (emptyRenderer == null || fillRenderer == null)
        {
            return;
        }

        float innerHeight = Mathf.Max(0.01f, height - (InnerPadding * 2f));
        innerWidth = Mathf.Max(0.02f, width - (InnerPadding * 2f));

        emptyRenderer.transform.localScale = new Vector3(innerWidth, innerHeight, 1f);

        normalizedHealth = Mathf.Clamp01(normalizedHealth);
        float currentWidth = innerWidth * normalizedHealth;

        fillRenderer.enabled = currentWidth > 0.001f;
        if (!fillRenderer.enabled)
        {
            return;
        }

        fillRenderer.transform.localScale = new Vector3(currentWidth, innerHeight, 1f);
        fillRenderer.transform.localPosition = new Vector3((currentWidth - innerWidth) * 0.5f, 0f, 0f);
        fillRenderer.color = Color.Lerp(new Color(0.85f, 0.18f, 0.18f), new Color(0.2f, 0.85f, 0.25f), normalizedHealth);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePosition();
    }

    private void BuildRenderers(SpriteRenderer ownerRenderer)
    {
        frameRenderer = CreateRenderer("Frame", Color.black);
        emptyRenderer = CreateRenderer("Empty", new Color(0.25f, 0.07f, 0.07f, 0.95f));
        fillRenderer = CreateRenderer("Fill", new Color(0.2f, 0.85f, 0.25f));

        frameRenderer.transform.localScale = new Vector3(width, height, 1f);

        int sortingLayerID = ownerRenderer != null ? ownerRenderer.sortingLayerID : 0;
        int baseSortingOrder = ownerRenderer != null ? ownerRenderer.sortingOrder + 20 : 20;

        frameRenderer.sortingLayerID = sortingLayerID;
        frameRenderer.sortingOrder = baseSortingOrder;

        emptyRenderer.sortingLayerID = sortingLayerID;
        emptyRenderer.sortingOrder = baseSortingOrder + 1;

        fillRenderer.sortingLayerID = sortingLayerID;
        fillRenderer.sortingOrder = baseSortingOrder + 2;
    }

    private SpriteRenderer CreateRenderer(string childName, Color color)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite;
        renderer.color = color;
        return renderer;
    }

    private void UpdatePosition()
    {
        transform.position = target.position + worldOffset;
    }

    private static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                whiteSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
            }

            return whiteSprite;
        }
    }
}

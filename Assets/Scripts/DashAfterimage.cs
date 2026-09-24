using UnityEngine;

public sealed class DashAfterimage : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color startingColor;
    private Vector3 startingScale;
    private float lifetime;
    private float finalScale;
    private float elapsedTime;

    public void Initialize(
        SpriteRenderer targetRenderer,
        Color color,
        float duration,
        float finalScaleMultiplier)
    {
        spriteRenderer = targetRenderer;
        startingColor = color;
        startingScale = transform.localScale;
        lifetime = Mathf.Max(0.01f, duration);
        finalScale = Mathf.Clamp01(finalScaleMultiplier);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / lifetime);

        Color fadedColor = startingColor;
        fadedColor.a = Mathf.Lerp(startingColor.a, 0f, progress);
        spriteRenderer.color = fadedColor;

        float scaleMultiplier = Mathf.Lerp(1f, finalScale, progress);
        transform.localScale = startingScale * scaleMultiplier;

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}

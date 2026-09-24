using UnityEngine;

public sealed class CrystalShard : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private Color startingColor;
    private float lifetime;
    private float elapsedTime;
    private float landingHeight;
    private bool stopAtBase;
    private bool hasLanded;

    public void Initialize(
        SpriteRenderer targetRenderer,
        Rigidbody2D shardBody,
        float duration,
        bool shouldStopAtBase,
        float baseHeight)
    {
        spriteRenderer = targetRenderer;
        body = shardBody;
        startingColor = targetRenderer.color;
        lifetime = Mathf.Max(0.01f, duration);
        stopAtBase = shouldStopAtBase;
        landingHeight = transform.position.y + (baseHeight - targetRenderer.bounds.min.y);
    }

    private void Update()
    {
        if (stopAtBase && !hasLanded && transform.position.y <= landingHeight)
        {
            Vector3 landedPosition = transform.position;
            landedPosition.y = landingHeight;
            transform.position = landedPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.gravityScale = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            hasLanded = true;
        }

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / lifetime);

        // Keep shards solid at first, then fade during the latter half.
        float fadeProgress = Mathf.InverseLerp(0.5f, 1f, progress);
        Color fadedColor = startingColor;
        fadedColor.a = Mathf.Lerp(startingColor.a, 0f, fadeProgress);
        spriteRenderer.color = fadedColor;

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}

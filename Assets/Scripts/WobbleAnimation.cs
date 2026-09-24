using UnityEngine;

[DisallowMultipleComponent]
public sealed class WobbleAnimation : MonoBehaviour
{
    [Header("Idle Wobble")]
    [SerializeField, Min(0f)] private float idleMinYScale = 0.9f;
    [SerializeField, Min(0f)] private float idleMaxYScale = 1f;
    [SerializeField] private float idleMinRotation = -2f;
    [SerializeField] private float idleMaxRotation = 2f;
    [SerializeField, Min(0f)] private float idleCyclesPerSecond = 1.5f;

    [Header("Run Wobble")]
    [SerializeField, Min(0f)] private float runMinYScale = 0.8f;
    [SerializeField, Min(0f)] private float runMaxYScale = 1f;
    [SerializeField] private float runMinRotation = -4f;
    [SerializeField] private float runMaxRotation = 4f;
    [SerializeField, Min(0f)] private float runCyclesPerSecond = 3.5f;

    [Header("State Transition")]
    [SerializeField, Min(0f)] private float transitionSpeed = 8f;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Grounding")]
    [Tooltip("Keeps the bottom-center of the sprite fixed while it scales and rotates.")]
    [SerializeField] private bool keepFeetPlanted = true;
    [Tooltip("Fine-tunes the planted point horizontally in the sprite's local units.")]
    [SerializeField] private float footAnchorXOffset;
    [Tooltip("Fine-tunes the planted point vertically in the sprite's local units.")]
    [SerializeField] private float footAnchorYOffset;

    private Vector3 baseScale;
    private Vector3 baseLocalPosition;
    private Vector3 baseEulerAngles;
    private Quaternion baseLocalRotation;
    private Vector3 footAnchor;
    private float phase;
    private float runBlend;

    private void Reset()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
        }

        baseScale = transform.localScale;
        baseLocalPosition = transform.localPosition;
        baseEulerAngles = transform.localEulerAngles;
        baseLocalRotation = transform.localRotation;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            footAnchor = new Vector3(
                spriteBounds.center.x + footAnchorXOffset,
                spriteBounds.min.y + footAnchorYOffset,
                0f);
        }
    }

    private void Update()
    {
        bool isRunning = playerMovement != null && playerMovement.IsMoving;
        float targetBlend = isRunning ? 1f : 0f;
        runBlend = Mathf.MoveTowards(runBlend, targetBlend, transitionSpeed * Time.deltaTime);

        float cyclesPerSecond = Mathf.Lerp(idleCyclesPerSecond, runCyclesPerSecond, runBlend);
        phase = Mathf.Repeat(phase + cyclesPerSecond * Mathf.PI * 2f * Time.deltaTime, Mathf.PI * 2f);

        float wave = Mathf.Sin(phase);
        float wave01 = (wave + 1f) * 0.5f;

        float minYScale = Mathf.Lerp(idleMinYScale, runMinYScale, runBlend);
        float maxYScale = Mathf.Lerp(idleMaxYScale, runMaxYScale, runBlend);
        float yScaleMultiplier = Mathf.Lerp(
            Mathf.Min(minYScale, maxYScale),
            Mathf.Max(minYScale, maxYScale),
            wave01);

        Vector3 animatedScale = transform.localScale;
        animatedScale.y = baseScale.y * yScaleMultiplier;
        transform.localScale = animatedScale;

        float minRotation = Mathf.Lerp(idleMinRotation, runMinRotation, runBlend);
        float maxRotation = Mathf.Lerp(idleMaxRotation, runMaxRotation, runBlend);
        float rotationAmount = Mathf.Lerp(
            Mathf.Min(minRotation, maxRotation),
            Mathf.Max(minRotation, maxRotation),
            wave01);
        Quaternion animatedRotation = Quaternion.Euler(
            baseEulerAngles.x,
            baseEulerAngles.y,
            baseEulerAngles.z + rotationAmount);
        transform.localRotation = animatedRotation;

        if (keepFeetPlanted)
        {
            Vector3 originalAnchorOffset = baseLocalRotation * Vector3.Scale(baseScale, footAnchor);
            Vector3 animatedAnchorOffset = animatedRotation * Vector3.Scale(animatedScale, footAnchor);
            transform.localPosition = baseLocalPosition + originalAnchorOffset - animatedAnchorOffset;
        }
        else
        {
            transform.localPosition = baseLocalPosition;
        }
    }

    private void OnDisable()
    {
        Vector3 restoredScale = transform.localScale;
        restoredScale.y = baseScale.y;
        transform.localScale = restoredScale;
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
    }
}

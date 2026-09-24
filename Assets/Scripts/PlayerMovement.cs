using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    [Header("Dash")]
    [SerializeField, Min(0f)] private float dashSpeed = 14f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.18f;
    [SerializeField, Min(0f)] private float dashCooldown = 0.35f;

    [Header("Dash Afterimages")]
    [SerializeField] private SpriteRenderer afterimageSource;
    [SerializeField] private Color afterimageColor = new Color(0.35f, 0.75f, 1f, 0.65f);
    [SerializeField, Min(0.01f)] private float afterimageLifetime = 0.2f;
    [SerializeField, Min(0.01f)] private float afterimageSpacing = 0.04f;
    [SerializeField, Range(0f, 1f)] private float afterimageFinalScale = 0.9f;
    [SerializeField] private int afterimageSortingOffset = -1;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.down;
    private Vector2 dashDirection;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    private float afterimageTimer;
    private bool dashRequested;

    private bool IsDashing => dashTimeRemaining > 0f;

    public bool IsMoving => moveInput.sqrMagnitude > 0.001f || IsDashing;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        // This also makes the controller safe if it is copied onto a Player
        // object that does not have its Rigidbody2D set up yet.
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        if (afterimageSource == null)
        {
            afterimageSource = GetComponentInChildren<SpriteRenderer>();
        }

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.wKey.isPressed) vertical += 1f;

        moveInput = new Vector2(horizontal, vertical).normalized;

        if (moveInput.sqrMagnitude > 0f)
        {
            lastMoveDirection = moveInput;
        }

        if (keyboard.spaceKey.wasPressedThisFrame && !IsDashing && dashCooldownRemaining <= 0f)
        {
            dashRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (dashRequested)
        {
            dashRequested = false;
            dashDirection = moveInput.sqrMagnitude > 0f ? moveInput : lastMoveDirection;
            dashTimeRemaining = dashDuration;
            afterimageTimer = 0f;
        }

        if (IsDashing)
        {
            body.linearVelocity = dashDirection * dashSpeed;
            dashTimeRemaining -= Time.fixedDeltaTime;

            if (dashTimeRemaining <= 0f)
            {
                dashCooldownRemaining = dashCooldown;
            }

            return;
        }

        dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.fixedDeltaTime);
        body.linearVelocity = moveInput * moveSpeed;
    }

    private void LateUpdate()
    {
        if (!IsDashing || afterimageSource == null || afterimageSource.sprite == null)
        {
            return;
        }

        afterimageTimer -= Time.deltaTime;
        if (afterimageTimer > 0f)
        {
            return;
        }

        CreateAfterimage();
        afterimageTimer = afterimageSpacing;
    }

    private void CreateAfterimage()
    {
        GameObject afterimageObject = new GameObject("Dash Afterimage");
        afterimageObject.layer = afterimageSource.gameObject.layer;
        afterimageObject.transform.SetPositionAndRotation(
            afterimageSource.transform.position,
            afterimageSource.transform.rotation);
        afterimageObject.transform.localScale = afterimageSource.transform.lossyScale;

        SpriteRenderer afterimageRenderer = afterimageObject.AddComponent<SpriteRenderer>();
        afterimageRenderer.sprite = afterimageSource.sprite;
        afterimageRenderer.sharedMaterial = afterimageSource.sharedMaterial;
        afterimageRenderer.flipX = afterimageSource.flipX;
        afterimageRenderer.flipY = afterimageSource.flipY;
        afterimageRenderer.drawMode = afterimageSource.drawMode;
        afterimageRenderer.size = afterimageSource.size;
        afterimageRenderer.spriteSortPoint = afterimageSource.spriteSortPoint;
        afterimageRenderer.sortingLayerID = afterimageSource.sortingLayerID;
        afterimageRenderer.sortingOrder = afterimageSource.sortingOrder + afterimageSortingOffset;
        afterimageRenderer.maskInteraction = afterimageSource.maskInteraction;

        Color sourceColor = afterimageSource.color;
        Color startingColor = new Color(
            sourceColor.r * afterimageColor.r,
            sourceColor.g * afterimageColor.g,
            sourceColor.b * afterimageColor.b,
            sourceColor.a * afterimageColor.a);
        afterimageRenderer.color = startingColor;

        DashAfterimage afterimage = afterimageObject.AddComponent<DashAfterimage>();
        afterimage.Initialize(afterimageRenderer, startingColor, afterimageLifetime, afterimageFinalScale);
    }

    private void OnDisable()
    {
        dashRequested = false;
        moveInput = Vector2.zero;
        afterimageTimer = 0f;

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}

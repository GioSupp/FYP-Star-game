using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RangedWeapon : MonoBehaviour
{
    [Header("Weapon Rig")]
    [SerializeField] private Transform weaponVisual;
    [SerializeField] private SpriteRenderer weaponRenderer;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0f)] private float orbitRadius = 1.15f;
    [SerializeField] private float weaponRotationOffset;
    [SerializeField] private bool flipWhenAimingLeft = true;

    [Header("Firing")]
    [SerializeField, Min(0.01f)] private float shotsPerSecond = 5f;
    [SerializeField] private bool automaticFire = true;

    [Header("Projectile")]
    [Tooltip("Optional projectile prefab. Its sprite and visual setup are used when assigned.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Fallback used when no prefab is assigned, or when the prefab has no sprite.")]
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private Color projectileColor = Color.white;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 14f;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 2f;
    [SerializeField, Min(0.01f)] private float projectileColliderRadius = 0.12f;
    [SerializeField, Min(0.01f)] private float projectileScale = 1f;
    [SerializeField] private int projectileSortingOrder = 1;

    private Camera aimCamera;
    private Collider2D ownerCollider;
    private Vector2 aimDirection = Vector2.right;
    private float nextShotTime;

    private void Awake()
    {
        aimCamera = Camera.main;
        ownerCollider = GetComponentInParent<Collider2D>();
        UpdateWeaponPosition();
    }

    private void Update()
    {
        AimAtCursor();
        HandleFiring();
    }

    private void AimAtCursor()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
            if (aimCamera == null)
            {
                return;
            }
        }

        Vector3 mouseScreenPosition = mouse.position.ReadValue();
        Vector3 mouseWorldPosition = aimCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 direction = mouseWorldPosition - transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        aimDirection = direction.normalized;
        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, aimAngle);

        UpdateWeaponPosition();

        if (weaponRenderer != null && flipWhenAimingLeft)
        {
            weaponRenderer.flipY = aimDirection.x < 0f;
        }
    }

    private void UpdateWeaponPosition()
    {
        if (weaponVisual == null)
        {
            return;
        }

        Vector3 position = weaponVisual.localPosition;
        position.x = orbitRadius;
        position.y = 0f;
        weaponVisual.localPosition = position;
        weaponVisual.localRotation = Quaternion.Euler(0f, 0f, weaponRotationOffset);
    }

    private void HandleFiring()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || Time.time < nextShotTime)
        {
            return;
        }

        bool wantsToFire = automaticFire
            ? mouse.leftButton.isPressed
            : mouse.leftButton.wasPressedThisFrame;

        if (!wantsToFire)
        {
            return;
        }

        Fire();
        nextShotTime = Time.time + 1f / shotsPerSecond;
    }

    private void Fire()
    {
        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : transform.position + (Vector3)(aimDirection * orbitRadius);
        float projectileAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        GameObject projectileObject = projectilePrefab != null
            ? Instantiate(projectilePrefab)
            : new GameObject("Player Projectile");
        projectileObject.name = "Player Projectile";
        projectileObject.transform.SetPositionAndRotation(
            spawnPosition,
            Quaternion.Euler(0f, 0f, projectileAngle));
        projectileObject.transform.localScale *= projectileScale;

        SpriteRenderer projectileRenderer = projectileObject.GetComponentInChildren<SpriteRenderer>();
        if (projectileRenderer == null)
        {
            projectileRenderer = projectileObject.AddComponent<SpriteRenderer>();
        }

        if (projectileRenderer.sprite == null)
        {
            projectileRenderer.sprite = projectileSprite;
        }

        Color prefabColor = projectileRenderer.color;
        projectileRenderer.color = new Color(
            prefabColor.r * projectileColor.r,
            prefabColor.g * projectileColor.g,
            prefabColor.b * projectileColor.b,
            prefabColor.a * projectileColor.a);
        projectileRenderer.sortingOrder = projectileSortingOrder;

        Rigidbody2D projectileBody = projectileObject.GetComponent<Rigidbody2D>();
        if (projectileBody == null)
        {
            projectileBody = projectileObject.AddComponent<Rigidbody2D>();
        }

        projectileBody.gravityScale = 0f;
        projectileBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Collider2D projectileCollider = projectileObject.GetComponentInChildren<Collider2D>();
        if (projectileCollider == null)
        {
            CircleCollider2D fallbackCollider = projectileObject.AddComponent<CircleCollider2D>();
            fallbackCollider.radius = projectileColliderRadius;
            projectileCollider = fallbackCollider;
        }

        projectileCollider.isTrigger = true;

        if (ownerCollider != null)
        {
            Physics2D.IgnoreCollision(projectileCollider, ownerCollider);
        }

        PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<PlayerProjectile>();
        }

        projectile.Initialize(projectileBody, aimDirection, projectileSpeed, projectileLifetime, transform.root);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);
    }
}

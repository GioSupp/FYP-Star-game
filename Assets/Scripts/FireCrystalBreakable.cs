using System.Collections;
using UnityEngine;

public sealed class FireCrystalBreakable : MonoBehaviour
{
    [Header("Durability")]
    [SerializeField, Min(1)] private int hitsToBreak = 3;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController animationController;
    [SerializeField] private string idleStateName = "Fire Crystal idle";
    [SerializeField] private string hitStateName = "Fire Crystal Hit";
    [SerializeField, Min(0.01f)] private float hitAnimationDuration = 0.1f;

    [Header("Drop")]
    [SerializeField] private GameObject zodiCoinPrefab;

    [Header("Break Shards")]
    [Tooltip("Optional prefab used for every shard's appearance and existing collider.")]
    [SerializeField] private GameObject shardPrefab;
    [Tooltip("Optional cut-up crystal sprites. The crystal's current sprite is used when empty.")]
    [SerializeField] private Sprite[] shardSprites;
    [SerializeField, Min(1)] private int shardCount = 6;
    [SerializeField, Min(0.01f)] private float shardLifetime = 1.25f;
    [SerializeField, Min(0f)] private float shardGravity = 2.5f;
    [SerializeField, Min(0f)] private float minShardLaunchSpeed = 2f;
    [SerializeField, Min(0f)] private float maxShardLaunchSpeed = 4.5f;
    [SerializeField, Min(0f)] private float maxShardSpin = 540f;
    [SerializeField, Min(0.01f)] private float minShardScale = 0.18f;
    [SerializeField, Min(0.01f)] private float maxShardScale = 0.35f;
    [SerializeField] private bool shardCollisions = true;
    [Tooltip("When enabled, shards fall from inside the crystal and stop on its original horizontal base.")]
    [SerializeField] private bool fallShardsFromBase;

    private int currentHits;
    private bool isBreaking;
    private Coroutine animationRoutine;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        if (animationController != null)
        {
            animator.runtimeAnimatorController = animationController;
        }

        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D crystalCollider = gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                crystalCollider.offset = spriteRenderer.sprite.bounds.center;
                crystalCollider.size = spriteRenderer.sprite.bounds.size;
            }
        }

        animator.Play(idleStateName, 0, 0f);
    }

    public void TakeProjectileHit()
    {
        if (!isActiveAndEnabled || isBreaking)
        {
            return;
        }

        currentHits++;
        animator.Play(hitStateName, 0, 0f);

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        if (currentHits >= hitsToBreak)
        {
            isBreaking = true;
            animationRoutine = StartCoroutine(BreakAfterHitAnimation());
        }
        else
        {
            animationRoutine = StartCoroutine(ReturnToIdleAfterHit());
        }
    }

    private IEnumerator ReturnToIdleAfterHit()
    {
        yield return new WaitForSeconds(hitAnimationDuration);
        animator.Play(idleStateName, 0, 0f);
        animationRoutine = null;
    }

    private IEnumerator BreakAfterHitAnimation()
    {
        yield return new WaitForSeconds(hitAnimationDuration);

        SpawnBreakShards();

        if (zodiCoinPrefab != null)
        {
            Instantiate(zodiCoinPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void SpawnBreakShards()
    {
        SpriteRenderer crystalRenderer = GetComponent<SpriteRenderer>();
        if (crystalRenderer == null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Collider2D[] playerColliders = player != null
            ? player.GetComponentsInChildren<Collider2D>()
            : System.Array.Empty<Collider2D>();

        for (int i = 0; i < shardCount; i++)
        {
            Sprite shardSprite = GetShardSprite(crystalRenderer.sprite, i);
            if (shardSprite == null)
            {
                continue;
            }

            GameObject shardObject = shardPrefab != null
                ? Instantiate(shardPrefab)
                : new GameObject("Fire Crystal Shard");
            shardObject.name = "Fire Crystal Shard";
            shardObject.layer = gameObject.layer;

            FireCrystalBreakable[] nestedBreakables = shardObject.GetComponentsInChildren<FireCrystalBreakable>();
            foreach (FireCrystalBreakable nestedBreakable in nestedBreakables)
            {
                nestedBreakable.enabled = false;
            }

            shardObject.transform.position = fallShardsFromBase
                ? GetFallingShardPosition(crystalRenderer, i)
                : transform.position + (Vector3)(Random.insideUnitCircle * 0.15f);
            shardObject.transform.rotation = transform.rotation * Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));

            float minScale = Mathf.Min(minShardScale, maxShardScale);
            float maxScale = Mathf.Max(minShardScale, maxShardScale);
            float shardScale = Random.Range(minScale, maxScale);
            shardObject.transform.localScale = Vector3.Scale(
                shardObject.transform.localScale,
                transform.lossyScale) * shardScale;

            SpriteRenderer shardRenderer = shardObject.GetComponentInChildren<SpriteRenderer>();
            if (shardRenderer == null)
            {
                shardRenderer = shardObject.AddComponent<SpriteRenderer>();
            }

            if (shardRenderer.sprite == null)
            {
                shardRenderer.sprite = shardSprite;
                shardRenderer.sharedMaterial = crystalRenderer.sharedMaterial;
                shardRenderer.color = crystalRenderer.color;
            }

            shardRenderer.sortingLayerID = crystalRenderer.sortingLayerID;
            shardRenderer.sortingOrder = crystalRenderer.sortingOrder + 1;

            Rigidbody2D shardBody = shardObject.GetComponent<Rigidbody2D>();
            if (shardBody == null)
            {
                shardBody = shardObject.AddComponent<Rigidbody2D>();
            }

            shardBody.bodyType = RigidbodyType2D.Dynamic;
            shardBody.gravityScale = shardGravity;
            shardBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (fallShardsFromBase)
            {
                shardBody.linearVelocity = new Vector2(Random.Range(-0.2f, 0.2f), 0f);
                shardBody.angularVelocity = Random.Range(-maxShardSpin * 0.25f, maxShardSpin * 0.25f);
            }
            else
            {
                float launchAngle = Random.Range(25f, 155f);
                Vector2 launchDirection = new Vector2(
                    Mathf.Cos(launchAngle * Mathf.Deg2Rad),
                    Mathf.Sin(launchAngle * Mathf.Deg2Rad));
                float minSpeed = Mathf.Min(minShardLaunchSpeed, maxShardLaunchSpeed);
                float maxSpeed = Mathf.Max(minShardLaunchSpeed, maxShardLaunchSpeed);
                shardBody.linearVelocity = launchDirection * Random.Range(minSpeed, maxSpeed);
                shardBody.angularVelocity = Random.Range(-maxShardSpin, maxShardSpin);
            }

            Collider2D[] shardColliders = shardObject.GetComponentsInChildren<Collider2D>();
            if (shardCollisions && shardColliders.Length == 0)
            {
                BoxCollider2D fallbackCollider = shardObject.AddComponent<BoxCollider2D>();
                fallbackCollider.offset = shardRenderer.sprite.bounds.center;
                fallbackCollider.size = shardRenderer.sprite.bounds.size * 0.75f;
                shardColliders = new Collider2D[] { fallbackCollider };
            }

            foreach (Collider2D shardCollider in shardColliders)
            {
                shardCollider.enabled = shardCollisions;

                foreach (Collider2D playerCollider in playerColliders)
                {
                    Physics2D.IgnoreCollision(shardCollider, playerCollider, true);
                }
            }

            CrystalShard shard = shardObject.GetComponent<CrystalShard>();
            if (shard == null)
            {
                shard = shardObject.AddComponent<CrystalShard>();
            }

            shard.Initialize(
                shardRenderer,
                shardBody,
                shardLifetime,
                fallShardsFromBase,
                crystalRenderer.bounds.min.y);
        }
    }

    private Vector3 GetFallingShardPosition(SpriteRenderer crystalRenderer, int shardIndex)
    {
        Bounds worldBounds = crystalRenderer.bounds;
        float evenlySpacedPosition = (shardIndex + 0.5f) / shardCount;
        float horizontalPosition = Mathf.Lerp(worldBounds.min.x, worldBounds.max.x, evenlySpacedPosition);
        float smallJitter = worldBounds.size.x / shardCount * Random.Range(-0.2f, 0.2f);
        float verticalPosition = Random.Range(worldBounds.center.y, worldBounds.max.y);

        return new Vector3(
            horizontalPosition + smallJitter,
            verticalPosition,
            transform.position.z);
    }

    private Sprite GetShardSprite(Sprite fallbackSprite, int shardIndex)
    {
        if (shardSprites == null || shardSprites.Length == 0)
        {
            return fallbackSprite;
        }

        return shardSprites[shardIndex % shardSprites.Length];
    }
}

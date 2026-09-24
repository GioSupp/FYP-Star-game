using UnityEngine;

public sealed class PlayerProjectile : MonoBehaviour
{
    private Rigidbody2D body;
    private Transform ownerRoot;
    private float remainingLifetime;

    public void Initialize(
        Rigidbody2D projectileBody,
        Vector2 direction,
        float speed,
        float lifetime,
        Transform owner)
    {
        body = projectileBody;
        ownerRoot = owner;
        remainingLifetime = lifetime;
        body.linearVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        FireCrystalBreakable fireCrystal = other.GetComponentInParent<FireCrystalBreakable>();
        if (fireCrystal != null)
        {
            fireCrystal.TakeProjectileHit();
        }

        Destroy(gameObject);
    }
}

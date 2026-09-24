using System.Collections;
using UnityEngine;

public sealed class ZodiCoinPickup : MonoBehaviour
{
    [Header("Value")]
    [SerializeField, Min(1)] private int coinValue = 1;

    [Header("Pickup Opacity Animation")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;
    [SerializeField, Min(0f)] private float pickupScaleMultiplier = 1.25f;

    private Collider2D pickupCollider;
    private SpriteRenderer[] spriteRenderers;
    private bool collected;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
        {
            return;
        }

        ZodiCoinWallet wallet = other.GetComponentInParent<ZodiCoinWallet>();
        if (wallet == null)
        {
            return;
        }

        collected = true;
        wallet.AddCoins(coinValue);

        if (pickupCollider != null)
        {
            pickupCollider.enabled = false;
        }

        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        Vector3 startingScale = transform.localScale;
        Color[] startingColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            startingColors[i] = spriteRenderers[i].color;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            transform.localScale = Vector3.Lerp(
                startingScale,
                startingScale * pickupScaleMultiplier,
                progress);

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                Color fadedColor = startingColors[i];
                fadedColor.a = Mathf.Lerp(startingColors[i].a, 0f, progress);
                spriteRenderers[i].color = fadedColor;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}

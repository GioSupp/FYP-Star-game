using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class ZodiCoinWallet : MonoBehaviour
{
    [Header("Currency")]
    [SerializeField, Min(0)] private int startingCoins;

    [Header("HUD Position")]
    [SerializeField] private float hudPositionX = -28f;
    [SerializeField] private float hudPositionY = 24f;

    [Header("HUD Layout")]
    [SerializeField] private Vector2 hudSize = new Vector2(160f, 64f);
    [SerializeField, Min(1f)] private float coinIconSize = 42f;
    [Tooltip("Use a negative value to move the number farther left from the coin icon.")]
    [SerializeField] private float counterXOffset;
    [SerializeField, Min(1)] private int fontSize = 30;
    [SerializeField] private Color textColor = Color.white;

    [Header("Coin Icon Animation")]
    [SerializeField] private Sprite[] coinAnimationFrames;
    [SerializeField, Min(0.1f)] private float coinFramesPerSecond = 8f;

    [Header("Pickup Opacity Animation")]
    [SerializeField, Range(0f, 1f)] private float idleOpacity = 0.7f;
    [SerializeField, Range(0f, 1f)] private float pickupOpacity = 1f;
    [SerializeField, Min(0f)] private float pickupHoldDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float opacityFadeDuration = 0.35f;

    private Text coinCountText;
    private Image coinIcon;
    private CanvasGroup hudCanvasGroup;
    private GameObject hudObject;
    private RectTransform hudRect;
    private RectTransform iconRect;
    private RectTransform textRect;
    private Coroutine opacityRoutine;
    private float frameTimer;
    private int frameIndex;

    public int CurrentCoins { get; private set; }

    private void OnEnable()
    {
        CurrentCoins = Mathf.Max(0, startingCoins);
        EnsureHudExists();
        ApplyHudSettings();
        RefreshCount();
    }

    private void Update()
    {
        EnsureHudExists();

        if (!Application.isPlaying)
        {
            CurrentCoins = Mathf.Max(0, startingCoins);
            ApplyHudSettings();
            RefreshCount();
        }

        AnimateCoinIcon();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentCoins += amount;
        RefreshCount();

        if (opacityRoutine != null)
        {
            StopCoroutine(opacityRoutine);
        }

        opacityRoutine = StartCoroutine(PlayPickupOpacity());
    }

    private void EnsureHudExists()
    {
        if (hudObject != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Zodi Coin HUD");
        hudObject = canvasObject;

        if (!Application.isPlaying)
        {
            canvasObject.hideFlags = HideFlags.DontSaveInEditor;
        }

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        hudCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        hudCanvasGroup.alpha = idleOpacity;
        hudCanvasGroup.interactable = false;
        hudCanvasGroup.blocksRaycasts = false;

        GameObject groupObject = new GameObject("HUD Group", typeof(RectTransform));
        groupObject.transform.SetParent(canvasObject.transform, false);
        hudRect = groupObject.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(1f, 0f);
        hudRect.anchorMax = new Vector2(1f, 0f);
        hudRect.pivot = new Vector2(1f, 0f);
        hudRect.anchoredPosition = new Vector2(hudPositionX, hudPositionY);
        hudRect.sizeDelta = hudSize;

        GameObject iconObject = new GameObject("Coin Icon");
        iconObject.transform.SetParent(groupObject.transform, false);
        coinIcon = iconObject.AddComponent<Image>();
        coinIcon.preserveAspect = true;
        coinIcon.raycastTarget = false;

        iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(1f, 0.5f);
        iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = Vector2.one * coinIconSize;

        if (coinAnimationFrames != null && coinAnimationFrames.Length > 0)
        {
            coinIcon.sprite = coinAnimationFrames[0];
        }

        GameObject textObject = new GameObject("Coin Count");
        textObject.transform.SetParent(groupObject.transform, false);
        coinCountText = textObject.AddComponent<Text>();
        coinCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinCountText.fontSize = fontSize;
        coinCountText.fontStyle = FontStyle.Bold;
        coinCountText.alignment = TextAnchor.MiddleRight;
        coinCountText.color = textColor;
        coinCountText.raycastTarget = false;

        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(1f, 0.5f);
        textRect.sizeDelta = new Vector2(
            Mathf.Max(1f, hudSize.x - coinIconSize - 8f),
            hudSize.y);
        textRect.anchoredPosition = new Vector2(
            -coinIconSize - 8f + counterXOffset,
            0f);
    }

    private void ApplyHudSettings()
    {
        if (hudObject == null)
        {
            return;
        }

        hudRect.anchoredPosition = new Vector2(hudPositionX, hudPositionY);
        hudRect.sizeDelta = hudSize;
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = Vector2.one * coinIconSize;
        textRect.sizeDelta = new Vector2(
            Mathf.Max(1f, hudSize.x - coinIconSize - 8f),
            hudSize.y);
        textRect.anchoredPosition = new Vector2(
            -coinIconSize - 8f + counterXOffset,
            0f);
        coinCountText.fontSize = fontSize;
        coinCountText.color = textColor;

        if (!Application.isPlaying)
        {
            hudCanvasGroup.alpha = idleOpacity;

            if (coinAnimationFrames != null && coinAnimationFrames.Length > 0)
            {
                frameIndex = Mathf.Clamp(frameIndex, 0, coinAnimationFrames.Length - 1);
                coinIcon.sprite = coinAnimationFrames[frameIndex];
            }
        }
    }

    private void OnDisable()
    {
        DestroyHud();
    }

    private void OnDestroy()
    {
        DestroyHud();
    }

    private void DestroyHud()
    {
        if (hudObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(hudObject);
        }
        else
        {
            DestroyImmediate(hudObject);
        }

        hudObject = null;
        hudRect = null;
        iconRect = null;
        textRect = null;
        coinCountText = null;
        coinIcon = null;
        hudCanvasGroup = null;
    }

    private void RefreshCount()
    {
        if (coinCountText != null)
        {
            coinCountText.text = CurrentCoins.ToString();
        }
    }

    private void AnimateCoinIcon()
    {
        if (coinIcon == null || coinAnimationFrames == null || coinAnimationFrames.Length == 0)
        {
            return;
        }

        frameTimer += Time.unscaledDeltaTime;
        float frameDuration = 1f / coinFramesPerSecond;

        if (frameTimer < frameDuration)
        {
            return;
        }

        frameTimer %= frameDuration;
        frameIndex = (frameIndex + 1) % coinAnimationFrames.Length;
        coinIcon.sprite = coinAnimationFrames[frameIndex];
    }

    private IEnumerator PlayPickupOpacity()
    {
        hudCanvasGroup.alpha = pickupOpacity;
        yield return new WaitForSecondsRealtime(pickupHoldDuration);

        float elapsed = 0f;
        while (elapsed < opacityFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / opacityFadeDuration);
            hudCanvasGroup.alpha = Mathf.Lerp(pickupOpacity, idleOpacity, progress);
            yield return null;
        }

        hudCanvasGroup.alpha = idleOpacity;
        opacityRoutine = null;
    }
}

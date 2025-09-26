using UnityEngine;
using System.Collections;

public class FlashlightController : MonoBehaviour
{
    [Tooltip("Light component (Spot) of the player's flashlight")]
    public Light flashlight;

    [Header("Usage timing (seconds)")]
    public Vector2 lightDurationRange = new Vector2(5f, 7f);
    public float flickerDuration = 2f;
    public Vector2 flickerIntervalRange = new Vector2(0.3f, 1f);

    private bool canUse = false;   // chỉ bật khi đã nhặt
    private bool isOn = false;
    private Coroutine cycleCoroutine;

    void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light>();
    }

    void Start()
    {
        if (flashlight != null)
            flashlight.enabled = false;
    }

    void Update()
    {
        // ❌ Chỉ cho phép bật đèn SAU KHI nhặt
        if (!canUse) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!isOn)
                TurnOn();
            else
                TurnOffImmediate();
        }
    }

    public void ActivateFlashlight()
    {
        canUse = true;   // ✅ Chỉ được bật sau khi nhặt
    }

    void TurnOn()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        cycleCoroutine = StartCoroutine(FlashlightCycle());
    }

    void TurnOffImmediate()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        if (flashlight != null) flashlight.enabled = false;
        isOn = false;
        cycleCoroutine = null;
    }

    IEnumerator FlashlightCycle()
    {
        isOn = true;
        if (flashlight != null) flashlight.enabled = true;

        float normalDuration = Random.Range(lightDurationRange.x, lightDurationRange.y);
        yield return new WaitForSeconds(normalDuration);

        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            if (flashlight != null) flashlight.enabled = !flashlight.enabled;
            float iv = Random.Range(flickerIntervalRange.x, flickerIntervalRange.y);
            yield return new WaitForSeconds(iv);
            elapsed += iv;
        }

        if (flashlight != null) flashlight.enabled = false;
        isOn = false;
        cycleCoroutine = null;
    }

    public void ResetCycle()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        if (flashlight != null) flashlight.enabled = false;
        isOn = false;
        cycleCoroutine = null;
    }
}

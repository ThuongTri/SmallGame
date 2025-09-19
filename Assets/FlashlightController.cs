// File: FlashlightController.cs
using UnityEngine;
using System.Collections;

public class FlashlightController : MonoBehaviour
{
    [Tooltip("Light component (Spot) of the player's flashlight")]
    public Light flashlight;

    [Header("Usage timing (seconds)")]
    public Vector2 lightDurationRange = new Vector2(5f, 7f); // random between
    public float flickerDuration = 2f; // tổng thời gian chập chờn trước khi tắt
    public Vector2 flickerIntervalRange = new Vector2(0.3f, 1f); // interval random trong lúc flicker

    bool canUse = false;   // chỉ cho bật khi đã pick up
    bool isOn = false;
    Coroutine cycleCoroutine;

    void Awake()
    {
        // tự tìm Light nếu chưa gán
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
        canUse = true;
    }

    // bật và khởi chu trình: sáng bình thường -> flicker -> tắt
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

        // sáng bình thường (random trong range)
        float normalDuration = Random.Range(lightDurationRange.x, lightDurationRange.y);
        yield return new WaitForSeconds(normalDuration);

        // flicker phase
        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            if (flashlight != null) flashlight.enabled = !flashlight.enabled;
            float iv = Random.Range(flickerIntervalRange.x, flickerIntervalRange.y);
            yield return new WaitForSeconds(iv);
            elapsed += iv;
        }

        // ensure off at end
        if (flashlight != null) flashlight.enabled = false;
        isOn = false;
        cycleCoroutine = null;
    }

    // Optionally reset the cycle so player can immediately turn on again (call if needed)
    public void ResetCycle()
    {
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        if (flashlight != null) flashlight.enabled = false;
        isOn = false;
        cycleCoroutine = null;
    }
}

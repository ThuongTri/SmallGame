using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight camera shake by offsetting a target transform (usually the camera rig / child camera).
/// </summary>
public class CameraShakeImpulse : MonoBehaviour
{
    public Transform shakeTarget;
    public float positionalAmplitude = 0.08f;
    public float rotationalAmplitude = 0.6f;
    public float frequency = 22f;

    Coroutine routine;
    Vector3 baseLocalPos;
    Quaternion baseLocalRot;

    void Awake()
    {
        if (shakeTarget == null) shakeTarget = transform;

        // Gán nhầm shakeTarget = root Player → localPosition rung = dịch cả CharacterController (giống teleport / bay về spawn).
        if (shakeTarget.CompareTag("Player") || shakeTarget.GetComponent<CharacterController>() != null)
        {
            Camera cam = GetComponentInChildren<Camera>(true);
            if (cam != null)
                shakeTarget = cam.transform;
            else
                shakeTarget = transform;
        }

        baseLocalPos = shakeTarget.localPosition;
        baseLocalRot = shakeTarget.localRotation;
    }

    public void Shake(float seconds, float intensity = 1f)
    {
        if (seconds <= 0f) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShakeRoutine(seconds, Mathf.Clamp01(intensity)));
    }

    IEnumerator ShakeRoutine(float seconds, float intensity)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float w = 1f - Mathf.Clamp01(t / seconds); // decay

            float n1 = Mathf.PerlinNoise(Time.unscaledTime * frequency, 17.3f) - 0.5f;
            float n2 = Mathf.PerlinNoise(91.1f, Time.unscaledTime * frequency) - 0.5f;
            float n3 = Mathf.PerlinNoise(Time.unscaledTime * frequency * 0.9f, 33.7f) - 0.5f;

            Vector3 pos = new Vector3(n1, n2 * 0.35f, n3) * positionalAmplitude * intensity * w;
            Vector3 euler = new Vector3(n2, n1, n3) * rotationalAmplitude * intensity * w;

            shakeTarget.localPosition = baseLocalPos + pos;
            shakeTarget.localRotation = baseLocalRot * Quaternion.Euler(euler);

            yield return null;
        }

        shakeTarget.localPosition = baseLocalPos;
        shakeTarget.localRotation = baseLocalRot;
        routine = null;
    }
}

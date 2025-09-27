using UnityEngine;
using System;

public class NoiseEmitter : MonoBehaviour
{
    // Event phát ra khi có tiếng động
    public static event Action<Vector3, float> OnNoise;

    public static void EmitNoise(Vector3 pos, float loudness)
    {
        OnNoise?.Invoke(pos, loudness);
    }

    // Instance call (nếu cần gắn trực tiếp lên object)
    public void Emit(float loudness)
    {
        EmitNoise(transform.position, loudness);
    }
}
